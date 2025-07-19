namespace Package.Infrastructure.Data.Contracts;

public interface xIEntityBase<TId>
{
    public TId Id { get; init; }
}
