namespace Verus.Application.Common.Paging;

/// <summary>1-based page request.</summary>
public sealed record PageRequest(int Page, int PageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;
}
