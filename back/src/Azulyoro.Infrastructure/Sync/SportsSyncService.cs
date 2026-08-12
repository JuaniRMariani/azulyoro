using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.ApiFootball;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azulyoro.Infrastructure.Sync;

public interface ISportsSyncService
{
    Task SyncStaticAsync(CancellationToken ct);
    Task SyncSemiAsync(CancellationToken ct);
}

/// <summary>
/// Idempotent API-Football ingestion for the tracked team. External calls are
/// completed before database writes so a provider failure cannot leave a
/// half-written catalog. Existing rows are updated by provider id.
/// </summary>
public sealed class SportsSyncService(
    AppDbContext db,
    IApiFootballClient api,
    IOptions<SportsSyncOptions> options,
    ILogger<SportsSyncService> logger) : ISportsSyncService
{
    private readonly SportsSyncOptions options = options.Value;

    public async Task SyncStaticAsync(CancellationToken ct)
    {
        ValidateOptions();

        var teamResponse = await api.GetAsync<ApiTeamItem>(
            "teams",
            new Dictionary<string, string?> { ["id"] = options.TeamExtId.ToString() },
            ct);
        var teamPayload = teamResponse.Response.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"API-Football returned no team for id {options.TeamExtId}.");

        var players = await GetAllPlayersAsync(ct);

        var seasons = await db.Seasons.ToDictionaryAsync(s => s.Year, ct);
        var competitions = await db.Competitions.ToDictionaryAsync(c => c.ExtId, ct);
        var teams = await db.Teams.ToDictionaryAsync(t => t.ExtId, ct);
        var playersByExt = await db.Players.ToDictionaryAsync(p => p.ExtId, ct);
        var season = GetOrCreateSeason(seasons);
        var trackedTeam = UpsertTeam(teams, teamPayload.Team, teamPayload.Venue, true);

        var teamPlayers = await db.Players
            .Where(p => p.TeamId == trackedTeam.Id)
            .ToListAsync(ct);
        foreach (var player in teamPlayers)
        {
            player.IsActive = false;
        }

        var statsByKey = await db.PlayerSeasonStats
            .Where(s => s.SeasonId == season.Id)
            .ToDictionaryAsync(s => (s.PlayerId, s.CompetitionId), ct);

        foreach (var item in players)
        {
            if (item.Player.Id <= 0)
            {
                continue;
            }

            if (!playersByExt.TryGetValue(item.Player.Id, out var player))
            {
                player = new Player { ExtId = item.Player.Id };
                db.Players.Add(player);
                playersByExt[item.Player.Id] = player;
            }

            player.TeamId = trackedTeam.Id;
            player.Name = item.Player.Name ??
                $"{item.Player.Firstname} {item.Player.Lastname}".Trim();
            player.Firstname = item.Player.Firstname;
            player.Lastname = item.Player.Lastname;
            player.BirthDate = item.Player.Birth.Date;
            player.Nationality = item.Player.Nationality;
            player.Height = ParseCentimeters(item.Player.Height);
            player.Weight = ParseKilograms(item.Player.Weight);
            player.Position = MapPosition(item.Statistics.FirstOrDefault()?.Games.Position);
            player.Number = item.Statistics.FirstOrDefault()?.Games.Number;
            player.PhotoUrl = item.Player.Photo;
            player.IsActive = true;

            foreach (var statistic in item.Statistics)
            {
                if (statistic.League.Id <= 0)
                {
                    continue;
                }

                var competition = UpsertCompetition(competitions, statistic.League);
                var key = (player.Id, competition.Id);
                if (!statsByKey.TryGetValue(key, out var seasonStats))
                {
                    seasonStats = new PlayerSeasonStats
                    {
                        PlayerId = player.Id,
                        CompetitionId = competition.Id,
                        SeasonId = season.Id,
                    };
                    db.PlayerSeasonStats.Add(seasonStats);
                    statsByKey[key] = seasonStats;
                }

                seasonStats.Appearances = statistic.Games.Appearances ?? 0;
                seasonStats.Minutes = statistic.Games.Minutes ?? 0;
                seasonStats.Goals = statistic.Goals.Total ?? 0;
                seasonStats.Assists = statistic.Goals.Assists ?? 0;
                seasonStats.Yellow = statistic.Cards.Yellow ?? 0;
                seasonStats.Red = statistic.Cards.Red ?? 0;
                seasonStats.Rating = statistic.Rating;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Sports static sync completed for team {TeamExtId}, season {Season}: {PlayerCount} player payloads.",
            options.TeamExtId, options.Season, players.Count);
    }

    public async Task SyncSemiAsync(CancellationToken ct)
    {
        ValidateOptions();

        var fixtureResponse = await api.GetAsync<ApiFixtureSyncItem>(
            "fixtures",
            new Dictionary<string, string?>
            {
                ["team"] = options.TeamExtId.ToString(),
                ["season"] = options.Season.ToString(),
            },
            ct);
        var standingResponse = await api.GetAsync<ApiStandingResponseItem>(
            "standings",
            new Dictionary<string, string?>
            {
                ["league"] = options.PrimaryLeagueExtId.ToString(),
                ["season"] = options.Season.ToString(),
            },
            ct);

        var seasons = await db.Seasons.ToDictionaryAsync(s => s.Year, ct);
        var competitions = await db.Competitions.ToDictionaryAsync(c => c.ExtId, ct);
        var teams = await db.Teams.ToDictionaryAsync(t => t.ExtId, ct);
        var season = GetOrCreateSeason(seasons);

        var fixtureIds = fixtureResponse.Response
            .Select(f => f.Fixture.Id)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        var fixtures = await db.Fixtures
            .Where(f => fixtureIds.Contains(f.ExtId))
            .ToDictionaryAsync(f => f.ExtId, ct);

        foreach (var item in fixtureResponse.Response)
        {
            if (item.Fixture.Id <= 0 || item.Fixture.Date is null ||
                item.Teams.Home.Id is null || item.Teams.Away.Id is null ||
                item.League.Id <= 0)
            {
                continue;
            }

            var home = UpsertTeam(teams, item.Teams.Home);
            var away = UpsertTeam(teams, item.Teams.Away);
            var competition = UpsertCompetition(competitions, item.League);

            if (!fixtures.TryGetValue(item.Fixture.Id, out var fixture))
            {
                fixture = new Fixture { ExtId = item.Fixture.Id };
                db.Fixtures.Add(fixture);
                fixtures[item.Fixture.Id] = fixture;
            }

            fixture.CompetitionId = competition.Id;
            fixture.SeasonId = season.Id;
            fixture.Round = item.League.Round;
            fixture.DateUtc = item.Fixture.Date.Value.UtcDateTime;
            fixture.Status = FixtureStatusExtensions.FromApiShort(item.Fixture.Status.Short);
            fixture.Elapsed = item.Fixture.Status.Elapsed;
            fixture.VenueName = item.Fixture.Venue.Name;
            fixture.HomeTeamId = home.Id;
            fixture.AwayTeamId = away.Id;
            fixture.HomeGoals = item.Goals.Home;
            fixture.AwayGoals = item.Goals.Away;
            fixture.HtHome = item.Score.Halftime.Home;
            fixture.HtAway = item.Score.Halftime.Away;
            fixture.FtHome = item.Score.Fulltime.Home;
            fixture.FtAway = item.Score.Fulltime.Away;
            fixture.IsBoca = item.Teams.Home.Id == options.TeamExtId ||
                item.Teams.Away.Id == options.TeamExtId;
            fixture.LastSyncedAt = DateTime.UtcNow;
        }

        await UpsertStandingsAsync(
            standingResponse.Response,
            season,
            competitions,
            teams,
            ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Sports semi sync completed for team {TeamExtId}, season {Season}: {FixtureCount} fixtures.",
            options.TeamExtId, options.Season, fixtureResponse.Response.Count);
    }

    private async Task<List<ApiPlayerItem>> GetAllPlayersAsync(CancellationToken ct)
    {
        var all = new List<ApiPlayerItem>();
        var page = 1;
        int totalPages;

        do
        {
            var response = await api.GetAsync<ApiPlayerItem>(
                "players",
                new Dictionary<string, string?>
                {
                    ["team"] = options.TeamExtId.ToString(),
                    ["season"] = options.Season.ToString(),
                    ["page"] = page.ToString(),
                },
                ct);
            all.AddRange(response.Response);
            totalPages = Math.Min(response.Paging?.Total ?? page, options.MaxPlayerPages);
            page++;
        }
        while (page <= totalPages);

        return all;
    }

    private async Task UpsertStandingsAsync(
        IReadOnlyCollection<ApiStandingResponseItem> payloads,
        Season season,
        Dictionary<int, Competition> competitions,
        Dictionary<int, Team> teams,
        CancellationToken ct)
    {
        var payload = payloads.FirstOrDefault(p => p.League.Id == options.PrimaryLeagueExtId);
        if (payload is null)
        {
            throw new InvalidOperationException(
                $"API-Football returned no standings for league {options.PrimaryLeagueExtId}.");
        }

        var competition = UpsertCompetition(competitions, new ApiLeague
        {
            Id = payload.League.Id,
            Name = payload.League.Name,
            Country = payload.League.Country,
            Logo = payload.League.Logo,
            Type = "League",
            Season = payload.League.Season,
        });

        var rows = payload.League.Standings.SelectMany(group => group).ToList();
        var existing = await db.Standings
            .Where(s => s.CompetitionId == competition.Id && s.SeasonId == season.Id)
            .ToListAsync(ct);
        var seen = new HashSet<(Guid TeamId, string Group)>();

        foreach (var row in rows)
        {
            if (row.Team.Id is null || row.Team.Id <= 0)
            {
                continue;
            }

            var team = UpsertTeam(teams, row.Team);
            var group = row.Group ?? string.Empty;
            var standing = existing.FirstOrDefault(s =>
                s.TeamId == team.Id && s.GroupName == group);
            if (standing is null)
            {
                standing = new Standing
                {
                    CompetitionId = competition.Id,
                    SeasonId = season.Id,
                    TeamId = team.Id,
                    GroupName = group,
                };
                db.Standings.Add(standing);
                existing.Add(standing);
            }

            standing.Rank = row.Rank;
            standing.Points = row.Points;
            standing.Played = row.All.Played;
            standing.Win = row.All.Win;
            standing.Draw = row.All.Draw;
            standing.Lose = row.All.Lose;
            standing.GoalsFor = row.All.Goals.For;
            standing.GoalsAgainst = row.All.Goals.Against;
            standing.GoalsDiff = row.GoalsDiff;
            standing.Form = row.Form;
            seen.Add((team.Id, group));
        }

        var stale = existing.Where(s => !seen.Contains((s.TeamId, s.GroupName))).ToList();
        db.Standings.RemoveRange(stale);
    }

    private Season GetOrCreateSeason(Dictionary<int, Season> seasons)
    {
        if (!seasons.TryGetValue(options.Season, out var season))
        {
            season = new Season { Year = options.Season };
            db.Seasons.Add(season);
            seasons[options.Season] = season;
        }

        foreach (var existing in seasons.Values)
        {
            existing.IsCurrent = existing.Id == season.Id;
        }

        season.IsCurrent = true;
        return season;
    }

    private Team UpsertTeam(
        Dictionary<int, Team> teams,
        ApiTeamRef source,
        bool isTracked = false)
    {
        if (source.Id is null || source.Id <= 0)
        {
            throw new InvalidOperationException("API-Football returned a team without an id.");
        }

        if (!teams.TryGetValue(source.Id.Value, out var team))
        {
            team = new Team { ExtId = source.Id.Value };
            db.Teams.Add(team);
            teams[source.Id.Value] = team;
        }

        team.Name = source.Name ?? team.Name;
        team.LogoUrl = source.Logo ?? team.LogoUrl;
        team.IsTracked |= isTracked || source.Id == options.TeamExtId;
        return team;
    }

    private Team UpsertTeam(
        Dictionary<int, Team> teams,
        ApiTeam source,
        ApiVenue? venue,
        bool isTracked)
    {
        var team = UpsertTeam(teams, new ApiTeamRef
        {
            Id = source.Id,
            Name = source.Name,
            Logo = source.Logo,
        }, isTracked);
        team.ShortName = source.Code ?? team.ShortName;
        team.Founded = source.Founded ?? team.Founded;
        team.VenueName = venue?.Name ?? team.VenueName;
        team.VenueCity = venue?.City ?? team.VenueCity;
        return team;
    }

    private Competition UpsertCompetition(
        Dictionary<int, Competition> competitions,
        ApiLeague source)
    {
        if (!competitions.TryGetValue(source.Id, out var competition))
        {
            competition = new Competition { ExtId = source.Id };
            db.Competitions.Add(competition);
            competitions[source.Id] = competition;
        }

        competition.Name = source.Name ?? competition.Name;
        competition.Country = source.Country ?? competition.Country;
        competition.LogoUrl = source.Logo ?? competition.LogoUrl;
        competition.Type = source.Type?.Equals("Cup", StringComparison.OrdinalIgnoreCase) == true
            ? CompetitionType.Cup
            : CompetitionType.League;
        return competition;
    }

    private void ValidateOptions()
    {
        if (options.TeamExtId <= 0 || options.PrimaryLeagueExtId <= 0 ||
            options.Season < 1900 || options.MaxPlayerPages < 1)
        {
            throw new InvalidOperationException(
                "SportsSync requires positive TeamExtId/PrimaryLeagueExtId and a valid Season.");
        }
    }

    private static PlayerPosition MapPosition(string? position) => position?.ToLowerInvariant() switch
    {
        "goalkeeper" => PlayerPosition.Goalkeeper,
        "defender" => PlayerPosition.Defender,
        "midfielder" => PlayerPosition.Midfielder,
        "attacker" => PlayerPosition.Attacker,
        _ => PlayerPosition.Unknown,
    };

    private static int? ParseCentimeters(string? value) =>
        int.TryParse(value?.Replace(" cm", string.Empty, StringComparison.OrdinalIgnoreCase), out var parsed)
            ? parsed
            : null;

    private static int? ParseKilograms(string? value) =>
        int.TryParse(value?.Replace(" kg", string.Empty, StringComparison.OrdinalIgnoreCase), out var parsed)
            ? parsed
            : null;
}
