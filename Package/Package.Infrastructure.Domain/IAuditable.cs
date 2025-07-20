namespace Package.Infrastructure.Domain;

public interface IAuditable<TAuditIdType>
{
    DateTime CreatedDate { get; set; }
    TAuditIdType CreatedBy { get; set; }
    DateTime? UpdatedDate { get; set; }
    TAuditIdType? UpdatedBy { get; set; }
}
