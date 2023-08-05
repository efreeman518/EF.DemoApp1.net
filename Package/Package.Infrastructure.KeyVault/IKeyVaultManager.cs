using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;

namespace Package.Infrastructure.KeyVault;
public interface IKeyVaultManager
{
    Task<string?> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default);
    Task<string?> SaveSecretAsync(string name, string? value = null, CancellationToken cancellationToken = default);
    Task<JsonWebKey?> GetKeyAsync(string name, string? version = null, CancellationToken cancellationToken = default);
    Task<JsonWebKey?> CreateKeyAsync(string name, KeyType keyType, CreateKeyOptions? options = null, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCertAsync(string name, CancellationToken cancellationToken = default);
    Task<byte[]?> ImportCertAsync(ImportCertificateOptions importOptions, CancellationToken cancellationToken = default);
}
