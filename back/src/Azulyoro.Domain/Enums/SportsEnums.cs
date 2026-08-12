namespace Azulyoro.Domain.Enums;

public enum CompetitionType
{
    League,
    Cup,
}

public enum PlayerPosition
{
    Unknown = 0,
    Goalkeeper,
    Defender,
    Midfielder,
    Attacker,
}

/// <summary>
/// Fixture status mirroring API-Football short codes. Persisted as string.
/// </summary>
public enum FixtureStatus
{
    Unknown = 0,
    NotStarted,   // NS / TBD
    FirstHalf,    // 1H
    HalfTime,     // HT
    SecondHalf,   // 2H
    ExtraTime,    // ET
    BreakTime,    // BT
    Penalty,      // P
    Suspended,    // SUSP
    Interrupted,  // INT
    Finished,     // FT / AET / PEN
    Postponed,    // PST
    Cancelled,    // CANC
    Abandoned,    // ABD
    Awarded,      // AWD
    WalkOver,     // WO
}

public enum EventType
{
    Goal,
    Card,
    Substitution,
    Var,
    Other,
}
