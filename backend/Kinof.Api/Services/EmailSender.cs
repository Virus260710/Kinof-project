using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Kinof.Api.Services;

public sealed class EmailOptions
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@kinof.local";
    public string FromName { get; set; } = "KINOF Lab System";
    /// <summary>When true in Development, skip Resend/SMTP and print OTP in the API console (and on the OTP page).</summary>
    public bool SkipSmtpInDevelopment { get; set; }
}

public interface IEmailSender
{
    Task<EmailDeliveryResult> SendLoginOtpAsync(
        string email,
        string firstName,
        string code,
        CancellationToken cancellationToken);
    Task<EmailDeliveryResult> SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetLink,
        CancellationToken cancellationToken);
    Task<EmailDeliveryResult> SendAdminInviteEmailAsync(
        string email,
        string firstName,
        string inviteLink,
        CancellationToken cancellationToken);
    Task<EmailDeliveryResult> SendEntryOtpAsync(
        string email,
        string firstName,
        string code,
        string? roomName,
        CancellationToken cancellationToken);
    Task<EmailDeliveryResult> SendGroupInvitationEmailAsync(
        string email,
        string firstName,
        string inviterName,
        string roomName,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        string appLink,
        CancellationToken cancellationToken);
}

public sealed record EmailDeliveryResult(bool Delivered, string Mode);

public sealed class EmailDeliveryException : InvalidOperationException
{
    public EmailDeliveryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public static class EmailDelivery
{
    public const string SmtpMode = "smtp";
    public const string ConsoleMode = "console";
    public const string FailedMode = "failed";

    public static IResult FailedResult() =>
        Results.Json(
            new { message = "ส่งอีเมลไม่สำเร็จ กรุณาลองใหม่ภายหลัง" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
}

public sealed class EmailSender(
    IOptions<EmailOptions> options,
    ILogger<EmailSender> logger,
    IHostEnvironment environment) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailDeliveryResult> SendLoginOtpAsync(
        string email,
        string firstName,
        string code,
        CancellationToken cancellationToken)
    {
        if (!IsSmtpConfigured())
            return DevFallbackOrThrow(
                "Development email fallback: login OTP for {Email} is {OtpCode} (valid 10 minutes)",
                email,
                code);

        var message = BuildMessage(
            email,
            "รหัส OTP เข้าสู่ระบบ KINOF",
            $"""
            สวัสดี {firstName},

            รหัส OTP ของคุณคือ: {code}
            ใช้ได้ 10 นาที ห้ามแชร์ให้ผู้อื่น

            — KINOF ระบบจองห้องแล็บ
            """);

        return await SendSmtpOrDevConsoleFallbackAsync(
            message,
            $"Development email fallback (SMTP send failed): login OTP for {email} is {code} (valid 10 minutes)",
            cancellationToken);
    }

    public async Task<EmailDeliveryResult> SendPasswordResetEmailAsync(
        string email,
        string firstName,
        string resetLink,
        CancellationToken cancellationToken)
    {
        if (!IsSmtpConfigured())
            return DevFallbackOrThrow(
                "Development email fallback: password reset link for {Email} is {ResetLink} (valid 1 hour)",
                email,
                resetLink);

        var message = BuildMessage(
            email,
            "รีเซ็ตรหัสผ่าน KINOF",
            $"""
            สวัสดี {firstName},

            เปิดลิงก์ด้านล่างเพื่อตั้งรหัสผ่านใหม่:
            {resetLink}

            ลิงก์นี้ใช้ได้ 1 ชั่วโมง หากคุณไม่ได้ส่งคำขอนี้ สามารถละเว้นอีเมลฉบับนี้ได้

            — KINOF ระบบจองห้องแล็บ
            """);

        return await SendSmtpOrDevConsoleFallbackAsync(
            message,
            $"Development email fallback (SMTP send failed): password reset link for {email} is {resetLink} (valid 1 hour)",
            cancellationToken);
    }

    public async Task<EmailDeliveryResult> SendAdminInviteEmailAsync(
        string email,
        string firstName,
        string inviteLink,
        CancellationToken cancellationToken)
    {
        if (!IsSmtpConfigured())
            return DevFallbackOrThrow(
                "Development email fallback: admin invite link for {Email} is {InviteLink} (valid 48 hours)",
                email,
                inviteLink);

        var message = BuildMessage(
            email,
            "ตั้งรหัสผ่านผู้ดูแลระบบ KINOF",
            $"""
            สวัสดี {firstName},

            Superadmin ได้สร้างบัญชีผู้ดูแลระบบให้คุณแล้ว
            เปิดลิงก์ด้านล่างเพื่อตั้งรหัสผ่าน:
            {inviteLink}

            ลิงก์นี้ใช้ได้ 48 ชั่วโมง หลังจากนั้นเข้าสู่ระบบด้วยรหัสผ่านและ OTP อีเมล

            — KINOF ระบบจองห้องแล็บ
            """);

        return await SendSmtpOrDevConsoleFallbackAsync(
            message,
            $"Development email fallback (SMTP send failed): admin invite link for {email} is {inviteLink} (valid 48 hours)",
            cancellationToken);
    }

    public async Task<EmailDeliveryResult> SendEntryOtpAsync(
        string email,
        string firstName,
        string code,
        string? roomName,
        CancellationToken cancellationToken)
    {
        var roomLine = string.IsNullOrWhiteSpace(roomName)
            ? ""
            : $"ห้อง: {roomName}{Environment.NewLine}";
        if (!IsSmtpConfigured())
            return DevFallbackOrThrow(
                "Development email fallback: entry OTP for {Email} is {OtpCode} (valid 10 minutes{RoomSuffix})",
                email,
                code,
                string.IsNullOrWhiteSpace(roomName) ? "" : $", room: {roomName}");

        var message = BuildMessage(
            email,
            "รหัสเข้าห้องแล็บ KINOF (สำรอง)",
            $"""
            สวัสดี {firstName},

            รหัสเข้าห้องแล็บของคุณคือ: {code}
            ใช้ได้ 10 นาที ครั้งเดียว ห้ามแชร์ให้ผู้อื่น

            ใช้เมื่อสแกนหน้าไม่สำเร็จที่ Kiosk เท่านั้น
            {roomLine}
            — KINOF ระบบจองห้องแล็บ
            """);

        var roomLog = string.IsNullOrWhiteSpace(roomName) ? "" : $", room: {roomName}";
        return await SendSmtpOrDevConsoleFallbackAsync(
            message,
            $"Development email fallback (SMTP send failed): entry OTP for {email} is {code} (valid 10 minutes{roomLog})",
            cancellationToken);
    }

    public async Task<EmailDeliveryResult> SendGroupInvitationEmailAsync(
        string email,
        string firstName,
        string inviterName,
        string roomName,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        string appLink,
        CancellationToken cancellationToken)
    {
        var bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, bangkok);
        var endLocal = TimeZoneInfo.ConvertTimeFromUtc(endTimeUtc, bangkok);
        var timeRange =
            $"{startLocal:dd/MM/yyyy HH:mm} – {endLocal:HH:mm} น. (เวลาไทย)";

        if (!IsSmtpConfigured())
            return DevFallbackOrThrow(
                "Development email fallback: group invitation for {Email} from {Inviter} — room {RoomName}, {TimeRange}. Open {AppLink} and go to คำเชิญ",
                email,
                inviterName,
                roomName,
                timeRange,
                appLink);

        var message = BuildMessage(
            email,
            $"คำเชิญเข้าร่วมจองห้อง {roomName} — KINOF",
            $"""
            สวัสดี {firstName},

            {inviterName} เชิญคุณเข้าร่วมจองห้องแล็บ
            ห้อง: {roomName}
            เวลา: {timeRange}

            เข้าสู่ระบบ KINOF แล้วเปิดเมนู "คำเชิญ" เพื่อยอมรับหรือปฏิเสธ:
            {appLink}

            — KINOF ระบบจองห้องแล็บ
            """);

        return await SendSmtpOrDevConsoleFallbackAsync(
            message,
            $"Development email fallback (SMTP send failed): group invitation for {email} from {inviterName} — room {roomName}, {timeRange}. Open {appLink} and go to คำเชิญ",
            cancellationToken);
    }

    private bool IsSmtpConfigured()
    {
        if (environment.IsDevelopment() && _options.SkipSmtpInDevelopment)
            return false;

        return !string.IsNullOrWhiteSpace(_options.SmtpHost) &&
            !string.IsNullOrWhiteSpace(_options.Username) &&
            !string.IsNullOrWhiteSpace(_options.Password);
    }

    private EmailDeliveryResult DevFallbackOrThrow(string messageTemplate, params object?[] args)
    {
        if (!environment.IsDevelopment())
            throw new EmailDeliveryException(
                "Email SMTP credentials must be configured with user-secrets or environment variables outside Development.");

        logger.LogWarning(messageTemplate, args);
        return new EmailDeliveryResult(false, EmailDelivery.ConsoleMode);
    }

    private MimeMessage BuildMessage(string toEmail, string subject, string body)
    {
        var fromAddress = string.IsNullOrWhiteSpace(_options.FromAddress)
            ? _options.Username.Trim()
            : _options.FromAddress.Trim();
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        return message;
    }

    private async Task<EmailDeliveryResult> SendSmtpOrDevConsoleFallbackAsync(
        MimeMessage message,
        string devFallbackLogMessage,
        CancellationToken cancellationToken)
    {
        var host = _options.SmtpHost.Trim();
        var username = _options.Username.Trim();
        var password = _options.Password.Trim();
        var socketOptions = _options.SmtpPort == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, _options.SmtpPort, socketOptions, cancellationToken);

            // Some hosts advertise XOAUTH2; API keys / app passwords use SMTP AUTH.
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(username, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation(
                "SMTP email sent subject {Subject} to {RecipientCount} recipient(s)",
                message.Subject,
                message.To.Count);
            return new EmailDeliveryResult(true, EmailDelivery.SmtpMode);
        }
        catch (Exception exception)
        {
            if (!environment.IsDevelopment())
                throw new EmailDeliveryException("SMTP email delivery failed.", exception);

            logger.LogWarning(
                exception,
                "{DevFallbackMessage}",
                devFallbackLogMessage);
            return new EmailDeliveryResult(false, EmailDelivery.ConsoleMode);
        }
    }
}
