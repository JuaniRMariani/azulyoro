using Azulyoro.Domain.Entities;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Admin;

/// <summary>
/// Recurring ingestion jobs orchestrated by Hangfire. The heavy sync logic
/// (teams/squads/fixtures/standings) is filled in once the API-Football key is
/// available (F1-5/F1-6); for now each job records its run in sync_state so the
/// schedule is observable in the dashboard.
/// </summary>
public class SyncJobs(AppDbContext db, ILogger<SyncJobs> logger)
{
    public const string StaticJobId = "sync-static";
    public const string SemiJobId = "sync-semi";

    public Task SyncStaticAsync(CancellationToken ct) =>
        MarkRunAsync("static:teams+squad", ct);

    public Task SyncSemiAsync(CancellationToken ct) =>
        MarkRunAsync("semi:standings+fixtures", ct);

    private async Task MarkRunAsync(string resource, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var state = await db.SyncStates.FirstOrDefaultAsync(s => s.Resource == resource, ct);
        if (state is null)
        {
            state = new SyncState { Resource = resource };
            db.SyncStates.Add(state);
        }

        state.LastRunAt = now;
        state.LastOkAt = now;
        state.LastError = null;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Sync job '{Resource}' ran at {Now:o}. Full ingestion pending API-Football key.",
            resource, now);
    }
}
