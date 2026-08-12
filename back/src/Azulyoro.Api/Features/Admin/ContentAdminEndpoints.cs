using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Admin;

// TODO: require Admin auth (Phase 4) — all /api/admin routes below must be gated.
public static class ContentAdminEndpoints
{
    public static IEndpointRouteBuilder MapContentAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin");

        // Moderation queue.
        group.MapGet("/moderation", GetModeration);
        group.MapPost("/moderation/{id:guid}/approve", ApproveModeration);
        group.MapPost("/moderation/{id:guid}/reject", RejectModeration);

        // Article editing.
        group.MapPut("/articles/{id:guid}", UpsertArticle);
        group.MapPost("/articles/{id:guid}/publish", PublishArticle);

        // Sources admin.
        group.MapGet("/sources", GetSources);
        group.MapPost("/sources", CreateSource);
        group.MapPut("/sources/{id:guid}", UpdateSource);

        return app;
    }

    // ---- Moderation -------------------------------------------------------

    private static async Task<IResult> GetModeration(
        AppDbContext db, CancellationToken ct, string? status = "Pending")
    {
        var query = db.StagingArticles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StagingStatus>(status, ignoreCase: true, out var st))
                return Results.Problem(
                    detail: "status must be one of: Pending, Approved, Rejected.",
                    statusCode: StatusCodes.Status400BadRequest);
            query = query.Where(s => s.Status == st);
        }

        var items = await query
            .OrderByDescending(s => s.ScrapedAt)
            .Select(s => new ModerationItemDto(
                s.Id,
                ShortId.Of(s.Id),
                s.Title,
                s.Excerpt,
                s.SourceName,
                s.SourceUrl,
                s.ImageUrl,
                s.Category.ToString(),
                s.Status.ToString(),
                s.PublishedAtSource,
                s.ScrapedAt))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> ApproveModeration(
        AppDbContext db, Guid id, CancellationToken ct)
    {
        var staging = await db.StagingArticles.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (staging is null)
            return Results.NotFound();
        if (staging.Status == StagingStatus.Approved)
            return Results.Problem(
                detail: "Staging item is already approved.",
                statusCode: StatusCodes.Status409Conflict);

        var slug = await UniqueSlugAsync(db, Slugger.Slugify(staging.Title), ct);

        var article = new Article
        {
            Slug = slug,
            Category = staging.Category,
            Status = ArticleStatus.Draft,
            CoverImageUrl = staging.ImageUrl,
            SourceName = staging.SourceName,
            SourceUrl = staging.SourceUrl,
            StagingId = staging.Id,
        };
        article.Translations.Add(new ArticleTranslation
        {
            ArticleId = article.Id,
            Locale = "es",
            Title = staging.Title,
            Summary = staging.Excerpt,
        });

        db.Articles.Add(article);

        staging.Status = StagingStatus.Approved;
        staging.ReviewedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { articleId = article.Id });
    }

    private static async Task<IResult> RejectModeration(
        AppDbContext db, Guid id, CancellationToken ct)
    {
        var staging = await db.StagingArticles.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (staging is null)
            return Results.NotFound();

        staging.Status = StagingStatus.Rejected;
        staging.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ---- Article editing --------------------------------------------------

    private static async Task<IResult> UpsertArticle(
        AppDbContext db, Guid id, UpsertArticleRequest body, CancellationToken ct)
    {
        var article = await db.Articles
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
            return Results.NotFound();

        if (body.Category is { } catStr)
        {
            if (!Enum.TryParse<ArticleCategory>(catStr, ignoreCase: true, out var cat))
                return Results.Problem(
                    detail: "category must be one of: News, Rumor, Editorial.",
                    statusCode: StatusCodes.Status400BadRequest);
            article.Category = cat;
        }

        if (body.CoverImageUrl is not null)
            article.CoverImageUrl = body.CoverImageUrl;
        if (body.IsMembersOnly is { } mo)
            article.IsMembersOnly = mo;

        UpsertTranslation(article, "es", body.Es);
        UpsertTranslation(article, "en", body.En);

        // Slug: keep existing unless empty; auto from es title.
        if (string.IsNullOrWhiteSpace(article.Slug))
        {
            var esTitle = article.Translations.FirstOrDefault(t => t.Locale == "es")?.Title;
            if (!string.IsNullOrWhiteSpace(esTitle))
                article.Slug = await UniqueSlugAsync(db, Slugger.Slugify(esTitle), ct, article.Id);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = article.Id, slug = article.Slug });
    }

    private static void UpsertTranslation(Article article, string locale, TranslationInput? input)
    {
        if (input is null)
            return;

        var tr = article.Translations.FirstOrDefault(t => t.Locale == locale);
        if (tr is null)
        {
            tr = new ArticleTranslation { ArticleId = article.Id, Locale = locale, Title = input.Title ?? string.Empty };
            article.Translations.Add(tr);
        }

        if (input.Title is not null) tr.Title = input.Title;
        if (input.Summary is not null) tr.Summary = input.Summary;
        if (input.BodyHtml is not null) tr.BodyHtml = input.BodyHtml;
        if (input.MetaTitle is not null) tr.MetaTitle = input.MetaTitle;
        if (input.MetaDescription is not null) tr.MetaDescription = input.MetaDescription;
    }

    private static async Task<IResult> PublishArticle(
        AppDbContext db, Guid id, CancellationToken ct)
    {
        var article = await db.Articles
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
            return Results.NotFound();

        if (article.Translations.Count == 0)
            return Results.Problem(
                detail: "Article requires at least one translation before publishing.",
                statusCode: StatusCodes.Status422UnprocessableEntity);

        if (string.IsNullOrWhiteSpace(article.SourceName) || string.IsNullOrWhiteSpace(article.SourceUrl))
            return Results.Problem(
                detail: "Article requires SourceName and SourceUrl (attribution) before publishing.",
                statusCode: StatusCodes.Status422UnprocessableEntity);

        article.Status = ArticleStatus.Published;
        article.PublishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { id = article.Id, slug = article.Slug, status = article.Status.ToString() });
    }

    // ---- Sources ----------------------------------------------------------

    private static async Task<IResult> GetSources(AppDbContext db, CancellationToken ct)
    {
        var items = await db.Sources.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SourceDto(
                s.Id, s.Name, s.RssUrl, s.Type.ToString(), s.Active,
                s.RateLimitSeconds, s.KeywordFilter, s.RobotsOk, s.LastFetchedAt))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateSource(
        AppDbContext db, SourceRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.RssUrl))
            return Results.Problem(
                detail: "name and rssUrl are required.",
                statusCode: StatusCodes.Status400BadRequest);

        var type = ParseSourceType(body.Type);

        var source = new Source
        {
            Name = body.Name,
            RssUrl = body.RssUrl,
            Type = type,
            Active = body.Active ?? false,
            RateLimitSeconds = body.RateLimitSeconds ?? 3,
            KeywordFilter = body.KeywordFilter,
            RobotsOk = body.RobotsOk ?? false,
        };
        db.Sources.Add(source);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/admin/sources/{source.Id}", new { id = source.Id });
    }

    private static async Task<IResult> UpdateSource(
        AppDbContext db, Guid id, SourceRequest body, CancellationToken ct)
    {
        var source = await db.Sources.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (source is null)
            return Results.NotFound();

        if (body.Name is not null) source.Name = body.Name;
        if (body.RssUrl is not null) source.RssUrl = body.RssUrl;
        if (body.Type is not null) source.Type = ParseSourceType(body.Type);
        if (body.Active is { } active) source.Active = active;
        if (body.RateLimitSeconds is { } rl) source.RateLimitSeconds = rl;
        if (body.KeywordFilter is not null) source.KeywordFilter = body.KeywordFilter;
        if (body.RobotsOk is { } ro) source.RobotsOk = ro;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = source.Id });
    }

    private static SourceType ParseSourceType(string? type) =>
        Enum.TryParse<SourceType>(type, ignoreCase: true, out var t) ? t : SourceType.Rss;

    // ---- Helpers ----------------------------------------------------------

    private static async Task<string> UniqueSlugAsync(
        AppDbContext db, string baseSlug, CancellationToken ct, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "articulo";

        var slug = baseSlug;
        var i = 1;
        while (await db.Articles.AsNoTracking()
                   .AnyAsync(a => a.Slug == slug && (excludeId == null || a.Id != excludeId), ct))
        {
            slug = $"{baseSlug}-{++i}";
        }
        return slug;
    }
}

// ---- DTOs -----------------------------------------------------------------

public record ModerationItemDto(
    Guid Id,
    string ShortId,
    string Title,
    string? Excerpt,
    string SourceName,
    string SourceUrl,
    string? ImageUrl,
    string Category,
    string Status,
    DateTime? PublishedAtSource,
    DateTime ScrapedAt);

public record TranslationInput(
    string? Title,
    string? Summary,
    string? BodyHtml,
    string? MetaTitle,
    string? MetaDescription);

public record UpsertArticleRequest(
    string? Category,
    string? CoverImageUrl,
    bool? IsMembersOnly,
    TranslationInput? Es,
    TranslationInput? En);

public record SourceDto(
    Guid Id,
    string Name,
    string RssUrl,
    string Type,
    bool Active,
    int RateLimitSeconds,
    string? KeywordFilter,
    bool RobotsOk,
    DateTime? LastFetchedAt);

public record SourceRequest(
    string? Name,
    string? RssUrl,
    string? Type,
    bool? Active,
    int? RateLimitSeconds,
    string? KeywordFilter,
    bool? RobotsOk);

/// <summary>Short-UUID helper (first 6 hex chars, uppercase) for list surfaces.</summary>
public static class ShortId
{
    public static string Of(Guid id) =>
        id.ToString("N")[..6].ToUpperInvariant();
}
