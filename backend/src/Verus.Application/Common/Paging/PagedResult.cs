namespace Verus.Application.Common.Paging;

/// <summary>A page of results plus the information needed to navigate the rest.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new([.. Items.Select(selector)], Page, PageSize, TotalCount);
}

public static class PagedResult
{
    /// <summary>Pages an in-memory, already ordered sequence.</summary>
    public static PagedResult<T> From<T>(IReadOnlyCollection<T> source, PageRequest page)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(page);
        return new([.. source.Skip(page.Skip).Take(page.PageSize)], page.Page, page.PageSize, source.Count);
    }
}
