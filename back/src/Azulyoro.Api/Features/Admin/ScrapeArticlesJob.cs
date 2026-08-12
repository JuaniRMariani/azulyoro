using Azulyoro.Domain.Entities;
using Azulyoro.Infrastructure.Content;
using Azulyoro.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Admin;

/// <summary>
/// Recurring Hangfire job that scrapes all active RSS sources into staging and
/// records the run in sync_state (resource "news:scrape"). Concurrent runs are
/// blocked so overlapping schedules cannot double-fetch.
/// </summary>
public class ScrapeArticlesJob(
    RssScraperService scraper,
    AppDbContext db,
    ILogger<ScrapeArticlesJob> logger)
{
    public const string JobId = "news-scrape";
    public const string Resource = "news:scrape";

    [DisableConcurrentExecution(600)]
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var state = await db.SyncStates.FirstOrDefaultAsync(s => s.Resource == Resource, ct);
        if (state is null)
        {
            state = new SyncState { Resource = Resource };
            db.SyncStates.Add(state);
        }
        state.LastRunAt = now;

        try
        {
            var inserted = await scraper.ScrapeAllActiveAsync(ct);
            state.LastOkAt = DateTime.UtcNow;
            state.LastError = null;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("News scrape inserted {Count} new staging item(s).", inserted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            state.LastError = ex.Message;
            await db.SaveChangesAsync(ct);
            logger.LogError(ex, "News scrape job failed.");
            throw;
        }
    }
}
