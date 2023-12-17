using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Attributes;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Data.Contracts;
using System.Text.RegularExpressions;

namespace Domain.Model;

public partial class TodoItem : EntityBase
{
    public string Name { get; private set; }
    public bool IsComplete => Status == TodoItemStatus.Completed;
    public TodoItemStatus Status { get; private set; }

    [Mask("***")]
    public string? SecureRandom { get; set; }
    [Mask("***")]
    public string? SecureDeterministic { get; set; }

    //enable soft deletes
    public bool IsDeleted { get; set; }

    public TodoItem(string name, TodoItemStatus status = TodoItemStatus.Created, string? secureRandom = null, string? secureDeterministic = null)
    {
        Name = name;
        Status = status;
        SecureRandom = secureRandom;
        SecureDeterministic = secureDeterministic;
        Validate(true);
    }

    public void SetName(string name)
    {
        Name = name;
        Validate(true);
    }

    public void SetStatus(TodoItemStatus status)
    {
        Status = status;
    }

    public ValidationResult Validate(bool throwOnInvalid = false)
    {
        var errors = new List<string>();
        if (Name == null || Name?.Length < Constants.RULE_NAME_LENGTH_MIN) errors.Add("Name length violation");
        if (!KnownGeneratedRegexNameRule().Match(Name ?? "").Success) errors.Add("Name regex violation");
        var result = new ValidationResult(errors.Count == 0, errors);
        if (errors.Count > 0 && throwOnInvalid) throw new ValidationException(result);
        return result;
    }

    /// <summary>
    /// Compile time regex for optimization
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("a")]
    private static partial Regex KnownGeneratedRegexNameRule();
}
