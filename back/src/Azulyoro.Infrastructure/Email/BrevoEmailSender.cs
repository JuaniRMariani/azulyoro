using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azulyoro.Infrastructure.Email;

/// <summary>Sends transactional email via the Brevo v3 SMTP API using a typed
/// <see cref="HttpClient"/>. The <c>api-key</c> header is set on the client.</summary>
public sealed class BrevoEmailSender(
    HttpClient http,
    IOptions<EmailOptions> options,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var payload = new
        {
            sender = new { email = _options.FromEmail, name = _options.FromName },
            to = new[] { new { email = to } },
            subject,
            htmlContent = htmlBody,
        };

        try
        {
            using var response = await http.PostAsJsonAsync("v3/smtp/email", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError(
                    "Brevo email to {To} failed with {Status}: {Body}",
                    to, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Brevo email to {To} threw", to);
        }
    }
}
