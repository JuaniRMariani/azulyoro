using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Persistence;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azulyoro.Infrastructure.Content;

/// <summary>
/// RSS-first scraper. Fetches whitelisted, robots-cleared, active sources with
/// polite per-host pacing + conditional requests, then stores ONLY
/// title/short-excerpt/link/image (never the full article body) as Pending
/// staging rows for human moderation. Dedup is by canonical URL hash.
/// </summary>
public class RssScraperService(
    HttpClient http,
    AppDbContext db,
    IOptions<ContentScrapeOptions> options,
    ILogger<RssScraperService> logger)
{
    private readonly ContentScrapeOptions _opts = options.Value;

    // One gate per host so we never hammer a single origin concurrently.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> HostGates = new();

    /// <summary>Scrape every Active + RobotsOk source. Returns total new staging rows.</summary>
    public async Task<int> ScrapeAllActiveAsync(CancellationToken ct)
    {
        var sourceIds = await db.Sources.AsNoTracking()
            .Where(s => s.Active && s.RobotsOk)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var total = 0;
        foreach (var id in sourceIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                total += await ScrapeSourceAsync(id, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Scrape failed for source {SourceId}", id);
            }
        }
        return total;
    }

    /// <summary>Scrape a single source. Returns the number of NEW staging rows inserted.</summary>
    public async Task<int> ScrapeSourceAsync(Guid sourceId, CancellationToken ct)
    {
        var source = await db.Sources.FirstOrDefaultAsync(s => s.Id == sourceId, ct);
        if (source is null)
        {
            logger.LogWarning("Scrape requested for unknown source {SourceId}", sourceId);
            return 0;
        }
        if (!source.Active || !source.RobotsOk)
        {
            logger.LogDebug("Skipping source {Name}: not active/robots-ok", source.Name);
            return 0;
        }

        if (source.Type == SourceType.Html)
        {
            // TODO(F3-2): HTML fallback scraping (AngleSharp) for feeds without RSS.
            // MVP is RSS-only; HTML sources stay inactive in the seeder.
            logger.LogInformation("Source {Name} is HTML-type; HTML scraping not implemented (MVP is RSS).", source.Name);
            return 0;
        }

        var host = TryGetHost(source.RssUrl);
        var gate = HostGates.GetOrAdd(host, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            // Politeness: honour per-source rate limit with jitter before hitting the host.
            var delayMs = Math.Max(0, source.RateLimitSeconds) * 1000
                          + Random.Shared.Next(0, Math.Max(1, _opts.JitterMaxMs));
            if (delayMs > 0)
                await Task.Delay(delayMs, ct);

            return await FetchAndStoreAsync(source, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<int> FetchAndStoreAsync(Source source, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, source.RssUrl);
        if (!string.IsNullOrWhiteSpace(source.Etag))
            request.Headers.TryAddWithoutValidation("If-None-Match", source.Etag);
        if (!string.IsNullOrWhiteSpace(source.LastModified))
            request.Headers.TryAddWithoutValidation("If-Modified-Since", source.LastModified);

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            // Nothing changed; just record that we checked.
            source.LastFetchedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return 0;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        SyndicationFeed feed;
        using (var xml = XmlReader.Create(stream, new XmlReaderSettings { Async = false, DtdProcessing = DtdProcessing.Prohibit }))
        {
            feed = SyndicationFeed.Load(xml);
        }

        var inserted = 0;
        var sanitizer = new HtmlSanitizer();

        foreach (var item in feed.Items)
        {
            ct.ThrowIfCancellationRequested();

            var title = (item.Title?.Text ?? string.Empty).Trim();
            var link = item.Links.FirstOrDefault()?.Uri?.ToString()
                       ?? item.Id
                       ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
                continue;

            var rawSummary = item.Summary?.Text ?? string.Empty;

            // Keyword filter (pipe-delimited OR terms) over title + summary.
            if (!MatchesKeyword(source.KeywordFilter, title, rawSummary))
                continue;

            var canonical = CanonicalizeUrl(link);
            var urlHash = Sha256Hex(canonical);
            var titleHash = Sha256Hex(title.ToLowerInvariant());

            // Dedup on canonical URL hash (unique index also backs this).
            var exists = await db.StagingArticles.AsNoTracking()
                .AnyAsync(s => s.UrlHash == urlHash, ct);
            if (exists)
                continue;

            var excerpt = BuildExcerpt(sanitizer, rawSummary);

            var staging = new StagingArticle
            {
                SourceId = source.Id,
                SourceName = source.Name,
                SourceUrl = link,
                UrlHash = urlHash,
                TitleHash = titleHash,
                Title = title.Length > 300 ? title[..300] : title,
                Excerpt = excerpt,
                ImageUrl = ExtractImageUrl(item),
                PublishedAtSource = ResolvePublished(item),
                ScrapedAt = DateTime.UtcNow,
                Status = StagingStatus.Pending,
                Category = ArticleCategory.News,
            };

            db.StagingArticles.Add(staging);
            inserted++;
        }

        // Persist conditional-request bookkeeping for next run.
        if (response.Headers.ETag is { } etag)
            source.Etag = etag.Tag;
        if (response.Content.Headers.LastModified is { } lm)
            source.LastModified = lm.ToString("R");
        source.LastFetchedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Scraped {Source}: {Inserted} new item(s).", source.Name, inserted);
        return inserted;
    }

    /// <summary>Sanitize + truncate the summary to a SHORT excerpt (never the full body).</summary>
    private string? BuildExcerpt(HtmlSanitizer sanitizer, string rawSummary)
    {
        if (string.IsNullOrWhiteSpace(rawSummary))
            return null;

        // Strip all markup down to plain text, then truncate hard.
        var text = sanitizer.Sanitize(rawSummary);
        text = StripTags(text).Trim();
        text = WebUtility.HtmlDecode(text);
        // Collapse whitespace.
        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        var max = _opts.ExcerptMaxLength;
        if (text.Length > max)
            text = text[..max].TrimEnd() + "…"; // ellipsis

        return text.Length == 0 ? null : text;
    }

    private static bool MatchesKeyword(string? keywordFilter, string title, string summary)
    {
        if (string.IsNullOrWhiteSpace(keywordFilter))
            return true;

        var haystack = (title + " " + summary).ToLowerInvariant();
        var terms = keywordFilter.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return terms.Length == 0 || terms.Any(t => haystack.Contains(t.ToLowerInvariant()));
    }

    /// <summary>Lowercase host, drop utm_* query params, drop fragment.</summary>
    public static string CanonicalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim().ToLowerInvariant();

        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        builder.Host = builder.Host.ToLowerInvariant();

        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            var kept = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(p =>
                {
                    var key = p.Split('=', 2)[0];
                    return !key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();
            builder.Query = string.Join('&', kept);
        }
        else
        {
            builder.Query = string.Empty;
        }

        // Normalize default ports out of the string.
        var result = builder.Uri.GetComponents(
            UriComponents.Scheme | UriComponents.Host | UriComponents.Path | UriComponents.Query,
            UriFormat.UriEscaped);
        return result.ToLowerInvariant();
    }

    private static string? ExtractImageUrl(SyndicationItem item)
    {
        // <enclosure url=... type="image/*"> first.
        var enclosure = item.Links.FirstOrDefault(l =>
            string.Equals(l.RelationshipType, "enclosure", StringComparison.OrdinalIgnoreCase) &&
            (l.MediaType?.StartsWith("image", StringComparison.OrdinalIgnoreCase) ?? false));
        if (enclosure?.Uri is { } encUri)
            return encUri.ToString();

        // media:content / media:thumbnail extensions.
        foreach (var ext in item.ElementExtensions)
        {
            if (string.Equals(ext.OuterName, "content", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext.OuterName, "thumbnail", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var el = ext.GetObject<System.Xml.Linq.XElement>();
                    var url = el.Attribute("url")?.Value;
                    if (!string.IsNullOrWhiteSpace(url))
                        return url;
                }
                catch
                {
                    // Ignore malformed extension nodes.
                }
            }
        }

        return null;
    }

    private static DateTime? ResolvePublished(SyndicationItem item)
    {
        if (item.PublishDate != default && item.PublishDate.Year > 1)
            return item.PublishDate.UtcDateTime;
        if (item.LastUpdatedTime != default && item.LastUpdatedTime.Year > 1)
            return item.LastUpdatedTime.UtcDateTime;
        return null;
    }

    private static string TryGetHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : url;

    private static string StripTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;
        var sb = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var c in html)
        {
            if (c == '<') inTag = true;
            else if (c == '>') inTag = false;
            else if (!inTag) sb.Append(c);
        }
        return sb.ToString();
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
