namespace Package.Infrastructure.Common.Contracts;

public record SearchRequest<TFilter>
{
    public int PageSize { get; init; }

    public int PageIndex { get; init; }

    public List<Sort>? Sorts { get; init; }

    public TFilter? Filter { get; init; }
}
