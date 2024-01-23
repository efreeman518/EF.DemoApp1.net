using Package.Infrastructure.Data.Contracts;

namespace Package.Infrastructure.Test.Integration.Model;

public class SomeTenantBasedDto : ITenantEntity
{
    public Guid TenantId { get; init; }
    public Guid Id { get; init; }
    public string? Name { get; set; }
    public TodoItemStatus Status { get; set; }

}