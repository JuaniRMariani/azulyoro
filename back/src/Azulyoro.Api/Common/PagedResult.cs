namespace Azulyoro.Api.Common;

/// <summary>Standard envelope for paginated list responses.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
