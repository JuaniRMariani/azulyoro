using Azulyoro.Domain.Entities;
using Azulyoro.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Admin;

/// <summary>
/// Recurring ingestion jobs orchestrated by Hangfire. Each job records its
/// result in sync_state so failures are visible without treating an API error
/// as a successful run.
/// </summary>
public class SyncJobs(
    AppDbContext db,
    Azulyoro.Infrastructure.Sync.ISportsSyncService sports,
    ILogger<SyncJobs> logger)
{
    public const string StaticJobId = "sync-static";
    public const string SemiJobId = "sync-semi";

    [DisableConcurrentExecution(600)]
    public Task SyncStaticAsync(CancellationToken ct) =>
        RunAsync("static:teams+squad", sports.SyncStaticAsync, ct);

    [DisableConcurrentExecution(600)]
    public Task SyncSemiAsync(CancellationToken ct) =>
        RunAsync("semi:standings+fixtures", sports.SyncSemiAsync, ct);

    private async Task RunAsync(
        string resource,
        Func<CancellationToken, Task> work,
        CancellationToken ct)
    {
        using var syncLock = JobStorage.Current.GetConnection()
            .AcquireDistributedLock("azulyoro:sports-sync", TimeSpan.FromMinutes(10));

        try
        {
            await work(ct);
            await MarkStateAsync(resource, null, ct);
            logger.LogInformation("Sync job '{Resource}' completed successfully.", resource);
        }
        catch (Exception ex)
        {
            // A failed transaction leaves newly added entities tracked. Clear
            // them before recording the failure, otherwise SaveChanges can
            // repeat the same unique-key insert and mask the real error.
            db.ChangeTracker.Clear();
            await MarkStateAsync(resource, ex.Message, CancellationToken.None);
            logger.LogError(ex, "Sync job '{Resource}' failed.", resource);
            throw;
        }
    }

    private async Task MarkStateAsync(
        string resource,
        string? error,
        CancellationToken ct)
    {
        var state = await db.SyncStates.FirstOrDefaultAsync(s => s.Resource == resource, ct);
        if (state is null)
        {
            state = new SyncState { Resource = resource };
            db.SyncStates.Add(state);
        }

        state.LastRunAt = DateTime.UtcNow;
        state.LastError = string.IsNullOrWhiteSpace(error)
            ? null
            : error.Length > 2000 ? error[..2000] : error;
        if (state.LastError is null)
        {
            state.LastOkAt = state.LastRunAt;
        }

        await db.SaveChangesAsync(ct);
    }
}
