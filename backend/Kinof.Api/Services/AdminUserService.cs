using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kinof.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kinof.Api.Services;

public sealed record CreateAdminRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Phone);

public sealed record UpdateAdminRequest(
    string FirstName,
    string LastName,
    string Email,
    string? JobTitle,
    string? Phone);

public sealed class AdminUserService(
    AppDbContext db,
    IEmailSender emailSender,
    IConfiguration configuration,
    AuditLogService auditLog,
    ILogger<AdminUserService> logger)
{
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var admins = await db.Users.AsNoTracking()
            .Where(x => x.UserType == UserType.Admin)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Select(x => ToAdminResponse(x))
            .ToListAsync(cancellationToken);
        return Results.Ok(admins);
    }

    public async Task<IResult> CreateAsync(
        Guid actorUserId,
        CreateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(username, "^[a-z0-9._-]{3,50}$"))
            return Results.BadRequest(new { message = "ชื่อผู้ใช้ต้องมี 3-50 ตัว และใช้ a-z, 0-9, จุด ขีดกลาง หรือขีดล่าง" });
        if (!MailAddress.TryCreate(email, out _))
            return Results.BadRequest(new { message = "รูปแบบอีเมลไม่ถูกต้อง" });
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Results.BadRequest(new { message = "กรุณากรอกชื่อและนามสกุล" });

        if (await db.Users.AnyAsync(
                x => x.Username.ToLower() == username || x.Email.ToLower() == email,
                cancellationToken))
        {
            return Results.Conflict(new { message = "ชื่อผู้ใช้หรืออีเมลนี้ถูกใช้งานแล้ว" });
        }

        var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(randomPassword, workFactor: 12),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            UserType = UserType.Admin,
            FaceEnrolled = false
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var invite = await SendInviteAsync(user, cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "admin.create",
            "user",
            user.Id.ToString(),
            $"{user.Username} ({user.Email})",
            cancellationToken);

        return Results.Ok(new
        {
            admin = ToAdminResponse(user),
            inviteSent = invite.Delivered,
            deliveryMode = invite.Mode
        });
    }

    public async Task<IResult> UpdateAsync(
        Guid actorUserId,
        Guid adminId,
        UpdateAdminRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == adminId && x.UserType == UserType.Admin,
            cancellationToken);
        if (user is null)
            return Results.NotFound(new { message = "ไม่พบบัญชีผู้ดูแลระบบ" });

        var email = request.Email.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(email, out _))
            return Results.BadRequest(new { message = "รูปแบบอีเมลไม่ถูกต้อง" });
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Results.BadRequest(new { message = "กรุณากรอกชื่อและนามสกุล" });
        if (await db.Users.AnyAsync(
                x => x.Id != adminId && x.Email.ToLower() == email,
                cancellationToken))
        {
            return Results.Conflict(new { message = "อีเมลนี้ถูกใช้งานแล้ว" });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "admin.update",
            "user",
            user.Id.ToString(),
            user.Username,
            cancellationToken);
        return Results.Ok(ToAdminResponse(user));
    }

    public async Task<IResult> SetStatusAsync(
        Guid actorUserId,
        Guid adminId,
        UserStatus status,
        CancellationToken cancellationToken)
    {
        if (actorUserId == adminId)
            return Results.BadRequest(new { message = "ไม่สามารถปิดบัญชีของตนเองได้" });

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == adminId && x.UserType == UserType.Admin,
            cancellationToken);
        if (user is null)
            return Results.NotFound(new { message = "ไม่พบบัญชีผู้ดูแลระบบ" });

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        if (status == UserStatus.Disabled)
        {
            var tokens = await db.RefreshTokens
                .Where(x => x.UserId == user.Id && x.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in tokens)
                token.RevokedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            status == UserStatus.Disabled ? "admin.disable" : "admin.enable",
            "user",
            user.Id.ToString(),
            user.Username,
            cancellationToken);
        return Results.Ok(ToAdminResponse(user));
    }

    public async Task<IResult> ResendInviteAsync(
        Guid actorUserId,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == adminId && x.UserType == UserType.Admin && x.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
            return Results.NotFound(new { message = "ไม่พบบัญชีผู้ดูแลระบบที่ใช้งานได้" });

        var invite = await SendInviteAsync(user, cancellationToken);
        await auditLog.WriteAsync(
            actorUserId,
            "admin.invite_resend",
            "user",
            user.Id.ToString(),
            user.Username,
            cancellationToken);
        return Results.Ok(new { ok = true, deliveryMode = invite.Mode });
    }

    private async Task<EmailDeliveryResult> SendInviteAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await db.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
            token.UsedAt = now;

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))),
            ExpiresAt = now.AddHours(48)
        });
        await db.SaveChangesAsync(cancellationToken);

        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(raw)}";
        try
        {
            return await emailSender.SendAdminInviteEmailAsync(
                user.Email,
                user.FirstName,
                resetLink,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Admin invite email failed for {Email}", user.Email);
            return new EmailDeliveryResult(false, "failed");
        }
    }

    private static object ToAdminResponse(User user) => new
    {
        id = user.Id,
        username = user.Username,
        email = user.Email,
        firstName = user.FirstName,
        lastName = user.LastName,
        jobTitle = user.JobTitle,
        phone = user.Phone,
        status = user.Status.ToString().ToLowerInvariant(),
        createdAt = user.CreatedAt,
        updatedAt = user.UpdatedAt
    };
}
