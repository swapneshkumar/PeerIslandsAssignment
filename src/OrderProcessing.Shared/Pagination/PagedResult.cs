namespace OrderProcessing.Shared.Pagination;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, long TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
