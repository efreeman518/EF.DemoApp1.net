namespace Package.Infrastructure.Common;
public interface IValidationHelper
{
    FluentValidation.Results.ValidationResult Validate<T>(T model);
    void ValidateAndThrow<T>(T model);
    Task<FluentValidation.Results.ValidationResult> ValidateAsync<T>(T model, CancellationToken cancellationToken = default);
    Task ValidateAndThrowAsync<T>(T model, CancellationToken cancellationToken = default);
}
