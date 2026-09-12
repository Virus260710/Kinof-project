using System.Security.Cryptography;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record EntryOtpRequestBody(Guid? RoomId);

public sealed class EntryOtpService(
    AppDbContext db,
    IEmailSender emailSender,
    ILogger<EntryOtpService> logger)
{
    public const int HourlyLimit = 3;
    public const int MonthlyLimit = 5;

    public Task<IResult> RequestAsync(
        Guid userId,
        Guid? roomId,
        CancellationToken cancellationToken) =>
        SendAsync(userId, roomId, cancellationToken);

    public Task<IResult> ResendAsync(
        Guid userId,
        Guid? roomId,
        CancellationToken cancellationToken) =>
        SendAsync(userId, roomId, cancellationToken);

    public async Task<IResult> GetActiveAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == userId && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.Unauthorized();

        var now = DateTime.UtcNow;
        var quota = await GetMonthlyQuotaAsync(userId, now, cancellationToken);
        var otp = await db.EntryOtps
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.UsedAt == null && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
            return Results.Ok(new
            {
                hasActive = false,
                monthlyUsed = quota.Used,
                monthlyLimit = quota.Limit,
                monthlyRemaining = quota.Remaining
            });

        string? roomName = null;
        if (otp.RoomId is Guid roomId)
        {
            roomName = await db.Rooms.AsNoTracking()
                .Where(x => x.Id == roomId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Results.Ok(new
        {
            hasActive = true,
            expiresAt = DateTime.SpecifyKind(otp.ExpiresAt, DateTimeKind.Utc),
            roomId = otp.RoomId,
            roomName,
            maskedEmail = MaskEmail(user.Email),
            monthlyUsed = quota.Used,
            monthlyLimit = quota.Limit,
            monthlyRemaining = quota.Remaining
        });
    }

    private async Task<IResult> SendAsync(
        Guid userId,
        Guid? roomId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == userId && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.Unauthorized();

        string? roomName = null;
        if (roomId is Guid requestedRoomId)
        {
            var room = await db.Rooms.SingleOrDefaultAsync(
                x => x.Id == requestedRoomId,
                cancellationToken);
            if (room is null)
                return Results.NotFound(new { message = "ไม่พบห้องแล็บที่ระบุ" });
            if (room.Status != RoomStatus.Open)
                return Results.BadRequest(new { message = "ห้องนี้ปิดหรืออยู่ระหว่างปรับปรุง" });
            roomName = room.Name;
        }

        var now = DateTime.UtcNow;
        var monthly = await GetMonthlyQuotaAsync(user.Id, now, cancellationToken);
        if (monthly.Used >= MonthlyLimit)
            return Results.Json(
                new
                {
                    message = "ขอรหัสเข้าห้องฉุกเฉินได้ไม่เกิน 5 ครั้งต่อเดือน",
                    monthlyUsed = monthly.Used,
                    monthlyLimit = monthly.Limit,
                    monthlyRemaining = monthly.Remaining
                },
                statusCode: StatusCodes.Status429TooManyRequests);

        var sentLastHour = await db.EntryOtps.CountAsync(
            x => x.UserId == user.Id && x.CreatedAt >= now.AddHours(-1),
            cancellationToken);
        if (sentLastHour >= HourlyLimit)
            return Results.Json(
                new { message = "ส่ง OTP เกิน 3 ครั้งต่อชั่วโมง กรุณาลองใหม่ภายหลัง" },
                statusCode: StatusCodes.Status429TooManyRequests);

        var activeOtps = await db.EntryOtps
            .Where(x => x.UserId == user.Id && x.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var activeOtp in activeOtps)
            activeOtp.UsedAt = now;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        db.EntryOtps.Add(new EntryOtp
        {
            UserId = user.Id,
            RoomId = roomId,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code, workFactor: 10),
            ExpiresAt = now.AddMinutes(10)
        });
        await db.SaveChangesAsync(cancellationToken);

        var deliveryMode = "failed";
        try
        {
            var delivery = await emailSender.SendEntryOtpAsync(
                user.Email,
                user.FirstName,
                code,
                roomName,
                cancellationToken);
            deliveryMode = delivery.Mode;
            logger.LogInformation(
                "Entry OTP delivery mode for {MaskedEmail}: {Mode}",
                MaskEmail(user.Email),
                delivery.Mode);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Entry OTP email delivery failed for {MaskedEmail}",
                MaskEmail(user.Email));
        }

        var quota = await GetMonthlyQuotaAsync(user.Id, now, cancellationToken);
        return Results.Ok(new
        {
            ok = true,
            maskedEmail = MaskEmail(user.Email),
            expiresAt = now.AddMinutes(10),
            roomId,
            roomName,
            deliveryMode,
            monthlyUsed = quota.Used,
            monthlyLimit = quota.Limit,
            monthlyRemaining = quota.Remaining
        });
    }

    private async Task<MonthlyQuota> GetMonthlyQuotaAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var monthStartUtc = CurrentBangkokMonthStartUtc(nowUtc);
        var used = await db.EntryOtps.CountAsync(
            x => x.UserId == userId && x.CreatedAt >= monthStartUtc,
            cancellationToken);
        return new MonthlyQuota(used, MonthlyLimit, Math.Max(0, MonthlyLimit - used));
    }

    internal static DateTime CurrentBangkokMonthStartUtc(DateTime nowUtc)
    {
        var local = BangkokTime.ToLocal(nowUtc);
        return BangkokTime.ToUtc(new DateTime(local.Year, local.Month, 1));
    }

    private readonly record struct MonthlyQuota(int Used, int Limit, int Remaining);

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2)
            return "***";

        var visible = parts[0].Length == 0 ? "" : parts[0][..1];
        return $"{visible}***@{parts[1]}";
    }
}
