using Azulyoro.Api.Common;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Articles;

public static class ArticlesEndpoints
{
    private const string DefaultLocale = "es";

    public static IEndpointRouteBuilder MapArticlesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles");

        group.MapGet("/", GetArticles);
        group.MapGet("/featured", GetFeatured);
        group.MapGet("/{slug}", GetBySlug);

        return app;
    }

    private static async Task<IResult> GetArticles(
        HttpContext http,
        AppDbContext db,
        CancellationToken ct,
        string? category = null,
        string? locale = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;

        var loc = NormalizeLocale(locale);

        var query = db.Articles.AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published);

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<ArticleCategory>(category, ignoreCase: true, out var cat))
                return Results.Problem(
                    detail: "category must be one of: News, Rumor, Editorial.",
                    statusCode: StatusCodes.Status400BadRequest);
            query = query.Where(a => a.Category == cat);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(new PagedResult<ArticleListDto>(dtos, page, pageSize, total));
    }

    private static async Task<IResult> GetFeatured(
        HttpContext http,
        AppDbContext db,
        CancellationToken ct,
        string? locale = null)
    {
        var loc = NormalizeLocale(locale);

        var items = await db.Articles.AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.PublishedAt)
            .Take(5)
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

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetBySlug(
        HttpContext http,
        AppDbContext db,
        string slug,
        CancellationToken ct,
        string? locale = null)
    {
        var loc = NormalizeLocale(locale);

        var article = await db.Articles.AsNoTracking()
            .Where(a => a.Slug == slug && a.Status == ArticleStatus.Published)
            .Select(a => new
            {
                a.Slug,
                a.Category,
                a.CoverImageUrl,
                a.SourceName,
                a.SourceUrl,
                a.IsMembersOnly,
                a.PublishedAt,
                Localized = a.Translations.FirstOrDefault(t => t.Locale == loc),
                Fallback = a.Translations.FirstOrDefault(t => t.Locale == DefaultLocale),
                Any = a.Translations.FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        if (article is null)
            return Results.NotFound();

        var tr = article.Localized ?? article.Fallback ?? article.Any;

        var dto = new ArticleDetailDto(
            article.Slug,
            article.Category.ToString(),
            tr?.Locale ?? DefaultLocale,
            tr?.Title ?? string.Empty,
            tr?.Summary,
            tr?.BodyHtml,
            tr?.MetaTitle,
            tr?.MetaDescription,
            article.CoverImageUrl,
            article.SourceName,
            article.SourceUrl,
            article.IsMembersOnly,
            article.PublishedAt);

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(dto);
    }

    private static string NormalizeLocale(string? locale) =>
        string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase) ? "en" : DefaultLocale;
}

public record ArticleListDto(
    string Slug,
    string Category,
    string Title,
    string? Summary,
    string? CoverImageUrl,
    bool IsMembersOnly,
    DateTime? PublishedAt);

public record ArticleDetailDto(
    string Slug,
    string Category,
    string Locale,
    string Title,
    string? Summary,
    string? BodyHtml,
    string? MetaTitle,
    string? MetaDescription,
    string? CoverImageUrl,
    string? SourceName,
    string? SourceUrl,
    bool IsMembersOnly,
    DateTime? PublishedAt);
