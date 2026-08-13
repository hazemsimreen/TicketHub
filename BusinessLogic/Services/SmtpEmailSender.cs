using System.Net;
using System.Net.Mail;
using BusinessLogic.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

/// <summary>
/// Production & Hybrid Email Sender that sends real emails via SMTP when configured in appsettings.json,
/// and falls back to logging to the console if SMTP settings are missing or disabled.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> log)
    {
        _config = config;
        _log = log;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = _config["Smtp:Host"];
        var portStr = _config["Smtp:Port"];
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:FromEmail"] ?? username ?? "noreply@tickethub.gov";
        var fromName = _config["Smtp:FromName"] ?? "TicketHub Support";

        string formattedBody = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #0056b3;'>{subject}</h2>
                <hr style='border: 0; border-top: 1px solid #eee;' />
                <p>{body}</p>
                <footer style='margin-top: 30px; font-size: 12px; color: #777;'>
                    &copy; {DateTime.UtcNow.Year} TicketHub Support. All rights reserved.
                </footer>
            </div>";

        // If SMTP credentials are configured in appsettings.json or UserSecrets, send real email!
        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            try
            {
                int port = int.TryParse(portStr, out var p) ? p : 587;
                bool enableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) ? ssl : true;

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = formattedBody,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                await client.SendMailAsync(message, ct);
                _log.LogInformation("✅ REAL EMAIL SENT via SMTP to {To} with Subject: {Subject}", to, subject);
                return;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "❌ Failed to send real email via SMTP to {To}. Falling back to console log.", to);
            }
        }

        // Fallback for development / unconfigured SMTP: Log to console
        _log.LogWarning("📧 EMAIL [Console Fallback - Configure Smtp in appsettings.json for real delivery]\nTo:      {To}\nSubject: {Subject}\n\n{Body}", to, subject, formattedBody);
    }
}
