using Azulyoro.Api.Common;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Competitions;

public record CompetitionDto(
    Guid Id,
    int ExtId,
    string Name,
    string Type,
    string? Country,
    string? LogoUrl);

public static class CompetitionsEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/competitions", GetCompetitions);
        return app;
    }

    private static async Task<IResult> GetCompetitions(HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var competitions = await db.Competitions.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CompetitionDto(
                c.Id, c.ExtId, c.Name, c.Type.ToString(), c.Country, c.LogoUrl))
            .ToListAsync(ct);

        CacheControl.SetPublicMaxAge(http, 300);
        return Results.Ok(competitions);
    }
}
