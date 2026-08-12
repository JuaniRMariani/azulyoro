using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Infrastructure.Persistence;

/// <summary>
/// Root EF Core context. Application tables live under the "app" schema;
/// Hangfire manages its own "hangfire" schema separately.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string Schema = "app";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        // Entity configurations are applied here in Phase 1 via
        // ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly).
        base.OnModelCreating(modelBuilder);
    }
}
