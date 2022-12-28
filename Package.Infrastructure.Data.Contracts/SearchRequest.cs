namespace Package.Infrastructure.Data.Contracts;
public class SearchRequest<T> where T : class
{
    public int PageSize { get; set; }

    public int PageIndex { get; set; }

    public List<Sort>? Sorts { get; set; }

    public T? FilterItem { get; set; }
}
