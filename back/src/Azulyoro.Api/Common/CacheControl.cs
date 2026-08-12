using Microsoft.AspNetCore.Http;

namespace Azulyoro.Api.Common;

/// <summary>Helpers to stamp Cache-Control headers by volatility.</summary>
public static class CacheControl
{
    public static void SetPublicMaxAge(HttpContext ctx, int seconds) =>
        ctx.Response.Headers.CacheControl = $"public, max-age={seconds}";

    public static void SetNoStore(HttpContext ctx) =>
        ctx.Response.Headers.CacheControl = "no-store";
}
