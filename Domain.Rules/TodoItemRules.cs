using Domain.Model;
using Domain.Shared.Constants;
using Package.Infrastructure.Common;
using System.Text.RegularExpressions;

namespace Domain.Rules;

public class TodoNameLengthRule(int nameLengthRequirement = Constants.RULE_NAME_LENGTH_MIN) : 
    Specification<TodoItem>(item => item?.Name?.Length >= nameLengthRequirement)
{
}

public class TodoNameRegexRule(string reMatch = ".*") : Specification<TodoItem>(item =>
    {
        Regex regex = new(reMatch);
        Match match = regex.Match(item.Name);
        return match.Success;
    })
{
}

public class TodoCompositeRule(int nameLengthRequirement, string reMatch) : Specification<TodoItem>(ce =>
        new TodoNameLengthRule(nameLengthRequirement)
            .And(new TodoNameRegexRule(reMatch))
            .IsSatisfiedBy(ce)
    )
{
}
