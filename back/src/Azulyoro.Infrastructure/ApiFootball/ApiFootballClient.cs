using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Azulyoro.Infrastructure.ApiFootball;

public class ApiFootballClient(HttpClient http, ILogger<ApiFootballClient> logger)
    : IApiFootballClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ApiFootballResponse<T>> GetAsync<T>(
        string endpoint,
        IReadOnlyDictionary<string, string?>? query = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(endpoint, query);

        using var response = await http.GetAsync(url, cancellationToken);
        LogRateLimit(endpoint, response);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ApiFootballResponse<T>>(
            stream, JsonOptions, cancellationToken);

        if (payload is null)
        {
            throw new InvalidOperationException($"API-Football returned an empty body for '{endpoint}'.");
        }

        if (payload.HasErrors)
        {
            logger.LogWarning("API-Football '{Endpoint}' returned errors: {Errors}",
                endpoint, payload.ErrorText);
        }

        return payload;
    }

    private static string BuildUrl(string endpoint, IReadOnlyDictionary<string, string?>? query)
    {
        var path = endpoint.TrimStart('/');
        if (query is null || query.Count == 0)
        {
            return path;
        }

        var pairs = query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        return $"{path}?{string.Join('&', pairs)}";
    }

    private void LogRateLimit(string endpoint, HttpResponseMessage response)
    {
        // Daily and per-minute quota headers (API-Football).
        if (response.Headers.TryGetValues("x-ratelimit-requests-remaining", out var daily))
        {
            var perMinute = response.Headers.TryGetValues("X-RateLimit-Remaining", out var pm)
                ? pm.FirstOrDefault()
                : "?";
            logger.LogDebug(
                "API-Football '{Endpoint}' quota — daily remaining: {Daily}, per-minute remaining: {Minute}",
                endpoint, daily.FirstOrDefault(), perMinute);
        }
    }
}
