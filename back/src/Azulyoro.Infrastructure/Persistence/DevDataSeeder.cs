using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Infrastructure.Persistence;

/// <summary>
/// Dev-only, idempotent seeder so the read endpoints return coherent data
/// without hitting the external API. Guard invocation behind IsDevelopment().
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Teams.AnyAsync(ct))
            return;

        var season = new Season { Year = 2026, IsCurrent = true };

        var liga = new Competition
        {
            ExtId = 128,
            Name = "Liga Profesional",
            Type = CompetitionType.League,
            Country = "Argentina",
            LogoUrl = "https://media.api-sports.io/football/leagues/128.png",
        };

        var boca = new Team
        {
            ExtId = 451,
            Name = "Boca Juniors",
            ShortName = "Boca",
            LogoUrl = "https://media.api-sports.io/football/teams/451.png",
            Founded = 1905,
            VenueName = "La Bombonera",
            VenueCity = "Buenos Aires",
            IsTracked = true,
        };

        var river = new Team
        {
            ExtId = 435,
            Name = "River Plate",
            ShortName = "River",
            LogoUrl = "https://media.api-sports.io/football/teams/435.png",
            Founded = 1901,
            VenueName = "Monumental",
            VenueCity = "Buenos Aires",
            IsTracked = false,
        };

        var players = new[]
        {
            new Player
            {
                ExtId = 1001, TeamId = boca.Id, Team = boca,
                Name = "Sergio Romero", Firstname = "Sergio", Lastname = "Romero",
                Position = PlayerPosition.Goalkeeper, Number = 1,
                Nationality = "Argentina", BirthDate = new DateOnly(1987, 2, 22),
                Height = 192, Weight = 82, IsActive = true,
                PhotoUrl = "https://media.api-sports.io/football/players/1001.png",
            },
            new Player
            {
                ExtId = 1002, TeamId = boca.Id, Team = boca,
                Name = "Marcos Rojo", Firstname = "Marcos", Lastname = "Rojo",
                Position = PlayerPosition.Defender, Number = 6,
                Nationality = "Argentina", BirthDate = new DateOnly(1990, 3, 20),
                Height = 189, Weight = 82, IsActive = true,
                PhotoUrl = "https://media.api-sports.io/football/players/1002.png",
            },
            new Player
            {
                ExtId = 1003, TeamId = boca.Id, Team = boca,
                Name = "Edinson Cavani", Firstname = "Edinson", Lastname = "Cavani",
                Position = PlayerPosition.Attacker, Number = 10,
                Nationality = "Uruguay", BirthDate = new DateOnly(1987, 2, 14),
                Height = 184, Weight = 77, IsActive = true,
                PhotoUrl = "https://media.api-sports.io/football/players/1003.png",
            },
        };

        var upcoming = new Fixture
        {
            ExtId = 900001,
            Competition = liga, CompetitionId = liga.Id,
            Season = season, SeasonId = season.Id,
            Round = "Fecha 5",
            DateUtc = DateTime.UtcNow.AddDays(7),
            Status = FixtureStatus.NotStarted,
            VenueName = "La Bombonera",
            HomeTeam = boca, HomeTeamId = boca.Id,
            AwayTeam = river, AwayTeamId = river.Id,
            IsBoca = true,
        };

        var finished = new Fixture
        {
            ExtId = 900002,
            Competition = liga, CompetitionId = liga.Id,
            Season = season, SeasonId = season.Id,
            Round = "Fecha 4",
            DateUtc = DateTime.UtcNow.AddDays(-7),
            Status = FixtureStatus.Finished,
            Elapsed = 90,
            VenueName = "Monumental",
            HomeTeam = river, HomeTeamId = river.Id,
            AwayTeam = boca, AwayTeamId = boca.Id,
            HomeGoals = 1, AwayGoals = 2,
            HtHome = 0, HtAway = 1,
            FtHome = 1, FtAway = 2,
            IsBoca = true,
        };

        var events = new[]
        {
            new FixtureEvent
            {
                Fixture = finished, FixtureId = finished.Id,
                ExtSeq = 1, Minute = 23,
                TeamId = boca.Id, PlayerId = players[2].Id,
                Type = EventType.Goal, Detail = "Normal Goal",
            },
            new FixtureEvent
            {
                Fixture = finished, FixtureId = finished.Id,
                ExtSeq = 2, Minute = 55,
                TeamId = river.Id,
                Type = EventType.Goal, Detail = "Penalty",
            },
            new FixtureEvent
            {
                Fixture = finished, FixtureId = finished.Id,
                ExtSeq = 3, Minute = 78,
                TeamId = boca.Id, PlayerId = players[1].Id,
                Type = EventType.Goal, Detail = "Header",
            },
        };

        var standing = new Standing
        {
            Competition = liga, CompetitionId = liga.Id,
            SeasonId = season.Id,
            Team = boca, TeamId = boca.Id,
            Rank = 1, Points = 10,
            Played = 4, Win = 3, Draw = 1, Lose = 0,
            GoalsFor = 8, GoalsAgainst = 3, GoalsDiff = 5,
            Form = "WWDW", GroupName = "Liga Profesional",
        };

        var playerSeasonStat = new PlayerSeasonStats
        {
            PlayerId = players[2].Id,
            CompetitionId = liga.Id,
            SeasonId = season.Id,
            Appearances = 4, Minutes = 340,
            Goals = 3, Assists = 1, Yellow = 1, Red = 0,
            Rating = 7.4m,
        };

        db.Seasons.Add(season);
        db.Competitions.Add(liga);
        db.Teams.AddRange(boca, river);
        db.Players.AddRange(players);
        db.Fixtures.AddRange(upcoming, finished);
        db.FixtureEvents.AddRange(events);
        db.Standings.Add(standing);
        db.PlayerSeasonStats.Add(playerSeasonStat);

        await db.SaveChangesAsync(ct);
    }
}
