using Microsoft.Extensions.Logging;

namespace Azulyoro.Infrastructure.Email;

/// <summary>Dev-safe email sender: logs the message instead of sending it.
/// Registered when <c>Brevo:ApiKey</c> is empty so the API is verifiable
/// without external credentials.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV EMAIL] To={To} Subject={Subject}\n{Body}",
            to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
