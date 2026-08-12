namespace Azulyoro.Infrastructure.Content;

/// <summary>Tunables for the RSS scraper (User-Agent, timeouts, excerpt length).</summary>
public class ContentScrapeOptions
{
    public const string SectionName = "ContentScrape";

    public string UserAgent { get; set; } = "AzulYOroBot/1.0 (+https://azulyoro.com.ar/bot)";
    public int RequestTimeoutSeconds { get; set; } = 20;

    /// <summary>Max sanitized excerpt length (chars) stored per staging row.</summary>
    public int ExcerptMaxLength { get; set; } = 280;

    /// <summary>Upper bound of random jitter added to per-source politeness delay (ms).</summary>
    public int JitterMaxMs { get; set; } = 750;
}
