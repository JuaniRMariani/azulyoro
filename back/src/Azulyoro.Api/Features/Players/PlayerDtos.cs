namespace Azulyoro.Api.Features.Players;

public record PlayerDto(
    Guid Id,
    int ExtId,
    string Name,
    string? Firstname,
    string? Lastname,
    string Position,
    int? Number,
    string? Nationality,
    string? PhotoUrl,
    DateOnly? BirthDate,
    int? Height,
    int? Weight);

public record PlayerSeasonStatDto(
    Guid CompetitionId,
    Guid SeasonId,
    int Appearances,
    int Minutes,
    int Goals,
    int Assists,
    int Yellow,
    int Red,
    decimal? Rating);
