using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Azulyoro.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>Registers <see cref="BrevoEmailSender"/> when a Brevo API key
    /// is configured, otherwise the dev-safe <see cref="LoggingEmailSender"/>.</summary>
    public static IServiceCollection AddEmailSender(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var apiKey = configuration[$"{EmailOptions.SectionName}:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
            return services;
        }

        services.AddHttpClient<IEmailSender, BrevoEmailSender>((sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            http.BaseAddress = new Uri("https://api.brevo.com/");
            http.DefaultRequestHeaders.Add("api-key", opt.ApiKey);
            http.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
