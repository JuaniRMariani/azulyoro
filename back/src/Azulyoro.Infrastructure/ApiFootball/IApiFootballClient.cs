namespace Azulyoro.Infrastructure.ApiFootball;

public interface IApiFootballClient
{
    /// <summary>
    /// GET an API-Football endpoint (e.g. "teams", "fixtures") with optional
    /// query params, returning the typed envelope. Resilience (retry on
    /// 429/5xx, circuit breaker) is applied by the HttpClient pipeline.
    /// </summary>
    Task<ApiFootballResponse<T>> GetAsync<T>(
        string endpoint,
        IReadOnlyDictionary<string, string?>? query = null,
        CancellationToken cancellationToken = default);
}
