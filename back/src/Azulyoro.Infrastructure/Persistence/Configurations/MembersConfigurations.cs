using Azulyoro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azulyoro.Infrastructure.Persistence.Configurations;

public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Locale).HasMaxLength(5);
        b.Property(x => x.ConfirmTokenHash).HasMaxLength(128);
        b.Property(x => x.ConfirmedIp).HasMaxLength(64);
    }
}

public class LegalPageConfiguration : IEntityTypeConfiguration<LegalPage>
{
    public void Configure(EntityTypeBuilder<LegalPage> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.Slug, x.Locale }).IsUnique();
        b.Property(x => x.Slug).HasMaxLength(40).IsRequired();
        b.Property(x => x.Locale).HasMaxLength(5).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        // BodyHtml stays unbounded text.
    }
}
