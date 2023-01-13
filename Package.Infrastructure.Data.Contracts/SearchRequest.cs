namespace Package.Infrastructure.Data.Contracts;
public class SearchRequest<TFilter>
{
    public int PageSize { get; set; }

    public int PageIndex { get; set; }

    public List<Sort>? Sorts { get; set; }

    public TFilter? Filter { get; set; }
}
