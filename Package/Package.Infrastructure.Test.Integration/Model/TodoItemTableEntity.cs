using Azure; //ETag
using Domain.Shared.Constants;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Exceptions;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Test.Integration.Model;

public partial class TodoItemTableEntity : Infrastructure.Table.ITableEntity
{
    //ITableEntity
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    //public get & set required pull out of Table storage properly 
    public string Name { get; set; }
    public TodoItemStatus Status { get; set; }
    public bool IsComplete => Status == TodoItemStatus.Completed;

    //Parameterless constructor required for Table retrieval
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TodoItemTableEntity() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable S3427 // Method overloads with default parameter values should not overlap 
    public TodoItemTableEntity(string? partitionKey = null, string? rowKey = null, string? name = null, TodoItemStatus status = TodoItemStatus.Created)
#pragma warning restore S3427 // Method overloads with default parameter values should not overlap 
    {
        RowKey = rowKey ?? Guid.NewGuid().ToString();
        PartitionKey = partitionKey ?? RowKey.ToString()[..5];
        Name = name ?? $"{RowKey}-a";
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
