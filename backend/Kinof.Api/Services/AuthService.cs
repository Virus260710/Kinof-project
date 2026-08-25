using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Kinof.Api.Services;

public sealed record LoginRequest(string Username, string Password);
public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserType,
    string? StudentId,
    string? Phone);
public sealed record VerifyEmailOtpRequest(Guid UserId, string Code);
public sealed record ResendEmailOtpRequest(Guid UserId);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RegisterFaceRequest(float[] Embedding);

public sealed class AuthService(
    AppDbContext db,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<AuthService> logger)
{
    public async Task<IResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Username.ToLower() == identifier || x.Email.ToLower() == identifier,
            cancellationToken);

        if (user is null ||
            user.Status != UserStatus.Active ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Results.Json(
                new { message = "ชื่อผู้ใช้/อีเมล หรือรหัสผ่านไม่ถูกต้อง" },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var sendResult = await TrySendOtpAsync(user, cancellationToken);
        if (!sendResult.Allowed)
            return Results.Json(
                new { message = "ส่ง OTP เกิน 3 ครั้งต่อชั่วโมง กรุณาลองใหม่ภายหลัง" },
                statusCode: StatusCodes.Status429TooManyRequests);

        return Results.Ok(new
        {
            requiresOtp = true,
            userId = user.Id,
            maskedEmail = MaskEmail(user.Email),
            deliveryMode = sendResult.DeliveryMode
        });
    }

    public async Task<IResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(username, "^[a-z0-9._-]{3,50}$"))
            return ValidationError("ชื่อผู้ใช้ต้องมี 3-50 ตัว และใช้ a-z, 0-9, จุด ขีดกลาง หรือขีดล่าง");
        if (!MailAddress.TryCreate(email, out _))
            return ValidationError("รูปแบบอีเมลไม่ถูกต้อง");
        if (request.Password.Length < 8)
            return ValidationError("รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร");
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return ValidationError("กรุณากรอกชื่อและนามสกุล");

        var userType = request.UserType.Trim().ToLowerInvariant() switch
        {
            "student" => UserType.Student,
            "external" => UserType.External,
            _ => (UserType?)null
        };
        if (userType is null)
            return ValidationError("ประเภทผู้ใช้ต้องเป็นนักศึกษาหรือบุคคลภายนอก");
        if (userType == UserType.Student && string.IsNullOrWhiteSpace(request.StudentId))
            return ValidationError("กรุณากรอกรหัสนักศึกษา");

        if (await db.Users.AnyAsync(
                x => x.Username.ToLower() == username || x.Email.ToLower() == email,
                cancellationToken))
        {
            return Results.Conflict(new { message = "ชื่อผู้ใช้หรืออีเมลนี้ถูกใช้งานแล้ว" });
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            StudentId = string.IsNullOrWhiteSpace(request.StudentId) ? null : request.StudentId.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            UserType = userType.Value
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var sendResult = await TrySendOtpAsync(user, cancellationToken);
        return Results.Ok(new
        {
            requiresOtp = true,
            userId = user.Id,
            maskedEmail = MaskEmail(user.Email),
            deliveryMode = sendResult.DeliveryMode
        });
    }

    public async Task<IResult> VerifyOtpAsync(
        VerifyEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Code.Length != 6 || request.Code.Any(character => !char.IsDigit(character)))
            return InvalidOtp();

        var now = DateTime.UtcNow;
        var otp = await db.EmailOtps
            .Where(x =>
                x.UserId == request.UserId &&
                (x.Purpose == "login" || x.Purpose == "login_resend") &&
                x.UsedAt == null &&
                x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || !BCrypt.Net.BCrypt.Verify(request.Code, otp.CodeHash))
            return InvalidOtp();

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == request.UserId && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.Unauthorized();

        otp.UsedAt = now;
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = now.AddDays(7)
        });
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            accessToken = CreateAccessToken(user),
            refreshToken,
            user = ToResponse(user)
        });
    }

    public async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
            return Results.Unauthorized();

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == userId && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(ToResponse(user));
    }

    public async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Results.Unauthorized();

        var now = DateTime.UtcNow;
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await db.RefreshTokens.SingleOrDefaultAsync(
            x => x.TokenHash == tokenHash &&
                 x.RevokedAt == null &&
                 x.ExpiresAt > now,
            cancellationToken);
        if (storedToken is null)
            return Results.Unauthorized();

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == storedToken.UserId && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.Unauthorized();

        storedToken.RevokedAt = now;
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = now.AddDays(7)
        });
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            accessToken = CreateAccessToken(user),
            refreshToken,
            user = ToResponse(user)
        });
    }

    public async Task<IResult> RegisterFaceAsync(
        Guid userId,
        RegisterFaceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Embedding is null || request.Embedding.Length != 512)
            return ValidationError("ต้องส่ง face embedding 512 มิติ");

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == userId && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.NotFound(new { message = "ไม่พบบัญชีผู้ใช้" });

        var embeddingJson = JsonSerializer.Serialize(request.Embedding);
        var existing = await db.FaceEmbeddings.SingleOrDefaultAsync(
            x => x.UserId == userId,
            cancellationToken);
        if (existing is null)
        {
            db.FaceEmbeddings.Add(new FaceEmbedding
            {
                UserId = userId,
                Embedding = embeddingJson,
                IsPrimary = true
            });
        }
        else
        {
            existing.Embedding = embeddingJson;
            existing.IsPrimary = true;
        }

        user.FaceEnrolled = true;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { ok = true, faceEnrolled = true, user = ToResponse(user) });
    }

    public async Task<IResult> ResendOtpAsync(
        ResendEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == request.UserId && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.NotFound(new { message = "ไม่พบบัญชีผู้ใช้" });

        var sent = await TrySendOtpAsync(user, cancellationToken, isResend: true);
        if (!sent.Allowed)
            return Results.Json(
                new { message = "ส่ง OTP เกิน 3 ครั้งต่อชั่วโมง กรุณาลองใหม่ภายหลัง" },
                statusCode: StatusCodes.Status429TooManyRequests);

        return Results.Ok(new
        {
            ok = true,
            maskedEmail = MaskEmail(user.Email),
            deliveryMode = sent.DeliveryMode
        });
    }

    private async Task<OtpSendResult> TrySendOtpAsync(
        User user,
        CancellationToken cancellationToken,
        bool isResend = false)
    {
        var now = DateTime.UtcNow;
        if (isResend)
        {
            var sentLastHour = await db.EmailOtps.CountAsync(
                x => x.UserId == user.Id &&
                     x.Purpose == "login_resend" &&
                     x.CreatedAt >= now.AddHours(-1),
                cancellationToken);
            if (sentLastHour >= 3)
                return new OtpSendResult(false, "rate_limited");
        }

        var activeOtps = await db.EmailOtps
            .Where(x =>
                x.UserId == user.Id &&
                (x.Purpose == "login" || x.Purpose == "login_resend") &&
                x.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var activeOtp in activeOtps)
            activeOtp.UsedAt = now;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        db.EmailOtps.Add(new EmailOtp
        {
            UserId = user.Id,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code, workFactor: 10),
            Purpose = isResend ? "login_resend" : "login",
            ExpiresAt = now.AddMinutes(10)
        });
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var delivery = await emailSender.SendLoginOtpAsync(
                user.Email,
                user.FirstName,
                code,
                cancellationToken);
            logger.LogInformation("Login OTP delivery mode for {MaskedEmail}: {Mode}",
                MaskEmail(user.Email),
                delivery.Mode);
            return new OtpSendResult(true, delivery.Mode);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Login OTP email delivery failed for {MaskedEmail}",
                MaskEmail(user.Email));
            return new OtpSendResult(true, "failed");
        }
    }

    private string CreateAccessToken(User user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "Kinof.Api";
        var audience = configuration["Jwt:Audience"] ?? "Kinof.App";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.UserType.ToString().ToLowerInvariant())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials));
    }

    private static object ToResponse(User user) => new
    {
        id = user.Id,
        username = user.Username,
        email = user.Email,
        firstName = user.FirstName,
        lastName = user.LastName,
        userType = user.UserType.ToString().ToLowerInvariant(),
        status = user.Status.ToString().ToLowerInvariant(),
        faceEnrolled = user.FaceEnrolled
    };

    private static IResult InvalidOtp() => Results.Json(
        new { message = "OTP ไม่ถูกต้องหรือหมดอายุแล้ว" },
        statusCode: StatusCodes.Status400BadRequest);

    private static IResult ValidationError(string message) =>
        Results.BadRequest(new { message });

    private sealed record OtpSendResult(bool Allowed, string DeliveryMode);

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2)
            return "***";

        var visible = parts[0].Length == 0 ? "" : parts[0][..1];
        return $"{visible}***@{parts[1]}";
    }

    public static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}
