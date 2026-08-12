namespace Azulyoro.Infrastructure.Sync;

public sealed class SportsSyncOptions
{
    public const string SectionName = "SportsSync";

    public int TeamExtId { get; set; } = 451;
    public int PrimaryLeagueExtId { get; set; } = 128;

    /// <summary>
    /// API-Football season to ingest. It is configurable because provider
    /// plans can restrict which seasons are available.
    /// </summary>
    public int Season { get; set; } = 2024;
}
