using Application.Contracts.Interfaces;
using FluentValidation;
using AppConstants = Application.Contracts.Constants.Constants;
using DomainConstants = Domain.Shared.Constants.Constants;

namespace Application.Services.Validators;

public class TodoItemDtoValidator : AbstractValidator<TodoItemDto>
{
    public TodoItemDtoValidator(ITodoRepositoryQuery repoQuery)
    {
        RuleFor(dto => dto.Name).NotEmpty()
            .Length(DomainConstants.RULE_NAME_LENGTH_MIN, DomainConstants.RULE_NAME_LENGTH_MAX)
                .WithMessage(string.Format(AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE, DomainConstants.RULE_NAME_LENGTH_MIN, DomainConstants.RULE_NAME_LENGTH_MAX))
            .Matches(DomainConstants.RULE_NAME_REGEX)
                .WithMessage(string.Format(AppConstants.ERROR_RULE_NAME_INVALID_MESSAGE, DomainConstants.RULE_NAME_REGEX));

        //Create - check for existing name 
        RuleFor(dto => dto).NotEmpty()
            .MustAsync(async (dto, cancellationToken) =>
            {
                return !(await repoQuery.ExistsAsync<TodoItem>(x => x.Name == dto.Name));
            })
            .When(dto => dto.Id == Guid.Empty)
            .WithMessage(dto => string.Format(AppConstants.ERROR_NAME_EXISTS, dto.Name));


        //Update - possible name change - check for existing name 
        RuleFor(dto => dto).NotEmpty()
            .MustAsync(async (dto, cancellationToken) =>
            {
                return !(await repoQuery.ExistsAsync<TodoItem>(x => x.Id != dto.Id && x.Name == dto.Name));
            })
            .When(dto => dto.Id != Guid.Empty)
            .WithMessage(dto => string.Format(AppConstants.ERROR_NAME_EXISTS, dto.Name));
    }
}
