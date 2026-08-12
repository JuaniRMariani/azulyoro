using Azulyoro.Infrastructure.Content;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Azulyoro.UnitTests.Content;

public class LegalSeederTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"legal-{Guid.CreateVersion7()}")
            .Options);

    [Fact]
    public async Task SeedLegalAsync_seeds_four_slugs_in_both_locales()
    {
        await using var db = NewDb();

        await LegalSeeder.SeedLegalAsync(db, CancellationToken.None);

        var pages = await db.LegalPages.ToListAsync();
        Assert.Equal(8, pages.Count);

        foreach (var slug in new[] { "terminos", "privacidad", "aviso-legal", "cookies" })
        {
            Assert.Contains(pages, p => p.Slug == slug && p.Locale == "es");
            Assert.Contains(pages, p => p.Slug == slug && p.Locale == "en");
        }
    }

    [Fact]
    public async Task SeedLegalAsync_resolves_every_placeholder()
    {
        await using var db = NewDb();

        await LegalSeeder.SeedLegalAsync(db, CancellationToken.None);

        var pages = await db.LegalPages.ToListAsync();
        Assert.All(pages, p =>
        {
            Assert.DoesNotContain("[[", p.BodyHtml);
            Assert.DoesNotContain("[[", p.Title);
        });
    }

    [Fact]
    public async Task SeedLegalAsync_converts_markdown_to_html_with_expected_values()
    {
        await using var db = NewDb();

        await LegalSeeder.SeedLegalAsync(db, CancellationToken.None);

        var terms = await db.LegalPages.SingleAsync(p => p.Slug == "terminos" && p.Locale == "es");
        Assert.Contains("<h2>", terms.BodyHtml);
        Assert.Contains("<p>", terms.BodyHtml);
        // Resolved placeholders present verbatim.
        Assert.Contains("Ciudad Autónoma de Buenos Aires (CABA)", terms.BodyHtml);
        Assert.Contains("legal@azulyoro.com.ar", terms.BodyHtml);
        Assert.Contains("16 años", terms.BodyHtml);
        Assert.Equal(1, terms.Version);
        Assert.Equal(new DateOnly(2026, 8, 12), terms.EffectiveDate);

        var privacy = await db.LegalPages.SingleAsync(p => p.Slug == "privacidad" && p.Locale == "es");
        Assert.Contains("<table>", privacy.BodyHtml);
        Assert.Contains("privacidad@azulyoro.com.ar", privacy.BodyHtml);
        Assert.Contains("Plausible", privacy.BodyHtml);

        var notice = await db.LegalPages.SingleAsync(p => p.Slug == "aviso-legal" && p.Locale == "en");
        Assert.Contains("API-Football", notice.BodyHtml);
    }

    [Fact]
    public async Task SeedLegalAsync_is_idempotent()
    {
        await using var db = NewDb();

        await LegalSeeder.SeedLegalAsync(db, CancellationToken.None);
        await LegalSeeder.SeedLegalAsync(db, CancellationToken.None);

        Assert.Equal(8, await db.LegalPages.CountAsync());
    }
}
