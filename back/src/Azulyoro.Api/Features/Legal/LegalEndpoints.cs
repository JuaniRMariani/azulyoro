using Azulyoro.Api.Common;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Legal;

/// <summary>
/// Public, versioned legal pages (terms/privacy/legal-notice/cookies). Served
/// per slug + locale with a fallback to the Spanish row when the requested
/// locale is missing.
/// </summary>
public static class LegalEndpoints
{
    private const string DefaultLocale = "es";

    public static IEndpointRouteBuilder MapLegalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/legal/{slug}", GetLegalPage);
        return app;
    }

    private static async Task<IResult> GetLegalPage(
        HttpContext http,
        AppDbContext db,
        string slug,
        CancellationToken ct,
        string? locale = null)
    {
        var loc = NormalizeLocale(locale);

        var rows = await db.LegalPages.AsNoTracking()
            .Where(p => p.Slug == slug && (p.Locale == loc || p.Locale == DefaultLocale))
            .ToListAsync(ct);

        if (rows.Count == 0)
            return Results.NotFound();

        var page = rows.FirstOrDefault(p => p.Locale == loc)
            ?? rows.First(p => p.Locale == DefaultLocale);

        var dto = new LegalPageDto(
            page.Slug,
            page.Locale,
            page.Title,
            page.BodyHtml,
            page.Version,
            page.EffectiveDate);

        CacheControl.SetPublicMaxAge(http, 3600);
        return Results.Ok(dto);
    }

    private static string NormalizeLocale(string? locale) =>
        string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase) ? "en" : DefaultLocale;
}

public record LegalPageDto(
    string Slug,
    string Locale,
    string Title,
    string BodyHtml,
    int Version,
    DateOnly EffectiveDate);
