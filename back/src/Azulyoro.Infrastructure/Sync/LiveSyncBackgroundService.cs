using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Azulyoro.Infrastructure.Sync;

/// <summary>
/// Coarse 60s heartbeat that polls only while a Boca match is live. Actual
/// per-fixture polling cadence and FT cut-off live in <see cref="LiveSyncService"/>.
/// </summary>
public class LiveSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<LiveSyncBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan WakeInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(WakeInterval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<LiveSyncService>();
                var polled = await sync.PollOnceAsync(stoppingToken);
                if (polled > 0)
                {
                    logger.LogDebug("Live sync polled {Count} live fixture(s).", polled);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Live sync poll failed; will retry next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
