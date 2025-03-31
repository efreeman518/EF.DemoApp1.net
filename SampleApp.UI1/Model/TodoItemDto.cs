using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SampleApp.UI1.Model;


//expand into record structure for better readability and data annotation attributes
public record TodoItemDto
{
    public Guid? Id { get; init; }

    [Required]
    [StringLength(50, ErrorMessageResourceName = "TodoNameLengthRule", MinimumLength = 5)]
    public string Name { get; set; } = null!;

    [JsonConverter(typeof(JsonNumberEnumConverter<TodoItemStatus>))]
    public TodoItemStatus Status { get; set; } = TodoItemStatus.Created;

    public string? SecureRandom { get; set; }
    public string? SecureDeterministic { get; set; }
}
