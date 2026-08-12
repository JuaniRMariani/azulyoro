using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Azulyoro.Api.Configuration;

/// <summary>
/// Cross-cutting security config consumed by the SPA front: CORS allow-list
/// with credentials, antiforgery (CSRF), a rate-limit policy for sensitive
/// public POSTs, and forwarded-headers handling for the Nginx/Cloudflare proxy.
/// </summary>
public static class ApiHardening
{
    public const string CorsPolicy = "frontend";
    public const string SensitivePolicy = "sensitive";

    public static IServiceCollection AddApiHardening(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["https://azulyoro.com.ar", "http://localhost:3000"];

        services.AddCors(options =>
            options.AddPolicy(CorsPolicy, policy =>
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "azulyoro.csrf";
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Applied to sensitive public POSTs. Partition by the client IP so
            // one abusive client cannot exhaust the quota for every user.
            options.AddPolicy(SensitivePolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        });

        // Nginx is the only local proxy. It normalizes Cloudflare's client IP
        // header before forwarding, so the application only trusts headers
        // received from the loopback proxy and never from the public network.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownProxies.Add(System.Net.IPAddress.Loopback);
            options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
        });

        return services;
    }

    public static WebApplication UseApiHardening(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseCors(CorsPolicy);
        app.UseRateLimiter();
        return app;
    }
}
