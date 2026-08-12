using Azulyoro.Domain.Common;
using Azulyoro.Domain.Enums;

namespace Azulyoro.Domain.Entities;

public class Season : Entity
{
    public int Year { get; set; }
    public bool IsCurrent { get; set; }
}

public class Competition : Entity
{
    public int ExtId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CompetitionType Type { get; set; }
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
}

public class Team : Entity
{
    public int ExtId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? LogoUrl { get; set; }
    public int? Founded { get; set; }
    public string? VenueName { get; set; }
    public string? VenueCity { get; set; }

    /// <summary>Teams we ingest squads/fixtures for (Boca = true).</summary>
    public bool IsTracked { get; set; }
}

public class Player : Entity
{
    public int ExtId { get; set; }
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Nationality { get; set; }
    public int? Height { get; set; }
    public int? Weight { get; set; }
    public PlayerPosition Position { get; set; }
    public int? Number { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
