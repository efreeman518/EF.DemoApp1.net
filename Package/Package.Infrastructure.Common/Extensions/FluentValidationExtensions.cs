namespace Package.Infrastructure.Common.Extensions;
public static class FluentValidationExtensions
{
    public static ValidationResult ToValidationResult(this FluentValidation.Results.ValidationResult validationResult)
    {
        var result = new ValidationResult(validationResult.IsValid)
        {
            Messages = validationResult.Errors.Select(failure => $"{failure.PropertyName}: {failure.ErrorMessage}").ToList()
        };
        return result;
    }
}
