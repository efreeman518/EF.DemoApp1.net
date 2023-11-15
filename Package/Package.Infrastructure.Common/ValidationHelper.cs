using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Package.Infrastructure.Common;
public class ValidationHelper(IServiceProvider serviceProvider) : IValidationHelper
{
    public FluentValidation.Results.ValidationResult Validate<T>(T model)
    {
        return serviceProvider.GetRequiredService<IValidator<T>>().Validate(model);
    }

    public void ValidateAndThrow<T>(T model)
    {
        serviceProvider.GetRequiredService<IValidator<T>>().ValidateAndThrow(model);
    }

    public async Task<FluentValidation.Results.ValidationResult> ValidateAsync<T>(T model, CancellationToken cancellationToken = default)
    {
        return await serviceProvider.GetRequiredService<IValidator<T>>().ValidateAsync(model, cancellationToken);
    }

    public async Task ValidateAndThrowAsync<T>(T model, CancellationToken cancellationToken = default)
    {
        await serviceProvider.GetRequiredService<IValidator<T>>().ValidateAndThrowAsync(model, cancellationToken: cancellationToken);
    }
}