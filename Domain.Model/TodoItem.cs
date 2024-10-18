using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Attributes;
using Package.Infrastructure.Data.Contracts;
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

    public ValidationResult Validate()
    {
        var result = new ValidationResult(true);
        if (Name?.Length < Constants.RULE_NAME_LENGTH_MIN) result.Messages.Add("Name length violation");
        if (!KnownGeneratedRegexNameRule().Match(Name ?? "").Success) result.Messages.Add("Name regex violation");
        result.IsValid = result.Messages.Count == 0;
        return result;
    }

    /// <summary>
    /// Compile time regex for optimization
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("a")]
    private static partial Regex KnownGeneratedRegexNameRule();
}
