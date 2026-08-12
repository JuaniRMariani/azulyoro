using Azulyoro.Api.Common;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Players;

public static class PlayersEndpoints
{
    public static IEndpointRouteBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/squad", GetSquad);
        app.MapGet("/api/players/{id:guid}", GetPlayer);
        app.MapGet("/api/players/{id:guid}/stats", GetPlayerStats);
        return app;
    }

    private static async Task<IResult> GetSquad(HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var players = await db.Players.AsNoTracking()
            .Where(p => p.IsActive && p.TeamId != null && p.Team!.IsTracked)
            .OrderBy(p => p.Position)
            .ThenBy(p => p.Number)
            .Select(p => new PlayerDto(
                p.Id, p.ExtId, p.Name, p.Firstname, p.Lastname,
                p.Position.ToString(), p.Number, p.Nationality,
                p.PhotoUrl, p.BirthDate, p.Height, p.Weight))
            .ToListAsync(ct);

        CacheControl.SetPublicMaxAge(http, 300);
        return Results.Ok(players);
    }

    private static async Task<IResult> GetPlayer(HttpContext http, AppDbContext db, Guid id, CancellationToken ct)
    {
        var player = await db.Players.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PlayerDto(
                p.Id, p.ExtId, p.Name, p.Firstname, p.Lastname,
                p.Position.ToString(), p.Number, p.Nationality,
                p.PhotoUrl, p.BirthDate, p.Height, p.Weight))
            .FirstOrDefaultAsync(ct);

        if (player is null)
            return Results.NotFound();

        CacheControl.SetPublicMaxAge(http, 300);
        return Results.Ok(player);
    }

    private static async Task<IResult> GetPlayerStats(
        HttpContext http,
        AppDbContext db,
        Guid id,
        CancellationToken ct,
        int? season = null)
    {
        var exists = await db.Players.AsNoTracking().AnyAsync(p => p.Id == id, ct);
        if (!exists)
            return Results.NotFound();

        var query = db.PlayerSeasonStats.AsNoTracking().Where(s => s.PlayerId == id);

        if (season is { } year)
        {
            var seasonIds = db.Seasons.AsNoTracking()
                .Where(s => s.Year == year)
                .Select(s => s.Id);
            query = query.Where(s => seasonIds.Contains(s.SeasonId));
        }

        var stats = await query
            .Select(s => new PlayerSeasonStatDto(
                s.CompetitionId, s.SeasonId, s.Appearances, s.Minutes,
                s.Goals, s.Assists, s.Yellow, s.Red, s.Rating))
            .ToListAsync(ct);

        CacheControl.SetPublicMaxAge(http, 300);
        return Results.Ok(stats);
    }
}
