namespace EventEase.Models;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
