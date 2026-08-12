using System.Net;
using System.Text;
using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Content;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Azulyoro.UnitTests.Content;

public class RssScraperTests
{
    private const string RssXml =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>Test Feed</title>
            <link>https://example.com/</link>
            <description>Test</description>
            <item>
              <title>Boca gana el clásico</title>
              <link>https://example.com/nota-1?utm_source=twitter&amp;ref=home</link>
              <description>&lt;p&gt;Un resumen &lt;b&gt;corto&lt;/b&gt; con algo de HTML que debería quedar limpio.&lt;/p&gt;</description>
              <pubDate>Wed, 12 Aug 2026 10:00:00 GMT</pubDate>
            </item>
            <item>
              <title>Riquelme habló en conferencia</title>
              <link>https://example.com/nota-2</link>
              <description>Otro resumen breve para la segunda nota del feed.</description>
              <pubDate>Wed, 12 Aug 2026 09:00:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    /// <summary>Returns the same RSS body for every request.</summary>
    private sealed class StaticRssHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RssXml, Encoding.UTF8, "application/rss+xml"),
            };
            return Task.FromResult(response);
        }
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"rss-{Guid.CreateVersion7()}")
            .Options);

    private static RssScraperService NewScraper(AppDbContext db, HttpMessageHandler handler)
    {
        var http = new HttpClient(handler);
        var opts = Options.Create(new ContentScrapeOptions
        {
            ExcerptMaxLength = 280,
            JitterMaxMs = 1, // keep the test fast
        });
        return new RssScraperService(http, db, opts, NullLogger<RssScraperService>.Instance);
    }

    private static async Task<Guid> SeedSourceAsync(AppDbContext db)
    {
        var source = new Source
        {
            Name = "Test Source",
            RssUrl = "https://example.com/feed/",
            Type = SourceType.Rss,
            Active = true,
            RobotsOk = true,
            RateLimitSeconds = 0, // no politeness delay in the test
            KeywordFilter = null,
        };
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        return source.Id;
    }

    [Fact]
    public async Task Scrape_inserts_two_staging_rows_then_dedups_on_rerun()
    {
        await using var db = NewDb();
        var sourceId = await SeedSourceAsync(db);
        var handler = new StaticRssHandler();
        var scraper = NewScraper(db, handler);

        var first = await scraper.ScrapeSourceAsync(sourceId, CancellationToken.None);
        Assert.Equal(2, first);
        Assert.Equal(2, await db.StagingArticles.CountAsync());

        // Second run: same URLs → deduped by UrlHash, zero new rows.
        var second = await scraper.ScrapeSourceAsync(sourceId, CancellationToken.None);
        Assert.Equal(0, second);
        Assert.Equal(2, await db.StagingArticles.CountAsync());
    }

    [Fact]
    public async Task Scrape_stores_only_short_sanitized_excerpt_no_full_body()
    {
        await using var db = NewDb();
        var sourceId = await SeedSourceAsync(db);
        var scraper = NewScraper(db, new StaticRssHandler());

        await scraper.ScrapeSourceAsync(sourceId, CancellationToken.None);

        var rows = await db.StagingArticles.AsNoTracking().ToListAsync();
        Assert.Equal(2, rows.Count);
        foreach (var row in rows)
        {
            Assert.NotNull(row.Excerpt);
            Assert.True(row.Excerpt!.Length <= 280, "Excerpt must be truncated to <= 280 chars.");
            // Sanitized: no raw HTML tags survive.
            Assert.DoesNotContain("<", row.Excerpt);
            Assert.Equal(StagingStatus.Pending, row.Status);
            Assert.Equal(ArticleCategory.News, row.Category);
            Assert.False(string.IsNullOrEmpty(row.UrlHash));
        }
    }

    [Fact]
    public void Canonicalize_strips_utm_and_lowercases_host()
    {
        var canonical = RssScraperService.CanonicalizeUrl(
            "https://Example.COM/Nota-1?utm_source=x&utm_medium=y&ref=home#frag");

        Assert.DoesNotContain("utm_", canonical);
        Assert.DoesNotContain("#", canonical);
        Assert.StartsWith("https://example.com/", canonical);
        Assert.Contains("ref=home", canonical);
    }
}
