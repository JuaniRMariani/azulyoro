namespace Azulyoro.Domain.Enums;

public static class FixtureStatusExtensions
{
    /// <summary>Statuses where the match is currently in play (poll target).</summary>
    public static bool IsLive(this FixtureStatus status) => status is
        FixtureStatus.FirstHalf or
        FixtureStatus.HalfTime or
        FixtureStatus.SecondHalf or
        FixtureStatus.ExtraTime or
        FixtureStatus.BreakTime or
        FixtureStatus.Penalty;

    /// <summary>Terminal statuses — live polling must stop here.</summary>
    public static bool IsFinished(this FixtureStatus status) => status is
        FixtureStatus.Finished or
        FixtureStatus.Cancelled or
        FixtureStatus.Abandoned or
        FixtureStatus.Awarded or
        FixtureStatus.WalkOver;

    /// <summary>Map an API-Football short code (e.g. "1H", "FT") to the enum.</summary>
    public static FixtureStatus FromApiShort(string? code) => code switch
    {
        "NS" or "TBD" => FixtureStatus.NotStarted,
        "1H" => FixtureStatus.FirstHalf,
        "HT" => FixtureStatus.HalfTime,
        "2H" => FixtureStatus.SecondHalf,
        "ET" => FixtureStatus.ExtraTime,
        "BT" => FixtureStatus.BreakTime,
        "P" => FixtureStatus.Penalty,
        "SUSP" => FixtureStatus.Suspended,
        "INT" => FixtureStatus.Interrupted,
        "FT" or "AET" or "PEN" => FixtureStatus.Finished,
        "PST" => FixtureStatus.Postponed,
        "CANC" => FixtureStatus.Cancelled,
        "ABD" => FixtureStatus.Abandoned,
        "AWD" => FixtureStatus.Awarded,
        "WO" => FixtureStatus.WalkOver,
        _ => FixtureStatus.Unknown,
    };
}
