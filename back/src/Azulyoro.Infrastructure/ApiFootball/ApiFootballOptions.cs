namespace Azulyoro.Infrastructure.ApiFootball;

public class ApiFootballOptions
{
    public const string SectionName = "ApiFootball";

    public string Key { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://v3.football.api-sports.io";

    /// <summary>Retry attempts on 429/5xx.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Base backoff used when the response has no Retry-After header.</summary>
    public int RetryBaseDelayMs { get; set; } = 500;

    /// <summary>Per-request timeout (seconds).</summary>
    public int RequestTimeoutSeconds { get; set; } = 20;
}
