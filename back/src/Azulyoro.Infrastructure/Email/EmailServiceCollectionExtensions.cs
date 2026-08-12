using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Azulyoro.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>Registers <see cref="BrevoEmailSender"/> in production. The
    /// logging sender is intentionally available only in development so that
    /// verification and password-reset links never land in production logs.</summary>
    public static IServiceCollection AddEmailSender(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var apiKey = configuration[$"{EmailOptions.SectionName}:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Brevo:ApiKey must be configured in Production; refusing to log sensitive email links.");
            }

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
