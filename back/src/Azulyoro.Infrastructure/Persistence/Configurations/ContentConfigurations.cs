using Azulyoro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azulyoro.Infrastructure.Persistence.Configurations;

public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.RssUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.KeywordFilter).HasMaxLength(500);
        b.Property(x => x.Etag).HasMaxLength(200);
        b.Property(x => x.LastModified).HasMaxLength(100);
        b.HasIndex(x => x.Active);
    }
}

public class StagingArticleConfiguration : IEntityTypeConfiguration<StagingArticle>
{
    public void Configure(EntityTypeBuilder<StagingArticle> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UrlHash).IsUnique();
        b.HasIndex(x => x.TitleHash);
        b.HasIndex(x => x.Status);

        b.Property(x => x.SourceName).HasMaxLength(120).IsRequired();
        b.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.UrlHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.TitleHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Excerpt).HasMaxLength(1200);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(16);

        b.HasOne(x => x.Source).WithMany()
            .HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.Status, x.PublishedAt });

        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.SourceName).HasMaxLength(120);
        b.Property(x => x.SourceUrl).HasMaxLength(1000);

        b.HasOne<StagingArticle>().WithMany()
            .HasForeignKey(x => x.StagingId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ArticleTranslationConfiguration : IEntityTypeConfiguration<ArticleTranslation>
{
    public void Configure(EntityTypeBuilder<ArticleTranslation> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ArticleId, x.Locale }).IsUnique();

        b.Property(x => x.Locale).HasMaxLength(5).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(600);
        b.Property(x => x.MetaTitle).HasMaxLength(200);
        b.Property(x => x.MetaDescription).HasMaxLength(320);
        // BodyHtml stays as unbounded text.

        b.HasOne(x => x.Article).WithMany(a => a.Translations)
            .HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
    }
}

public class ArticleTagConfiguration : IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> b)
    {
        b.HasKey(x => new { x.ArticleId, x.TagId });

        b.HasOne(x => x.Article).WithMany(a => a.ArticleTags)
            .HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Tag).WithMany(t => t.ArticleTags)
            .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
