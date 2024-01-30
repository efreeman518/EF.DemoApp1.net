namespace Package.Infrastructure.Data.Contracts;

public interface IAuditable<TId>
{
    DateTime CreatedDate { get; set; }
    TId CreatedBy { get; set; }
    DateTime? UpdatedDate { get; set; }
    TId? UpdatedBy { get; set; }
}
