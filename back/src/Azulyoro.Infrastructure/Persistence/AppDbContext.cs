using Azulyoro.Domain.Common;
using Azulyoro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Infrastructure.Persistence;

/// <summary>
/// Root EF Core context. Application tables live under the "app" schema;
/// Hangfire manages its own "hangfire" schema separately.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string Schema = "app";

    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Fixture> Fixtures => Set<Fixture>();
    public DbSet<FixtureEvent> FixtureEvents => Set<FixtureEvent>();
    public DbSet<FixtureLineup> FixtureLineups => Set<FixtureLineup>();
    public DbSet<FixtureLineupPlayer> FixtureLineupPlayers => Set<FixtureLineupPlayer>();
    public DbSet<FixturePlayerStats> FixturePlayerStats => Set<FixturePlayerStats>();
    public DbSet<PlayerSeasonStats> PlayerSeasonStats => Set<PlayerSeasonStats>();
    public DbSet<Standing> Standings => Set<Standing>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // PKs are app-generated UUID v7; never let the store overwrite them.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entity.ClrType))
            {
                modelBuilder.Entity(entity.ClrType)
                    .Property(nameof(Entity.Id))
                    .ValueGeneratedNever();
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
