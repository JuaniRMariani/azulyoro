using Azulyoro.Api.Common;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Matches;

public static class MatchesEndpoints
{
    public static IEndpointRouteBuilder MapMatchesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches");

        group.MapGet("/", GetMatches);
        group.MapGet("/next", GetNext);
        group.MapGet("/live", GetLive);
        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/{id:guid}/events", GetEvents);
        group.MapGet("/{id:guid}/lineups", GetLineups);
        group.MapGet("/{id:guid}/player-stats", GetPlayerStats);

        return app;
    }

    private static async Task<IResult> GetMatches(
        HttpContext http,
        AppDbContext db,
        CancellationToken ct,
        string? status = null,
        Guid? competitionId = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;

        var query = db.Fixtures.AsNoTracking().Where(f => f.IsBoca);

        bool upcoming = false;
        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status.Trim().ToLowerInvariant())
            {
                case "upcoming":
                    upcoming = true;
                    query = query.Where(f => f.Status == FixtureStatus.NotStarted);
                    break;
                case "finished":
                    query = query.Where(f =>
                        f.Status == FixtureStatus.Finished ||
                        f.Status == FixtureStatus.Cancelled ||
                        f.Status == FixtureStatus.Abandoned ||
                        f.Status == FixtureStatus.Awarded ||
                        f.Status == FixtureStatus.WalkOver);
                    break;
                case "live":
                    query = query.Where(f =>
                        f.Status == FixtureStatus.FirstHalf ||
                        f.Status == FixtureStatus.HalfTime ||
                        f.Status == FixtureStatus.SecondHalf ||
                        f.Status == FixtureStatus.ExtraTime ||
                        f.Status == FixtureStatus.BreakTime ||
                        f.Status == FixtureStatus.Penalty);
                    break;
                default:
                    return Results.Problem(
                        detail: "status must be one of: upcoming, finished, live.",
                        statusCode: StatusCodes.Status400BadRequest);
            }
        }

        if (competitionId is { } cid)
            query = query.Where(f => f.CompetitionId == cid);
        if (from is { } fromUtc)
            query = query.Where(f => f.DateUtc >= fromUtc);
        if (to is { } toUtc)
            query = query.Where(f => f.DateUtc <= toUtc);

        var total = await query.CountAsync(ct);

        query = upcoming
            ? query.OrderBy(f => f.DateUtc)
            : query.OrderByDescending(f => f.DateUtc);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new MatchDto(
                f.Id,
                f.ExtId,
                f.DateUtc,
                f.Status.ToString(),
                f.CompetitionId,
                f.Competition!.Name,
                f.HomeTeamId,
                f.HomeTeam!.Name,
                f.HomeTeam.LogoUrl,
                f.AwayTeamId,
                f.AwayTeam!.Name,
                f.AwayTeam.LogoUrl,
                f.HomeGoals,
                f.AwayGoals,
                f.IsBoca))
            .ToListAsync(ct);

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(new PagedResult<MatchDto>(items, page, pageSize, total));
    }

    private static async Task<IResult> GetNext(HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var match = await db.Fixtures.AsNoTracking()
            .Where(f => f.IsBoca && f.Status == FixtureStatus.NotStarted && f.DateUtc >= now)
            .OrderBy(f => f.DateUtc)
            .Select(f => new MatchDto(
                f.Id, f.ExtId, f.DateUtc, f.Status.ToString(),
                f.CompetitionId, f.Competition!.Name,
                f.HomeTeamId, f.HomeTeam!.Name, f.HomeTeam.LogoUrl,
                f.AwayTeamId, f.AwayTeam!.Name, f.AwayTeam.LogoUrl,
                f.HomeGoals, f.AwayGoals, f.IsBoca))
            .FirstOrDefaultAsync(ct);

        CacheControl.SetNoStore(http);
        return match is null
            ? Results.NotFound()
            : Results.Ok(match);
    }

    private static async Task<IResult> GetLive(HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var items = await db.Fixtures.AsNoTracking()
            .Where(f => f.IsBoca && (
                f.Status == FixtureStatus.FirstHalf ||
                f.Status == FixtureStatus.HalfTime ||
                f.Status == FixtureStatus.SecondHalf ||
                f.Status == FixtureStatus.ExtraTime ||
                f.Status == FixtureStatus.BreakTime ||
                f.Status == FixtureStatus.Penalty))
            .OrderByDescending(f => f.DateUtc)
            .Select(f => new MatchDto(
                f.Id, f.ExtId, f.DateUtc, f.Status.ToString(),
                f.CompetitionId, f.Competition!.Name,
                f.HomeTeamId, f.HomeTeam!.Name, f.HomeTeam.LogoUrl,
                f.AwayTeamId, f.AwayTeam!.Name, f.AwayTeam.LogoUrl,
                f.HomeGoals, f.AwayGoals, f.IsBoca))
            .ToListAsync(ct);

        CacheControl.SetNoStore(http);
        return items.Count == 0
            ? Results.NoContent()
            : Results.Ok(items);
    }

    private static async Task<IResult> GetById(HttpContext http, AppDbContext db, Guid id, CancellationToken ct)
    {
        var match = await db.Fixtures.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new MatchDetailDto(
                f.Id, f.ExtId, f.DateUtc, f.Status.ToString(),
                f.CompetitionId, f.Competition!.Name,
                f.HomeTeamId, f.HomeTeam!.Name, f.HomeTeam.LogoUrl,
                f.AwayTeamId, f.AwayTeam!.Name, f.AwayTeam.LogoUrl,
                f.HomeGoals, f.AwayGoals, f.IsBoca,
                f.VenueName, f.Round, f.HtHome, f.HtAway, f.FtHome, f.FtAway, f.Elapsed))
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return Results.NotFound();

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(match);
    }

    private static async Task<IResult> GetEvents(HttpContext http, AppDbContext db, Guid id, CancellationToken ct)
    {
        var exists = await db.Fixtures.AsNoTracking().AnyAsync(f => f.Id == id, ct);
        if (!exists)
            return Results.NotFound();

        var events = await db.FixtureEvents.AsNoTracking()
            .Where(e => e.FixtureId == id)
            .OrderBy(e => e.ExtSeq)
            .Select(e => new EventDto(
                e.Minute, e.ExtraMinute, e.Type.ToString(), e.Detail,
                e.TeamId, e.PlayerId, e.AssistPlayerId))
            .ToListAsync(ct);

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(events);
    }

    private static async Task<IResult> GetLineups(HttpContext http, AppDbContext db, Guid id, CancellationToken ct)
    {
        var exists = await db.Fixtures.AsNoTracking().AnyAsync(f => f.Id == id, ct);
        if (!exists)
            return Results.NotFound();

        var lineups = await db.FixtureLineups.AsNoTracking()
            .Where(l => l.FixtureId == id)
            .Select(l => new LineupDto(
                l.TeamId,
                l.Formation,
                l.CoachName,
                l.Players
                    .OrderByDescending(p => p.IsStarter)
                    .ThenBy(p => p.Number)
                    .Select(p => new LineupPlayerDto(p.PlayerId, p.IsStarter, p.Grid, p.Number))
                    .ToList()))
            .ToListAsync(ct);

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(lineups);
    }

    private static async Task<IResult> GetPlayerStats(HttpContext http, AppDbContext db, Guid id, CancellationToken ct)
    {
        var exists = await db.Fixtures.AsNoTracking().AnyAsync(f => f.Id == id, ct);
        if (!exists)
            return Results.NotFound();

        var stats = await db.FixturePlayerStats.AsNoTracking()
            .Where(s => s.FixtureId == id)
            .Select(s => new PlayerStatDto(
                s.PlayerId, s.TeamId, s.Minutes, s.Rating,
                s.Goals, s.Assists, s.ShotsTotal, s.ShotsOn,
                s.Passes, s.PassesAccuracy, s.Tackles, s.Yellow, s.Red))
            .ToListAsync(ct);

        CacheControl.SetPublicMaxAge(http, 60);
        return Results.Ok(stats);
    }
}
