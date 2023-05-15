using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Package.Infrastructure.Common;
public class ValidationHelper : IValidationHelper
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationHelper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public FluentValidation.Results.ValidationResult Validate<T>(T model)
    {
        return _serviceProvider.GetRequiredService<IValidator<T>>().Validate(model);
    }

    public void ValidateAndThrow<T>(T model)
    {
        _serviceProvider.GetRequiredService<IValidator<T>>().ValidateAndThrow(model);
    }

    public async Task<FluentValidation.Results.ValidationResult> ValidateAsync<T>(T model, CancellationToken cancellationToken = default)
    {
        return await _serviceProvider.GetRequiredService<IValidator<T>>().ValidateAsync(model, cancellationToken);
    }

    public async Task ValidateAndThrowAsync<T>(T model, CancellationToken cancellationToken = default)
    {
        await _serviceProvider.GetRequiredService<IValidator<T>>().ValidateAndThrowAsync(model, cancellationToken: cancellationToken);
    }
}