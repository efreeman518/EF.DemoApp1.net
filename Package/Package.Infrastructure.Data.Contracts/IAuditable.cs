namespace Package.Infrastructure.Data.Contracts;

public interface IAuditable<TAuditIdType>
{
    DateTime CreatedDate { get; set; }
    TAuditIdType CreatedBy { get; set; }
    DateTime? UpdatedDate { get; set; }
    TAuditIdType? UpdatedBy { get; set; }
}
