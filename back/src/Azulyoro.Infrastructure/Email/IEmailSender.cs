namespace Azulyoro.Infrastructure.Email;

/// <summary>Transactional email abstraction. Implementations must not throw
/// on delivery failure paths that should be swallowed by callers; they return
/// after best-effort delivery and log failures.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
