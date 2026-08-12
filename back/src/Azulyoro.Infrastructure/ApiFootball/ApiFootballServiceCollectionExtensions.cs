using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Azulyoro.Infrastructure.ApiFootball;

public static class ApiFootballServiceCollectionExtensions
{
    public static IServiceCollection AddApiFootballClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ApiFootballOptions>(
            configuration.GetSection(ApiFootballOptions.SectionName));

        services
            .AddHttpClient<IApiFootballClient, ApiFootballClient>((sp, http) =>
            {
                var opt = sp.GetRequiredService<IOptions<ApiFootballOptions>>().Value;
                var baseUrl = opt.BaseUrl.EndsWith('/') ? opt.BaseUrl : opt.BaseUrl + "/";
                http.BaseAddress = new Uri(baseUrl);
                http.Timeout = TimeSpan.FromSeconds(opt.RequestTimeoutSeconds);
                if (!string.IsNullOrWhiteSpace(opt.Key))
                {
                    http.DefaultRequestHeaders.Add("x-apisports-key", opt.Key);
                }
            })
            .AddResilienceHandler("api-football", (builder, context) =>
            {
                var opt = context.ServiceProvider
                    .GetRequiredService<IOptions<ApiFootballOptions>>().Value;

                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome)),
                    MaxRetryAttempts = opt.MaxRetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs),
                    DelayGenerator = args =>
                    {
                        // Honour Retry-After (delta or HTTP-date) when present;
                        // returning null falls back to the exponential backoff.
                        var retryAfter = args.Outcome.Result?.Headers.RetryAfter;
                        if (retryAfter?.Delta is { } delta)
                        {
                            return ValueTask.FromResult<TimeSpan?>(delta);
                        }
                        if (retryAfter?.Date is { } date)
                        {
                            var wait = date - DateTimeOffset.UtcNow;
                            if (wait > TimeSpan.Zero)
                            {
                                return ValueTask.FromResult<TimeSpan?>(wait);
                            }
                        }
                        return ValueTask.FromResult<TimeSpan?>(null);
                    },
                });

                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome)),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5,
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(15),
                });
            });

        return services;
    }

    private static bool ShouldRetry(Outcome<HttpResponseMessage> outcome) =>
        outcome.Result is { } response &&
        (response.StatusCode == HttpStatusCode.TooManyRequests ||
         (int)response.StatusCode >= 500);
}
