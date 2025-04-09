using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SampleApp.UI1.Model.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public partial class TodoNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var name = value as string;
        if (!string.IsNullOrWhiteSpace(name) && NameValidRegex().IsMatch(name))
        {
            return ValidationResult.Success;
        }
        //ErrorMessage is optional on the attribute
        return new ValidationResult(ErrorMessage ?? "Name must contain the letter 'a'.");
    }

    [GeneratedRegex("a", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NameValidRegex();
}
