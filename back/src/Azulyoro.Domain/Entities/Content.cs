using Azulyoro.Domain.Common;
using Azulyoro.Domain.Enums;

namespace Azulyoro.Domain.Entities;

/// <summary>A news source (RSS-first). Robots + rate-limit metadata drive polite scraping.</summary>
public class Source : Entity
{
    public string Name { get; set; } = string.Empty;
    public string RssUrl { get; set; } = string.Empty;
    public SourceType Type { get; set; } = SourceType.Rss;
    public bool Active { get; set; }
    public int RateLimitSeconds { get; set; } = 3;

    /// <summary>Regex/keyword OR-list to keep only Boca-relevant items (section feeds).</summary>
    public string? KeywordFilter { get; set; }
    public bool RobotsOk { get; set; }

    // Conditional-request bookkeeping.
    public DateTime? LastFetchedAt { get; set; }
    public string? Etag { get; set; }
    public string? LastModified { get; set; }
}

/// <summary>
/// Raw scraped item awaiting human moderation. Stores ONLY title/excerpt/source
/// (never the full article body) per the copyright rules.
/// </summary>
public class StagingArticle : Entity
{
    public Guid? SourceId { get; set; }
    public Source? Source { get; set; }

    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>Canonical URL hash (strip UTM) for dedup.</summary>
    public string UrlHash { get; set; } = string.Empty;
    public string TitleHash { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? PublishedAtSource { get; set; }
    public DateTime ScrapedAt { get; set; }

    public StagingStatus Status { get; set; } = StagingStatus.Pending;
    public ArticleCategory Category { get; set; } = ArticleCategory.News;

    public Guid? ReviewedBy { get; set; } // loose ref to users (Phase 4)
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Published/editable article. Body lives in translations (es/en).</summary>
public class Article : Entity
{
    public string Slug { get; set; } = string.Empty;
    public ArticleCategory Category { get; set; } = ArticleCategory.News;
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    public string? CoverImageUrl { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public Guid? AuthorUserId { get; set; } // loose ref to users (Phase 4)
    public bool IsMembersOnly { get; set; }
    public DateTime? PublishedAt { get; set; }

    public Guid? StagingId { get; set; }

    public ICollection<ArticleTranslation> Translations { get; set; } = new List<ArticleTranslation>();
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}

public class ArticleTranslation : Entity
{
    public Guid ArticleId { get; set; }
    public Article? Article { get; set; }
    public string Locale { get; set; } = "es";

    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? BodyHtml { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class Tag : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}

/// <summary>M:N join between articles and tags (composite key, no surrogate id).</summary>
public class ArticleTag
{
    public Guid ArticleId { get; set; }
    public Article? Article { get; set; }
    public Guid TagId { get; set; }
    public Tag? Tag { get; set; }
}
