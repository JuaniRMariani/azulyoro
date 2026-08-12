namespace Azulyoro.Infrastructure.Email;

/// <summary>Brevo/transactional email config. When <see cref="ApiKey"/> is
/// empty the app registers <see cref="LoggingEmailSender"/> instead.</summary>
public class EmailOptions
{
    public const string SectionName = "Brevo";

    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@azulyoro.com.ar";
    public string FromName { get; set; } = "Azul y Oro";
}
