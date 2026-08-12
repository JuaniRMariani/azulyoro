using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azulyoro.Infrastructure.Content;

public interface IFrontendRevalidator
{
    /// <summary>Ask the Next front to revalidate the given cache tags (on publish/update).</summary>
    Task RevalidateAsync(IEnumerable<string> tags, CancellationToken ct);
}

public class FrontendRevalidator(
    HttpClient http,
    IConfiguration configuration,
    ILogger<FrontendRevalidator> logger)
    : IFrontendRevalidator
{
    public async Task RevalidateAsync(IEnumerable<string> tags, CancellationToken ct)
    {
        var baseUrl = configuration["Frontend:BaseUrl"];
        var secret = configuration["Frontend:RevalidateSecret"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(secret))
        {
            logger.LogWarning("Frontend revalidation skipped: Frontend:BaseUrl or RevalidateSecret not configured.");
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/revalidate")
        {
            Content = JsonContent.Create(new { tags = tags.ToArray() }),
        };
        request.Headers.Add("x-revalidate-secret", secret);

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Frontend revalidation returned {Status}.", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Never fail a publish because revalidation was unreachable.
            logger.LogWarning(ex, "Frontend revalidation call failed.");
        }
    }
}
