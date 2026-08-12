using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Infrastructure.Content;

/// <summary>
/// Idempotent seeder for the news-source whitelist (see docs/09). VERIFIED RSS
/// feeds are seeded Active + RobotsOk; INFERRED / WAF-blocked ones are seeded
/// inactive until validated from the server. Section feeds carry a Boca keyword
/// filter so only relevant items are ingested.
/// </summary>
public static class ContentSeeder
{
    public static async Task SeedSourcesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Sources.AnyAsync(ct))
            return;

        const string bocaFilter = "Boca|Xeneize|Riquelme";

        var sources = new[]
        {
            // VERIFIED — pure-Boca fan feed (WordPress).
            new Source
            {
                Name = "La Número 12",
                RssUrl = "https://www.lanumero12.com.ar/feed/",
                Type = SourceType.Rss,
                Active = true,
                RobotsOk = true,
                RateLimitSeconds = 3,
                KeywordFilter = null,
            },
            // VERIFIED — section feed, robots-friendly → keyword filter.
            new Source
            {
                Name = "La Nación Fútbol",
                RssUrl = "https://www.lanacion.com.ar/arc/outboundfeeds/rss/category/deportes/futbol/?outputType=xml",
                Type = SourceType.Rss,
                Active = true,
                RobotsOk = true,
                RateLimitSeconds = 5,
                KeywordFilter = bocaFilter,
            },
            // VERIFIED — section feed, robots-friendly → keyword filter.
            new Source
            {
                Name = "Infobae Deportes",
                RssUrl = "https://www.infobae.com/arc/outboundfeeds/rss/category/deportes/?outputType=xml",
                Type = SourceType.Rss,
                Active = true,
                RobotsOk = true,
                RateLimitSeconds = 5,
                KeywordFilter = bocaFilter,
            },
            // VERIFIED — strong on transfer rumors. Note /rss (not /feed).
            new Source
            {
                Name = "Doble Amarilla",
                RssUrl = "https://www.dobleamarilla.com.ar/rss",
                Type = SourceType.Rss,
                Active = true,
                RobotsOk = true,
                RateLimitSeconds = 4,
                KeywordFilter = bocaFilter,
            },
            // INFERRED (Arc XP) — WAF blocked research fetch → inactive until verified from server.
            new Source
            {
                Name = "Olé Boca",
                RssUrl = "https://www.ole.com.ar/arc/outboundfeeds/rss/category/boca-juniors/?outputType=xml",
                Type = SourceType.Rss,
                Active = false,
                RobotsOk = false,
                RateLimitSeconds = 5,
                KeywordFilter = bocaFilter,
            },
            // VERIFIED — broad LatAm ES feed → strong Boca keyword filter.
            new Source
            {
                Name = "ESPN Deportes",
                RssUrl = "https://espndeportes.espn.com/espn/rss/news",
                Type = SourceType.Rss,
                Active = true,
                RobotsOk = true,
                RateLimitSeconds = 5,
                KeywordFilter = bocaFilter,
            },
        };

        db.Sources.AddRange(sources);
        await db.SaveChangesAsync(ct);
    }
}
