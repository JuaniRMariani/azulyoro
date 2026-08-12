namespace Azulyoro.Domain.Common;

/// <summary>
/// Base type for all persisted aggregates. PK is a UUID v7 generated
/// app-side (time-ordered, index-friendly). Timestamps are stamped by the
/// DbContext on save.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
