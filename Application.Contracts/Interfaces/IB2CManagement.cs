namespace Application.Contracts.Interfaces;
public interface IB2CManagement
{
    Task<string?> CreateUserAsync(string displayName, string email, string userTenantId, IEnumerable<string> roles);
}
