namespace Package.Infrastructure.Common.Contracts;

public record SearchRequest<TFilter>
{
    public int PageSize { get; set; }

    public int PageIndex { get; set; }

    public IEnumerable<Sort>? Sorts { get; set; }

    public TFilter? Filter { get; set; }
}
