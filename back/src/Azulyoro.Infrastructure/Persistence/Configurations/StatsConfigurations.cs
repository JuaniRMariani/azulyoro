using Azulyoro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azulyoro.Infrastructure.Persistence.Configurations;

public class PlayerSeasonStatsConfiguration : IEntityTypeConfiguration<PlayerSeasonStats>
{
    public void Configure(EntityTypeBuilder<PlayerSeasonStats> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PlayerId, x.CompetitionId, x.SeasonId }).IsUnique();
        b.Property(x => x.Rating).HasPrecision(5, 2);

        b.HasOne(x => x.Player).WithMany()
            .HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Competition>().WithMany()
            .HasForeignKey(x => x.CompetitionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Season>().WithMany()
            .HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StandingConfiguration : IEntityTypeConfiguration<Standing>
{
    public void Configure(EntityTypeBuilder<Standing> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.CompetitionId, x.SeasonId, x.TeamId, x.GroupName }).IsUnique();
        b.Property(x => x.Form).HasMaxLength(20);
        b.Property(x => x.GroupName).HasMaxLength(60).HasDefaultValue(string.Empty);

        b.HasOne(x => x.Competition).WithMany()
            .HasForeignKey(x => x.CompetitionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Season>().WithMany()
            .HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Team).WithMany()
            .HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Resource).IsUnique();
        b.Property(x => x.Resource).HasMaxLength(80).IsRequired();
        b.Property(x => x.LastError).HasMaxLength(2000);
    }
}
