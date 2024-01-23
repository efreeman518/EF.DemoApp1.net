using Domain.Shared.Constants;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.CosmosDb;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Test.Integration.Model;

public partial class TodoItemNoSql : CosmosDbEntity
{
    //determine partition key for entity
    public override string PartitionKey { get => Id.ToString()[..5]; }

    public string Name { get; private set; }
    public bool IsComplete => Status == TodoItemStatus.Completed;
    public TodoItemStatus Status { get; private set; }

    public TodoItemNoSql(string name, TodoItemStatus status = TodoItemStatus.Created)
    {
        Name = name;
        Status = status;
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
