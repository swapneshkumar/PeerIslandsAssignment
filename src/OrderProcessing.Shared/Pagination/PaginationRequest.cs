namespace OrderProcessing.Shared.Pagination;

public sealed record PaginationRequest(int PageNumber = 1, int PageSize = 20, string? SortBy = null, string? SortDirection = "desc")
{
    public int Skip => (Math.Max(PageNumber, 1) - 1) * Take;
    public int Take => Math.Clamp(PageSize, 1, 100);
}
