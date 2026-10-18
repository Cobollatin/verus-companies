using Verus.Application.Common.Paging;

namespace Verus.Api.Common;

/// <summary>A page of items plus the information needed to navigate the rest.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);

internal static class PagedResponse
{
    public static PagedResponse<T> From<T>(PagedResult<T> page) =>
        new(
            page.Items,
            page.Page,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            page.HasNextPage,
            page.HasPreviousPage
        );
}
