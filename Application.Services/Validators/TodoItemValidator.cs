using FluentValidation;
using AppConstants = Application.Contracts.Constants.Constants;
using DomainConstants = Domain.Shared.Constants.Constants;

namespace Application.Services.Validators;

public class TodoItemValidator : AbstractValidator<TodoItemDto>
{
    readonly ITodoRepositoryQuery _repoQuery;

    public TodoItemValidator(ITodoRepositoryQuery repoQuery)
    {
        
        _repoQuery = repoQuery;

        RuleFor(t => t.Name).NotEmpty()
            .Length(DomainConstants.RULE_NAME_LENGTH_MIN, DomainConstants.RULE_NAME_LENGTH_MAX).WithMessage(string.Format(AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE, DomainConstants.RULE_NAME_LENGTH_MIN, DomainConstants.RULE_NAME_LENGTH_MAX))
            .Matches(DomainConstants.RULE_NAME_REGEX).WithMessage($"{AppConstants.ERROR_RULE_NAME_INVALID_MESSAGE}; '{DomainConstants.RULE_NAME_REGEX}'.")
            ;

        //Create - check for existing
        RuleFor(t => t.Name).NotEmpty()
            .When(t => t.Id != Guid.Empty)
            .MustAsync(async (name, cancellationToken) =>
            {
                return !(await _repoQuery.ExistsAsync<TodoItem>(x => x.Name == name));
            })
            .WithMessage(AppConstants.ERROR_ITEM_EXISTS);

    }
}
