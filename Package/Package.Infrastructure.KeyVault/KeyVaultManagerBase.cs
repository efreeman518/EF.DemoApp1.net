using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Package.Infrastructure.KeyVault;

public abstract class KeyVaultManagerBase : IKeyVaultManager
{
    private readonly ILogger<KeyVaultManagerBase> _logger;
    private readonly KeyVaultManagerSettingsBase _settings;
    private readonly SecretClient _secretClient;
    private readonly KeyClient _keyClient;
    private readonly CertificateClient _certClient;
    
    protected KeyVaultManagerBase(ILogger<KeyVaultManagerBase> logger, IOptions<KeyVaultManagerSettingsBase> settings,
        IAzureClientFactory<SecretClient> clientFactorySecret, IAzureClientFactory<KeyClient> clientFactoryKey, IAzureClientFactory<CertificateClient> clientFactoryCert)
    {
        _logger = logger;
        _settings = settings.Value;
        _secretClient = clientFactorySecret.CreateClient(_settings.KeyVaultClientName);
        _keyClient = clientFactoryKey.CreateClient(_settings.KeyVaultClientName);
        _certClient = clientFactoryCert.CreateClient(_settings.KeyVaultClientName);
    }

    public async Task<string?> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        _ = _settings.GetHashCode(); //compilet warning

        _logger.LogInformation("GetSecretAsync - {name} {version}", name, version);
        var response =  await _secretClient.GetSecretAsync(name, version, cancellationToken);
        return response.Value.Value;
    }

    public async Task<string?> SaveSecretAsync(string name, string? value = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SaveSecretAsync - {name}", name);
        var response = await _secretClient.SetSecretAsync(name, value, cancellationToken);
        return response.Value.Value;
    }

    public async Task<JsonWebKey?> GetKeyAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetKeyAsync - {name} {version}", name, version);
        var response = await _keyClient.GetKeyAsync(name, version, cancellationToken);
        return response.Value.Key;
    }

    public async Task<JsonWebKey?> CreateKeyAsync(string name, KeyType keyType, CreateKeyOptions? options = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateKeyAsync - {name} {keyType}", name, keyType);
        var response = await _keyClient.CreateKeyAsync(name, keyType, options, cancellationToken);
        return response.Value.Key;
    }

    public async Task<byte[]?> GetCertAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetCertAsync - {name}", name);
        var response = await _certClient.GetCertificateAsync(name, cancellationToken);
        return response.Value.Cer;
    }

    public async Task<byte[]?> ImportCertAsync(ImportCertificateOptions importOptions, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ImportCertAsync");
        var response = await _certClient.ImportCertificateAsync(importOptions, cancellationToken);
        return response.Value.Cer;
    }
}
