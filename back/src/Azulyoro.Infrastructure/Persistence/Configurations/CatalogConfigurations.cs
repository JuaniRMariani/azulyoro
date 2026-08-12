using Azulyoro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azulyoro.Infrastructure.Persistence.Configurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Year).IsUnique();
    }
}

public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ExtId).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Country).HasMaxLength(80);
    }
}

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ExtId).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.ShortName).HasMaxLength(60);
        b.Property(x => x.VenueName).HasMaxLength(120);
        b.Property(x => x.VenueCity).HasMaxLength(80);
    }
}

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ExtId).IsUnique();
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Firstname).HasMaxLength(120);
        b.Property(x => x.Lastname).HasMaxLength(120);
        b.Property(x => x.Nationality).HasMaxLength(80);
        b.Property(x => x.Position).HasConversion<string>().HasMaxLength(16);

        b.HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
