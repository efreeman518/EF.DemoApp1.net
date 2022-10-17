using Domain.Model;
using Domain.Shared.Constants;
using Package.Infrastructure.Utility;
using System.Text.RegularExpressions;

namespace Domain.Rules;

public class TodoNameLengthRule : Specification<TodoItem>
{
    public TodoNameLengthRule(int nameLengthRequirement = Constants.RULE_NAME_LENGTH)
        : base(item => item?.Name?.Length >= nameLengthRequirement) { }
}

public class TodoNameRegexRule : Specification<TodoItem>
{
    public TodoNameRegexRule(string reMatch = ".*") : base(item =>
    {
        Regex regex = new(reMatch);
        Match match = regex.Match(item.Name);
        return match.Success;
    })
    { }
}

public class TodoCompositeRule : Specification<TodoItem>
{
    public TodoCompositeRule(int nameLengthRequirement, string reMatch) : base(ce =>
        new TodoNameLengthRule(nameLengthRequirement)
            .And(new TodoNameRegexRule(reMatch))
            .IsSatisfiedBy(ce)
    )
    { }
}
