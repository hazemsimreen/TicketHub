using BusinessLogic.Auth;
using Microsoft.Extensions.Logging;

namespace API.Services;

/// <summary>
/// Development-only IEmailSender.
/// Logs email subject and body (including reset/confirmation links) to the terminal.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _log;
    public ConsoleEmailSender(ILogger<ConsoleEmailSender> log) => _log = log;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _log.LogWarning("📧 EMAIL [Development Mode]\nTo:      {To}\nSubject: {Subject}\n\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
