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
}

public sealed record EmailDeliveryResult(bool Delivered, string Mode);

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
        var smtpConfigured =
            !string.IsNullOrWhiteSpace(_options.SmtpHost) &&
            !string.IsNullOrWhiteSpace(_options.Username) &&
            !string.IsNullOrWhiteSpace(_options.Password);
        if (!smtpConfigured)
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Email SMTP credentials must be configured outside Development.");

            logger.LogWarning(
                "Development email fallback: login OTP for {Email} is {OtpCode} (valid 10 minutes)",
                email,
                code);
            return new EmailDeliveryResult(false, "console");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "รหัส OTP เข้าสู่ระบบ KINOF";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                    สวัสดี {firstName},

                    รหัส OTP ของคุณคือ: {code}
                    ใช้ได้ 10 นาที ห้ามแชร์ให้ผู้อื่น

                    — KINOF ระบบจองห้องแล็บ
                    """
        };

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
        var smtpConfigured =
            !string.IsNullOrWhiteSpace(_options.SmtpHost) &&
            !string.IsNullOrWhiteSpace(_options.Username) &&
            !string.IsNullOrWhiteSpace(_options.Password);
        if (!smtpConfigured)
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Email SMTP credentials must be configured outside Development.");

            logger.LogWarning(
                "Development email fallback: password reset link for {Email} is {ResetLink} (valid 1 hour)",
                email,
                resetLink);
            return new EmailDeliveryResult(false, "console");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "รีเซ็ตรหัสผ่าน KINOF";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                    สวัสดี {firstName},

                    เปิดลิงก์ด้านล่างเพื่อตั้งรหัสผ่านใหม่:
                    {resetLink}

                    ลิงก์นี้ใช้ได้ 1 ชั่วโมง หากคุณไม่ได้ส่งคำขอนี้ สามารถละเว้นอีเมลฉบับนี้ได้

                    — KINOF ระบบจองห้องแล็บ
                    """
        };

        return await SendSmtpOrDevConsoleFallbackAsync(
            message,
            $"Development email fallback (SMTP send failed): password reset link for {email} is {resetLink} (valid 1 hour)",
            cancellationToken);
    }

    private async Task<EmailDeliveryResult> SendSmtpOrDevConsoleFallbackAsync(
        MimeMessage message,
        string devFallbackLogMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.SmtpHost,
                _options.SmtpPort,
                SecureSocketOptions.StartTlsWhenAvailable,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return new EmailDeliveryResult(true, "smtp");
        }
        catch (Exception exception)
        {
            if (!environment.IsDevelopment())
                throw;

            logger.LogWarning(
                exception,
                "{DevFallbackMessage}",
                devFallbackLogMessage);
            return new EmailDeliveryResult(false, "console");
        }
    }
}
