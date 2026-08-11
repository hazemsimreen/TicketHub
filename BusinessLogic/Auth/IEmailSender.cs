namespace BusinessLogic.Auth;

/// <summary>
/// Abstraction for sending emails.
/// Development: ConsoleEmailSender writes links to application logs.
/// Production: Swap for SendGrid/SMTP in Program.cs.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
