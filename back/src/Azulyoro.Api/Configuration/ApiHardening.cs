using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

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
            // Applied to sensitive public POSTs (register/login/subscribe) in later phases.
            options.AddFixedWindowLimiter(SensitivePolicy, limiter =>
            {
                limiter.PermitLimit = 5;
                limiter.Window = TimeSpan.FromSeconds(10);
                limiter.QueueLimit = 0;
            });
        });

        // Behind Nginx + Cloudflare: trust forwarded scheme/host so Secure
        // cookies and redirects use the real external origin.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
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
