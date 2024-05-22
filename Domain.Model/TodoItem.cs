using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Package.Infrastructure.Common.Attributes;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Data.Contracts;
using System.Text.RegularExpressions;

namespace Domain.Model;

public partial class TodoItem : AuditableBase<string>
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
    }

    public void SetName(string name)
    {
        Name = name;
    }

    public void SetStatus(TodoItemStatus status)
    {
        Status = status;
    }

    /// <summary>
    /// Validate the entity
    /// </summary>
    /// <param name="throwOnInvalid"></param>
    /// <returns>Returns null for valid; List of errors otherwise</returns>
    public List<string>? Validate(bool throwOnInvalid = false)
    {
        var errors = new List<string>();
        if (Name == null || Name?.Length < Constants.RULE_NAME_LENGTH_MIN) errors.Add("Name length violation");
        if (!KnownGeneratedRegexNameRule().Match(Name ?? "").Success) errors.Add("Name regex violation");
        if (errors.Count > 0)
        {
            var ex = new ValidationException(errors);
            if (throwOnInvalid) throw ex;
            return errors;
        }
        return null;
    }

    /// <summary>
    /// Compile time regex for optimization
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("a")]
    private static partial Regex KnownGeneratedRegexNameRule();
}
