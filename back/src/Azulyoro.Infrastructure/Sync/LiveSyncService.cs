using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.ApiFootball;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azulyoro.Infrastructure.Sync;

/// <summary>
/// Incremental live-fixture ingestion. Pulls the API-Football fixture bundle,
/// upserts score/status/events, and reports whether the match has finished so
/// the poller can stop.
/// </summary>
public class LiveSyncService(
    AppDbContext db,
    IApiFootballClient api,
    ILogger<LiveSyncService> logger)
{
    /// <summary>Poll every Boca fixture currently live; returns how many were polled.</summary>
    public async Task<int> PollOnceAsync(CancellationToken ct)
    {
        var live = await db.Fixtures.AsNoTracking()
            .Where(f => f.IsBoca)
            .Select(f => new { f.Id, f.ExtId, f.Status })
            .ToListAsync(ct);

        var targets = live.Where(f => f.Status.IsLive()).ToList();
        foreach (var fixture in targets)
        {
            await SyncFixtureAsync(fixture.Id, fixture.ExtId, ct);
        }

        return targets.Count;
    }

    /// <summary>
    /// Pull one fixture and upsert its live state. Returns true when the match
    /// is finished (poller should stop for it).
    /// </summary>
    public async Task<bool> SyncFixtureAsync(Guid fixtureId, int extId, CancellationToken ct)
    {
        var response = await api.GetAsync<ApiFixtureItem>(
            "fixtures", new Dictionary<string, string?> { ["id"] = extId.ToString() }, ct);

        var item = response.Response.FirstOrDefault();
        if (item is null)
        {
            logger.LogWarning("Live sync: no payload for fixture ext_id {ExtId}.", extId);
            return false;
        }

        var fixture = await db.Fixtures
            .Include(f => f.Events)
            .FirstOrDefaultAsync(f => f.Id == fixtureId, ct);
        if (fixture is null)
        {
            return false;
        }

        fixture.Status = FixtureStatusExtensions.FromApiShort(item.Fixture.Status.Short);
        fixture.Elapsed = item.Fixture.Status.Elapsed;
        fixture.HomeGoals = item.Goals.Home;
        fixture.AwayGoals = item.Goals.Away;
        fixture.LastSyncedAt = DateTime.UtcNow;

        // Upsert events keyed by their ordinal within the fixture feed.
        for (var seq = 0; seq < item.Events.Count; seq++)
        {
            var source = item.Events[seq];
            var target = fixture.Events.FirstOrDefault(e => e.ExtSeq == seq);
            if (target is null)
            {
                target = new FixtureEvent { FixtureId = fixture.Id, ExtSeq = seq };
                fixture.Events.Add(target);
            }

            target.Minute = source.Time.Elapsed;
            target.ExtraMinute = source.Time.Extra;
            target.Type = MapEventType(source.Type);
            target.Detail = source.Detail;
            target.Comments = source.Comments;
        }

        await db.SaveChangesAsync(ct);

        var finished = fixture.Status.IsFinished();
        if (finished)
        {
            logger.LogInformation("Live sync: fixture {ExtId} reached final status; stopping polling.", extId);
        }
        return finished;
    }

    private static EventType MapEventType(string? apiType) => apiType?.ToLowerInvariant() switch
    {
        "goal" => EventType.Goal,
        "card" => EventType.Card,
        "subst" => EventType.Substitution,
        "var" => EventType.Var,
        _ => EventType.Other,
    };
}
