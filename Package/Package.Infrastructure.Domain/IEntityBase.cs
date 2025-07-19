namespace Package.Infrastructure.Domain;

public interface IEntityBase<TId>
{
    public TId Id { get; init; }
}
