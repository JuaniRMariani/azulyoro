using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Azulyoro.Infrastructure.ApiFootball;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;

namespace Azulyoro.UnitTests.ApiFootball;

public class ApiFootballClientTests
{
    private sealed class TeamWrapper
    {
        [JsonPropertyName("team")]
        public TeamNode Team { get; set; } = new();
    }

    private sealed class TeamNode
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Queued-response mock handler that counts requests.</summary>
    private sealed class QueueHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(responses.Dequeue());
        }
    }

    private const string BocaPayload =
        """{"results":1,"paging":{"current":1,"total":1},"errors":[],"response":[{"team":{"id":451,"name":"Boca Juniors"}}]}""";

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static IApiFootballClient BuildClient(QueueHandler handler)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiFootball:Key"] = "test-key",
                ["ApiFootball:MaxRetryAttempts"] = "3",
                ["ApiFootball:RetryBaseDelayMs"] = "1", // keep the test fast
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiFootballClient(config);
        // Feed the mock as the primary handler beneath the resilience pipeline.
        services.AddHttpClient<IApiFootballClient, ApiFootballClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider().GetRequiredService<IApiFootballClient>();
    }

    [Fact]
    public async Task Retries_on_429_then_succeeds()
    {
        var responses = new Queue<HttpResponseMessage>();
        responses.Enqueue(Json(HttpStatusCode.TooManyRequests, "{}"));
        responses.Enqueue(Json(HttpStatusCode.OK, BocaPayload));
        var handler = new QueueHandler(responses);
        var client = BuildClient(handler);

        var result = await client.GetAsync<TeamWrapper>("teams",
            new Dictionary<string, string?> { ["id"] = "451" });

        Assert.Equal(2, handler.Calls); // one retry after the 429
        Assert.False(result.HasErrors);
        Assert.Single(result.Response);
        Assert.Equal(451, result.Response[0].Team.Id);
        Assert.Equal("Boca Juniors", result.Response[0].Team.Name);
    }

    [Fact]
    public async Task Parses_payload_envelope()
    {
        var responses = new Queue<HttpResponseMessage>();
        responses.Enqueue(Json(HttpStatusCode.OK, BocaPayload));
        var handler = new QueueHandler(responses);
        var client = BuildClient(handler);

        var result = await client.GetAsync<TeamWrapper>("teams");

        Assert.Equal(1, handler.Calls);
        Assert.Equal(1, result.Results);
        Assert.Equal(1, result.Paging!.Total);
        Assert.Equal("Boca Juniors", result.Response[0].Team.Name);
    }
}
