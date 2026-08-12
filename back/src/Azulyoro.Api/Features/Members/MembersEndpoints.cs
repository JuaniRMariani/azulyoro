using Azulyoro.Api.Common;
using Azulyoro.Api.Features.Articles;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Members;

/// <summary>
/// Authenticated members zone (F4-5). Returns Published articles flagged
/// <c>IsMembersOnly</c>. 401s when the caller is not authenticated (cookie auth).
/// </summary>
public static class MembersEndpoints
{
    private const string DefaultLocale = "es";

    public static IEndpointRouteBuilder MapMembersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/members/content", GetMembersContent)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetMembersContent(
        HttpContext http,
        AppDbContext db,
        CancellationToken ct,
        string? locale = null)
    {
        var loc = NormalizeLocale(locale);

        var items = await db.Articles.AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published && a.IsMembersOnly)
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new
            {
                a.Slug,
                a.Category,
                a.CoverImageUrl,
                a.PublishedAt,
                a.IsMembersOnly,
                Localized = a.Translations.FirstOrDefault(t => t.Locale == loc),
                Fallback = a.Translations.FirstOrDefault(t => t.Locale == DefaultLocale),
                Any = a.Translations.FirstOrDefault(),
            })
            .ToListAsync(ct);

        var dtos = items.Select(a =>
        {
            var tr = a.Localized ?? a.Fallback ?? a.Any;
            return new ArticleListDto(
                a.Slug,
                a.Category.ToString(),
                tr?.Title ?? string.Empty,
                tr?.Summary,
                a.CoverImageUrl,
                a.IsMembersOnly,
                a.PublishedAt);
        }).ToList();

        // Members content is personalized/gated — never cache at the edge.
        CacheControl.SetNoStore(http);
        return Results.Ok(dtos);
    }

    private static string NormalizeLocale(string? locale) =>
        string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase) ? "en" : DefaultLocale;
}
