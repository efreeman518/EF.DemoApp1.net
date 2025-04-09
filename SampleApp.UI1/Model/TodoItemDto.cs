using SampleApp.UI1.Model.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SampleApp.UI1.Model;

/// <summary>
/// Option FluentValidation (no validation attributes) - https://youtu.be/ED99nB6SoWM?t=640
/// </summary>
public record TodoItemDto : IValidatableObject
{
    public Guid? Id { get; init; }

    [TodoNameAttribute]
    [RegularExpression(".*a.*", ErrorMessage = "Name must include the letter 'a'")]
    public string Name { get; set; } = null!;

    //[JsonConverter(typeof(JsonNumberEnumConverter<TodoItemStatus>))]
    public TodoItemStatus Status { get; set; } = TodoItemStatus.Created;


    public string? SecureRandom { get; set; }
    public string? SecureDeterministic { get; set; }

    /// <summary>
    /// class level validation across multiple properties
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required.", [nameof(Name)]);
        }
    }
}
