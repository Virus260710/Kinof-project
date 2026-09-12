using System.Collections.Concurrent;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record KioskVerifyOtpRequest(Guid RoomId, string? Code);
public sealed record KioskVerifyFaceRequest(Guid RoomId, string? ImageBase64);

/// <summary>
/// Brute-force guard for the public Kiosk endpoints: 5 failed attempts per room inside a
/// 15 minute window, counted separately for each credential type (see
/// <see cref="KioskService.OtpScope"/> and <see cref="KioskService.FaceScope"/>) so an
/// unrecognised face never locks out the OTP fallback. In-memory is enough while every
/// Kiosk talks to a single API instance; a shared cache is needed once the API scales out.
/// </summary>
public sealed class KioskAttemptLimiter
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, List<DateTime>> failures = new();

    public TimeSpan? GetLockRemaining(string scope, Guid roomId, DateTime nowUtc)
    {
        if (!failures.TryGetValue(Key(scope, roomId), out var attempts))
            return null;

        lock (attempts)
        {
            Prune(attempts, nowUtc);
            if (attempts.Count < MaxFailures)
                return null;
            var remaining = attempts[0].Add(Window) - nowUtc;
            return remaining > TimeSpan.Zero ? remaining : null;
        }
    }

    public void RecordFailure(string scope, Guid roomId, DateTime nowUtc)
    {
        var attempts = failures.GetOrAdd(Key(scope, roomId), _ => []);
        lock (attempts)
        {
            Prune(attempts, nowUtc);
            attempts.Add(nowUtc);
        }
    }

    public void Reset(string scope, Guid roomId) => failures.TryRemove(Key(scope, roomId), out _);

    private static string Key(string scope, Guid roomId) => $"{scope}:{roomId}";

    private static void Prune(List<DateTime> attempts, DateTime nowUtc) =>
        attempts.RemoveAll(attempt => attempt < nowUtc - Window);
}

public sealed class KioskService(
    AppDbContext db,
    EntryService entryService,
    IFaceEmbeddingClient faceEmbeddingClient,
    FaceMatchingService faceMatchingService,
    KioskAttemptLimiter attemptLimiter,
    ILogger<KioskService> logger)
{
    public const string OtpScope = "otp";
    public const string FaceScope = "face";

    /// <summary>Newest entry OTPs only — bounds the number of BCrypt verifies per request.</summary>
    private const int MaxCandidateOtps = 200;

    public async Task<IResult> GetRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await db.Rooms
            .AsNoTracking()
            .Where(x => x.Id == roomId)
            .Select(x => new
            {
                id = x.Id,
                name = x.Name,
                building = x.Building,
                status = x.Status.ToString().ToLowerInvariant()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return room is null
            ? Results.NotFound(new { message = "ไม่พบห้องแล็บนี้" })
            : Results.Ok(room);
    }

    public async Task<IResult> VerifyEntryOtpAsync(
        KioskVerifyOtpRequest? request,
        CancellationToken cancellationToken)
    {
        var roomId = request?.RoomId ?? Guid.Empty;
        var code = request?.Code?.Trim() ?? "";
        if (roomId == Guid.Empty)
            return Results.BadRequest(new { granted = false, message = "ไม่ระบุห้องแล็บ" });
        if (code.Length != 6 || !code.All(char.IsAsciiDigit))
            return Results.Ok(new { granted = false, message = "กรุณากรอกรหัสตัวเลข 6 หลัก" });

        var nowUtc = DateTime.UtcNow;
        if (attemptLimiter.GetLockRemaining(OtpScope, roomId, nowUtc) is TimeSpan remaining)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
            return Results.Json(
                new { granted = false, message = $"กรอกรหัสผิดหลายครั้งเกินไป กรุณารอ {minutes} นาทีแล้วลองใหม่" },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var otp = await FindMatchingOtpAsync(code, nowUtc, cancellationToken);
        if (otp is null)
            return FailOtp(roomId, nowUtc, "รหัสไม่ถูกต้องหรือหมดอายุแล้ว กรุณาขอรหัสใหม่จากเว็บ");
        if (otp.RoomId is Guid boundRoomId && boundRoomId != roomId)
            return FailOtp(roomId, nowUtc, "รหัสนี้ขอไว้สำหรับห้องอื่น กรุณาขอรหัสใหม่สำหรับห้องนี้");

        otp.UsedAt = nowUtc;
        await db.SaveChangesAsync(cancellationToken);

        var decision = await entryService.AuthorizeAndAssignSeatAsync(
            otp.UserId,
            roomId,
            AuthMethod.OtpFallback,
            cancellationToken);

        if (!decision.Granted)
        {
            logger.LogInformation(
                "Kiosk entry denied for user {UserId} at room {RoomId}: {Reason}",
                otp.UserId,
                roomId,
                decision.Message);
            return Results.Ok(new { granted = false, message = decision.Message });
        }

        attemptLimiter.Reset(OtpScope, roomId);
        return Granted(decision);
    }

    /// <summary>
    /// Phase 3C face path: identify the person from a single Kiosk frame, then run the
    /// exact same entitlement pipeline as the OTP path. Every failure carries
    /// <c>suggestOtp</c> so the Kiosk knows whether retrying the camera can help
    /// (bad frame) or whether the user should switch to the Entry OTP fallback.
    /// </summary>
    public async Task<IResult> VerifyFaceAsync(
        KioskVerifyFaceRequest? request,
        CancellationToken cancellationToken)
    {
        var roomId = request?.RoomId ?? Guid.Empty;
        if (roomId == Guid.Empty)
            return Results.BadRequest(new { granted = false, message = "ไม่ระบุห้องแล็บ", suggestOtp = false });

        if (!FaceImage.TryDecode(
                request?.ImageBase64,
                out var image,
                out var contentType,
                out var imageMessage))
        {
            return Results.Ok(new { granted = false, message = imageMessage, suggestOtp = false });
        }

        var nowUtc = DateTime.UtcNow;
        if (attemptLimiter.GetLockRemaining(FaceScope, roomId, nowUtc) is TimeSpan remaining)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
            return Results.Json(
                new
                {
                    granted = false,
                    message = $"สแกนใบหน้าไม่ผ่านหลายครั้งเกินไป กรุณาใช้รหัสฉุกเฉินจากเว็บ หรือรอ {minutes} นาที",
                    suggestOtp = true,
                    serviceError = true
                },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        float[] probe;
        try
        {
            probe = await faceEmbeddingClient.CreateEmbeddingAsync(image, contentType, cancellationToken);
        }
        catch (FaceServiceException exception)
        {
            // 400 means the frame itself was unusable (no face / more than one face), so
            // another scan is the right next step. Anything else is a Face Service
            // problem — docs/AUTH_ADAPTIVE.md sends those straight to the OTP fallback.
            var serviceDown = exception.StatusCode != StatusCodes.Status400BadRequest;
            return Results.Ok(new
            {
                granted = false,
                message = exception.Message,
                suggestOtp = serviceDown,
                serviceError = serviceDown
            });
        }

        var match = await faceMatchingService.IdentifyAsync(probe, cancellationToken);
        if (match is null)
            return FailFace(roomId, nowUtc, "ยังไม่มีใบหน้าที่ลงทะเบียนไว้ในระบบ กรุณาใช้รหัสจากเว็บ", null);
        if (!match.Matched)
        {
            return FailFace(
                roomId,
                nowUtc,
                "ไม่พบใบหน้าที่ตรงกับผู้ใช้ในระบบ กรุณาลองสแกนใหม่ หรือใช้รหัสจากเว็บ",
                match.Score);
        }

        var decision = await entryService.AuthorizeAndAssignSeatAsync(
            match.UserId,
            roomId,
            AuthMethod.Face,
            cancellationToken);

        if (!decision.Granted)
        {
            logger.LogInformation(
                "Kiosk face entry denied for user {UserId} at room {RoomId} (score {Score:F3}): {Reason}",
                match.UserId,
                roomId,
                match.Score,
                decision.Message);
            // The person was recognised, so an OTP would hit the same entitlement check.
            return Results.Ok(new
            {
                granted = false,
                message = decision.Message,
                suggestOtp = false,
                identified = true
            });
        }

        logger.LogInformation(
            "Kiosk face entry granted for user {UserId} at room {RoomId} (score {Score:F3})",
            match.UserId,
            roomId,
            match.Score);
        attemptLimiter.Reset(FaceScope, roomId);
        return Granted(decision);
    }

    private static IResult Granted(EntryDecision decision) =>
        Results.Ok(new
        {
            granted = true,
            user = new { displayName = decision.DisplayName, username = decision.Username },
            room = new { id = decision.RoomId, name = decision.RoomName, building = decision.Building },
            seatNumber = decision.SeatNumber,
            seatLabel = decision.SeatLabel,
            computerName = decision.ComputerName
        });

    /// <summary>
    /// Codes are stored as BCrypt hashes, so every unexpired unused OTP has to be
    /// verified one by one.
    /// </summary>
    private async Task<EntryOtp?> FindMatchingOtpAsync(
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var candidates = await db.EntryOtps
            .Where(x => x.UsedAt == null && x.ExpiresAt > nowUtc)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxCandidateOtps)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(candidate =>
            BCrypt.Net.BCrypt.Verify(code, candidate.CodeHash));
    }

    private IResult FailOtp(Guid roomId, DateTime nowUtc, string message)
    {
        attemptLimiter.RecordFailure(OtpScope, roomId, nowUtc);
        logger.LogInformation("Kiosk entry OTP verification failed at room {RoomId}", roomId);
        return Results.Ok(new { granted = false, message });
    }

    private IResult FailFace(Guid roomId, DateTime nowUtc, string message, double? bestScore)
    {
        attemptLimiter.RecordFailure(FaceScope, roomId, nowUtc);
        logger.LogInformation(
            "Kiosk face identification failed at room {RoomId} (best score {Score})",
            roomId,
            bestScore?.ToString("F3") ?? "none");
        return Results.Ok(new { granted = false, message, suggestOtp = true });
    }
}
