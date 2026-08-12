namespace Azulyoro.Api.Features.Matches;

public record MatchDto(
    Guid Id,
    int ExtId,
    DateTime DateUtc,
    string Status,
    Guid CompetitionId,
    string? CompetitionName,
    Guid HomeTeamId,
    string? HomeTeamName,
    string? HomeTeamLogoUrl,
    Guid AwayTeamId,
    string? AwayTeamName,
    string? AwayTeamLogoUrl,
    int? HomeGoals,
    int? AwayGoals,
    bool IsBoca);

public record MatchDetailDto(
    Guid Id,
    int ExtId,
    DateTime DateUtc,
    string Status,
    Guid CompetitionId,
    string? CompetitionName,
    Guid HomeTeamId,
    string? HomeTeamName,
    string? HomeTeamLogoUrl,
    Guid AwayTeamId,
    string? AwayTeamName,
    string? AwayTeamLogoUrl,
    int? HomeGoals,
    int? AwayGoals,
    bool IsBoca,
    string? Venue,
    string? Round,
    int? HtHome,
    int? HtAway,
    int? FtHome,
    int? FtAway,
    int? Elapsed);

public record EventDto(
    int Minute,
    int? ExtraMinute,
    string Type,
    string? Detail,
    Guid? TeamId,
    Guid? PlayerId,
    Guid? AssistPlayerId);

public record LineupPlayerDto(
    Guid PlayerId,
    bool IsStarter,
    string? Grid,
    int? Number);

public record LineupDto(
    Guid TeamId,
    string? Formation,
    string? CoachName,
    IReadOnlyList<LineupPlayerDto> Players);

public record PlayerStatDto(
    Guid PlayerId,
    Guid TeamId,
    int? Minutes,
    decimal? Rating,
    int Goals,
    int Assists,
    int ShotsTotal,
    int ShotsOn,
    int Passes,
    int? PassesAccuracy,
    int Tackles,
    int Yellow,
    int Red);
