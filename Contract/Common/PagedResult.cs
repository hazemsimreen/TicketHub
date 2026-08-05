namespace Contract.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;

        TotalPages = pageSize == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)pageSize);
    }
}