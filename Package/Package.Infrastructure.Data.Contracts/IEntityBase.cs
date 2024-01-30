namespace Package.Infrastructure.Data.Contracts;

public interface IEntityBase<TId>
{
    public TId Id { get; init; }
}
