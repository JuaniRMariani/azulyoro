using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.ApiFootball;
using Azulyoro.Infrastructure.Persistence;
using Azulyoro.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Azulyoro.UnitTests.Sync;

public class FixtureDetailSyncTests
{
    private const int FixtureExtId = 910001;
    private const int BocaExtId = 451;
    private const int RivalExtId = 435;

    // Boca players we already ingest.
    private const int BocaPlayerA = 1001;
    private const int BocaPlayerB = 1002;

    // Opponent players not yet in the DB — should be auto-created.
    private const int RivalPlayerA = 2001;
    private const int RivalPlayerB = 2002;

    /// <summary>Returns queued fixture payloads, one per call.</summary>
    private sealed class FakeApi(params ApiFixtureItem[] items) : IApiFootballClient
    {
        private readonly Queue<ApiFixtureItem> _items = new(items);
        public int Calls { get; private set; }

        public Task<ApiFootballResponse<T>> GetAsync<T>(
            string endpoint,
            IReadOnlyDictionary<string, string?>? query = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            var response = new ApiFootballResponse<ApiFixtureItem>
            {
                Results = 1,
                Response = [_items.Dequeue()],
            };
            return Task.FromResult((ApiFootballResponse<T>)(object)response);
        }
    }

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"detailsync-{Guid.CreateVersion7()}")
            .Options);

    private static async Task<Guid> SeedFinishedBocaFixtureAsync(AppDbContext db)
    {
        var season = new Season { Year = 2026, IsCurrent = true };
        var competition = new Competition { ExtId = 128, Name = "Liga Profesional", Type = CompetitionType.League };
        var boca = new Team { ExtId = BocaExtId, Name = "Boca Juniors", IsTracked = true };
        var rival = new Team { ExtId = RivalExtId, Name = "River Plate" };

        var pA = new Player { ExtId = BocaPlayerA, TeamId = boca.Id, Name = "Boca A", IsActive = true };
        var pB = new Player { ExtId = BocaPlayerB, TeamId = boca.Id, Name = "Boca B", IsActive = true };

        var fixture = new Fixture
        {
            ExtId = FixtureExtId,
            CompetitionId = competition.Id,
            SeasonId = season.Id,
            HomeTeamId = boca.Id,
            AwayTeamId = rival.Id,
            DateUtc = DateTime.UtcNow.AddHours(-3),
            Status = FixtureStatus.Finished,
            IsBoca = true,
        };
        db.AddRange(season, competition, boca, rival, pA, pB, fixture);
        await db.SaveChangesAsync();
        return fixture.Id;
    }

    private static ApiFixtureItem Payload()
    {
        return new ApiFixtureItem
        {
            Fixture = new ApiFixtureCore
            {
                Id = FixtureExtId,
                Status = new ApiFixtureStatus { Short = "FT", Elapsed = 90 },
            },
            Goals = new ApiGoals { Home = 2, Away = 1 },
            Events =
            {
                new ApiFixtureEvent
                {
                    Time = new ApiEventTime { Elapsed = 23 },
                    Team = new ApiEventRef { Id = BocaExtId },
                    Player = new ApiEventRef { Id = BocaPlayerA, Name = "Boca A" },
                    Assist = new ApiEventRef { Id = BocaPlayerB, Name = "Boca B" },
                    Type = "Goal",
                    Detail = "Normal Goal",
                },
                new ApiFixtureEvent
                {
                    Time = new ApiEventTime { Elapsed = 67 },
                    Team = new ApiEventRef { Id = RivalExtId },
                    Player = new ApiEventRef { Id = RivalPlayerA, Name = "Rival A" },
                    Type = "Card",
                    Detail = "Yellow Card",
                },
            },
            Lineups =
            {
                new ApiFixtureLineup
                {
                    Team = new ApiEventRef { Id = BocaExtId },
                    Formation = "4-3-3",
                    Coach = new ApiLineupCoach { Name = "Boca Coach" },
                    StartXI =
                    {
                        new ApiLineupSlot { Player = new ApiLineupPlayer { Id = BocaPlayerA, Name = "Boca A", Number = 10, Grid = "4:1" } },
                    },
                    Substitutes =
                    {
                        new ApiLineupSlot { Player = new ApiLineupPlayer { Id = BocaPlayerB, Name = "Boca B", Number = 16 } },
                    },
                },
                new ApiFixtureLineup
                {
                    Team = new ApiEventRef { Id = RivalExtId },
                    Formation = "4-4-2",
                    Coach = new ApiLineupCoach { Name = "Rival Coach" },
                    StartXI =
                    {
                        new ApiLineupSlot { Player = new ApiLineupPlayer { Id = RivalPlayerA, Name = "Rival A", Number = 9 } },
                    },
                    Substitutes =
                    {
                        new ApiLineupSlot { Player = new ApiLineupPlayer { Id = RivalPlayerB, Name = "Rival B", Number = 22 } },
                    },
                },
            },
            Players =
            {
                new ApiFixturePlayers
                {
                    Team = new ApiEventRef { Id = BocaExtId },
                    Players =
                    {
                        new ApiFixturePlayerEntry
                        {
                            Player = new ApiEventRef { Id = BocaPlayerA, Name = "Boca A" },
                            Statistics =
                            {
                                new ApiFixturePlayerStat
                                {
                                    Games = new ApiFixtureStatGames { Minutes = 90, Rating = "7.8", Number = 10 },
                                    Goals = new ApiFixtureStatGoals { Total = 1, Assists = 0 },
                                    Shots = new ApiFixtureStatShots { Total = 3, On = 2 },
                                    Passes = new ApiFixtureStatPasses { Total = 45, Accuracy = "88%" },
                                    Tackles = new ApiFixtureStatTackles { Total = 1 },
                                    Cards = new ApiFixtureStatCards { Yellow = 0, Red = 0 },
                                },
                            },
                        },
                    },
                },
                new ApiFixturePlayers
                {
                    Team = new ApiEventRef { Id = RivalExtId },
                    Players =
                    {
                        new ApiFixturePlayerEntry
                        {
                            Player = new ApiEventRef { Id = RivalPlayerA, Name = "Rival A" },
                            Statistics =
                            {
                                new ApiFixturePlayerStat
                                {
                                    Games = new ApiFixtureStatGames { Minutes = 80, Rating = null },
                                    Goals = new ApiFixtureStatGoals { Total = 0, Assists = 1 },
                                    Shots = new ApiFixtureStatShots { Total = 1, On = 0 },
                                    Passes = new ApiFixtureStatPasses { Total = 30, Accuracy = null },
                                    Tackles = new ApiFixtureStatTackles { Total = 4 },
                                    Cards = new ApiFixtureStatCards { Yellow = 1, Red = 0 },
                                },
                            },
                        },
                    },
                },
            },
        };
    }

    [Fact]
    public async Task Detail_sync_upserts_events_lineups_stats_and_is_idempotent()
    {
        await using var db = NewDb();
        var fixtureId = await SeedFinishedBocaFixtureAsync(db);

        var api = new FakeApi(Payload(), Payload());
        var service = new FixtureDetailSyncService(db, api, NullLogger<FixtureDetailSyncService>.Instance);

        var count = await service.SyncFixtureDetailAsync(fixtureId, FixtureExtId, CancellationToken.None);
        db.ChangeTracker.Clear();

        Assert.Equal(2, count);

        var boca = await db.Teams.AsNoTracking().FirstAsync(t => t.ExtId == BocaExtId);

        // Events: both upserted, team resolved for the Boca goal.
        var events = await db.FixtureEvents.AsNoTracking()
            .Where(e => e.FixtureId == fixtureId).OrderBy(e => e.ExtSeq).ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Equal(boca.Id, events[0].TeamId);
        Assert.NotNull(events[0].PlayerId);
        Assert.NotNull(events[0].AssistPlayerId);
        Assert.Equal(EventType.Goal, events[0].Type);

        // Opponent players auto-created (inactive, no team).
        var rivalA = await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.ExtId == RivalPlayerA);
        Assert.NotNull(rivalA);
        Assert.False(rivalA!.IsActive);
        Assert.Null(rivalA.TeamId);
        Assert.Equal("Rival A", rivalA.Name);

        // Lineups: 2 lineups, players resolved.
        var lineups = await db.FixtureLineups.AsNoTracking()
            .Include(l => l.Players)
            .Where(l => l.FixtureId == fixtureId).ToListAsync();
        Assert.Equal(2, lineups.Count);
        var bocaLineup = lineups.First(l => l.TeamId == boca.Id);
        Assert.Equal("4-3-3", bocaLineup.Formation);
        Assert.Equal(2, bocaLineup.Players.Count);
        Assert.Contains(bocaLineup.Players, p => p.IsStarter && p.Number == 10);
        Assert.Contains(bocaLineup.Players, p => !p.IsStarter && p.Number == 16);

        // Player stats: 2 rows, defensive parse of rating/accuracy.
        var stats = await db.FixturePlayerStats.AsNoTracking()
            .Where(s => s.FixtureId == fixtureId).ToListAsync();
        Assert.Equal(2, stats.Count);
        var bocaStat = stats.First(s => s.TeamId == boca.Id);
        Assert.Equal(90, bocaStat.Minutes);
        Assert.Equal(7.8m, bocaStat.Rating);
        Assert.Equal(88, bocaStat.PassesAccuracy);
        Assert.Equal(1, bocaStat.Goals);

        // ── Second run: no duplicates ────────────────────────────────────────
        await service.SyncFixtureDetailAsync(fixtureId, FixtureExtId, CancellationToken.None);
        db.ChangeTracker.Clear();

        Assert.Equal(2, await db.FixtureEvents.CountAsync(e => e.FixtureId == fixtureId));
        Assert.Equal(2, await db.FixtureLineups.CountAsync(l => l.FixtureId == fixtureId));
        Assert.Equal(2, await db.FixturePlayerStats.CountAsync(s => s.FixtureId == fixtureId));

        // Lineup players not duplicated either.
        var bocaLineupPlayers = await db.FixtureLineups.AsNoTracking()
            .Where(l => l.FixtureId == fixtureId && l.TeamId == boca.Id)
            .SelectMany(l => l.Players)
            .CountAsync();
        Assert.Equal(2, bocaLineupPlayers);

        // Opponent players not re-created.
        Assert.Equal(1, await db.Players.CountAsync(p => p.ExtId == RivalPlayerA));
    }

    [Fact]
    public async Task Backfill_targets_finished_boca_fixtures_without_events()
    {
        await using var db = NewDb();
        var fixtureId = await SeedFinishedBocaFixtureAsync(db);

        var api = new FakeApi(Payload());
        var service = new FixtureDetailSyncService(db, api, NullLogger<FixtureDetailSyncService>.Instance);

        await service.BackfillFinishedAsync(8, CancellationToken.None);

        Assert.Equal(1, api.Calls);
        Assert.Equal(2, await db.FixtureEvents.CountAsync(e => e.FixtureId == fixtureId));
    }
}
