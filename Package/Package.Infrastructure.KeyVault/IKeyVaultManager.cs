using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace Package.Infrastructure.KeyVault;

public interface IKeyVaultManager
{
    Task<string?> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default);
    Task<string?> SaveSecretAsync(string name, string? value = null, CancellationToken cancellationToken = default);
    Task<DeletedSecret> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default);
    Task<JsonWebKey?> GetKeyAsync(string name, string? version = null, CancellationToken cancellationToken = default);
    Task<JsonWebKey?> CreateKeyAsync(string name, KeyType keyType, CreateKeyOptions? options = null, CancellationToken cancellationToken = default);
    Task<KeyRotationPolicy> UpdateKeyRotationPolicyAsync(string name, KeyRotationPolicy policy, CancellationToken cancellationToken = default);
    Task<JsonWebKey?> RotateKeyAsync(string name, CancellationToken cancellationToken = default);
    Task<JsonWebKey?> DeleteKeyAsync(string name, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCertAsync(string certificateName, CancellationToken cancellationToken = default);
    Task<byte[]?> ImportCertAsync(ImportCertificateOptions importCertificateOptions, CancellationToken cancellationToken = default);
    //Task<CertificateOperation> StartCreateCertAsync(string certificateName, CertificatePolicy policy, bool? enabled = default, IDictionary<string, string> tags = default, CancellationToken cancellationToken = default);
    //Task<DeleteCertificateOperation?> StartDeleteCertificateAsync(string certificateName, CancellationToken cancellationToken = default);
    //Task<CertificateIssuer> CreateIssuerAsync(CertificateIssuer issuer, CancellationToken cancellationToken = default);
    //Task<CertificateIssuer> DeleteIssuerAsync(string issuerName, CancellationToken cancellationToken = default);

}
