using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Package.Infrastructure.Domain;
using Package.Infrastructure.Domain.Attributes;
using System.Text.RegularExpressions;

namespace Domain.Model;

public partial class TodoItem(string name, TodoItemStatus status = TodoItemStatus.Created, string? secureRandom = null, string? secureDeterministic = null)
    : EntityBase
{
    public string Name { get; private set; } = name;
    public bool IsComplete => Status == TodoItemStatus.Completed;
    public TodoItemStatus Status { get; private set; } = status;

    [Mask("***")]
    public string? SecureRandom { get; set; } = secureRandom;
    [Mask("***")]
    public string? SecureDeterministic { get; set; } = secureDeterministic;

    //enable soft deletes
    public bool IsDeleted { get; set; }

    public void SetName(string name)
    {
        Name = name;
    }

    public void SetStatus(TodoItemStatus status)
    {
        Status = status;
    }

    public Result Validate()
    {
        var errors = new List<string>();
        if (Name?.Length < Constants.RULE_NAME_LENGTH_MIN) errors.Add("Name length violation");
        if (!KnownGeneratedRegexNameRule().Match(Name ?? "").Success) errors.Add("Name regex violation");
        if (errors.Count > 0)
        {
            var errorMessage = $"{GetType().Name} is not valid: {string.Join(", ", errors)}";
            return Result.Failure(errorMessage);
        }

        return Result.Success();
    }

    /// <summary>
    /// Compile time regex for optimization; requires partial class
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("a")]
    private static partial Regex KnownGeneratedRegexNameRule();
}
