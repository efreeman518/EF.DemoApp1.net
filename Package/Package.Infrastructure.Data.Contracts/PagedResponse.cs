namespace Package.Infrastructure.Data.Contracts;

public class PagedResponse<T>
{
    public int PageSize { get; set; }
    public int PageIndex { get; set; }
    public int Total { get; set; }
    public List<T> Data { get; set; } = [];
}
