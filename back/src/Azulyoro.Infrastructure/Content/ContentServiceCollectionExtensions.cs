using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Azulyoro.Infrastructure.Content;

public static class ContentServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RSS scraper as a typed HttpClient with a realistic bot
    /// User-Agent. Call from <c>AddInfrastructure</c>.
    /// </summary>
    public static IServiceCollection AddContentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ContentScrapeOptions>(
            configuration.GetSection(ContentScrapeOptions.SectionName));

        services.AddHttpClient<RssScraperService>((sp, http) =>
        {
            var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ContentScrapeOptions>>().Value;
            http.Timeout = TimeSpan.FromSeconds(opt.RequestTimeoutSeconds);
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opt.UserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/xml;q=0.9, text/xml;q=0.8");
        });

        services.AddHttpClient<IFrontendRevalidator, FrontendRevalidator>();

        return services;
    }
}
