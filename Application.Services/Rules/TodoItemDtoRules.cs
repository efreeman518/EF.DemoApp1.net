using Domain.Shared.Constants;
using Package.Infrastructure.Utility;
using System.Text.RegularExpressions;

namespace Application.Services.Rules;

public class TodoNameLengthRule : Specification<TodoItemDto>
{
    public TodoNameLengthRule(int nameLengthRequirement = Constants.RULE_NAME_LENGTH)
        : base(item => item?.Name?.Length >= nameLengthRequirement) { }
}

public class TodoNameRegexRule : Specification<TodoItemDto>
{
    public TodoNameRegexRule(string reMatch = ".*") : base(item =>
    {
        Regex regex = new(reMatch);
        Match match = regex.Match(item.Name);
        return match.Success;
    })
    { }
}

public class TodoCompositeRule : Specification<TodoItemDto>
{
    public TodoCompositeRule(int nameLengthRequirement, string reMatch) : base(ce =>
        new TodoNameLengthRule(nameLengthRequirement)
            .And(new TodoNameRegexRule(reMatch))
            .IsSatisfiedBy(ce)
    )
    { }
}
