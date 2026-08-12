using Azulyoro.Domain.Common;

namespace Azulyoro.Domain.Entities;

/// <summary>
/// Per-resource sync bookkeeping so jobs can reason about staleness and
/// surface failures. Keyed by a stable <see cref="Resource"/> string.
/// </summary>
public class SyncState : Entity
{
    public string Resource { get; set; } = string.Empty;
    public DateTime? LastRunAt { get; set; }
    public DateTime? LastOkAt { get; set; }
    public string? LastError { get; set; }
}
