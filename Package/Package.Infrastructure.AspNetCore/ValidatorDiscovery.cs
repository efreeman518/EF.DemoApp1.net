using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Package.Infrastructure.AspNetCore;

public interface IValidatorDiscovery
{
    ValidationResult Validate<T>(T model);
    void ValidateAndThrow<T>(T model);
    Task<ValidationResult> ValidateAsync<T>(T model, CancellationToken cancellationToken = default);
    Task ValidateAndThrowAsync<T>(T model, CancellationToken cancellationToken = default);
}

public class ValidatorDiscovery(IServiceProvider serviceProvider) : IValidatorDiscovery
{
    public ValidationResult Validate<T>(T model)
    {
        return serviceProvider.GetRequiredService<IValidator<T>>().Validate(model);
    }

    public void ValidateAndThrow<T>(T model)
    {
        serviceProvider.GetRequiredService<IValidator<T>>().ValidateAndThrow(model);
    }

    public async Task<ValidationResult> ValidateAsync<T>(T model, CancellationToken cancellationToken = default)
    {
        return await serviceProvider.GetRequiredService<IValidator<T>>().ValidateAsync(model, cancellationToken);
    }

    public async Task ValidateAndThrowAsync<T>(T model, CancellationToken cancellationToken = default)
    {
        await serviceProvider.GetRequiredService<IValidator<T>>().ValidateAndThrowAsync(model, cancellationToken: cancellationToken);
    }
}