namespace Azulyoro.Domain.Enums;

public enum StagingStatus
{
    Pending,
    Approved,
    Rejected,
}

public enum ArticleCategory
{
    News,
    Rumor,
    Editorial,
}

public enum ArticleStatus
{
    Draft,
    Published,
    Archived,
}

public enum SourceType
{
    Rss,
    Html,
}
