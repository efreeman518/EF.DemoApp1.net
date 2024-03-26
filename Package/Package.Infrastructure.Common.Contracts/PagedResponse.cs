namespace Package.Infrastructure.Common.Contracts;

public class PagedResponse<T>
{
    public int PageSize { get; set; }
    public int PageIndex { get; set; }
    public int Total { get; set; }
    public IReadOnlyList<T> Data { get; set; } = [];
}
