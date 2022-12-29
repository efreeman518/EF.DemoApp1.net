using Application.Services.Rules;
using Package.Infrastructure.Data.Contracts;
using Package.Infrastructure.Utility.Exceptions;
using Package.Infrastructure.Utility.Extensions;
using System.Collections.Generic;
using AppConstants = Application.Contracts.Constants.Constants;
using DomainConstants = Domain.Shared.Constants.Constants;

namespace Application.Services;

public class TodoService : ServiceBase, ITodoService
{
    private readonly TodoServiceSettings _settings;
    private readonly ITodoRepositoryTrxn _repoTrxn;
    private readonly ITodoRepositoryQuery _repoQuery;
    private readonly IMapper _mapper;

    public TodoService(ILogger<TodoService> logger, IOptions<TodoServiceSettings> settings, ITodoRepositoryTrxn repoTrxn, ITodoRepositoryQuery repoQuery, IMapper mapper)
        : base(logger)
    {
        _settings = settings.Value;
        _repoTrxn = repoTrxn;
        _repoQuery = repoQuery;
        _mapper = mapper;
    }

    public async Task<PagedResponse<TodoItemDto>> GetItemsAsync(int pageSize = 10, int pageIndex = 0)
    {
        //normal structured logging
        Logger.Log(LogLevel.Information, "GetItemsAsync Start - pageSize:{pageSize} pageIndex:{pageIndex}", pageSize, pageIndex);
        //performant logging
        Logger.InfoLog($"GetItemsAsync Start - pageSize:{pageSize} pageIndex:{pageIndex}");

        _ = _settings.IntValue;

        //return mapped domain -> app
        return await _repoQuery.GetPageTodoItemDtoAsync(pageSize, pageIndex);
    }

    public async Task<TodoItemDto> GetItemAsync(Guid id)
    {
        Logger.Log(LogLevel.Information, "GetItemAsync - id:{id}", id);

        var todo = await _repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id);
        if (todo == null) throw new NotFoundException($"TodoItem.Id '{id}' not found.");

        //return mapped domain -> app
        return _mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto> AddItemAsync(TodoItemDto dto)
    {
        Logger.Log(LogLevel.Information, "AddItemAsync Start - {TodoItemDto}", dto.SerializeToJson());

        #region dto validation - using service code

        if (dto.Name.Length < DomainConstants.RULE_NAME_LENGTH)
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE} {DomainConstants.RULE_NAME_LENGTH}");
        if (await _repoTrxn.ExistsAsync<TodoItem>(t => t.Name == dto.Name))
            throw new ValidationException($"{AppConstants.ERROR_ITEM_EXISTS}: '{dto.Name}'");

        #endregion

        #region dto validation - using rule classes

        if (!new TodoNameLengthRule(DomainConstants.RULE_NAME_LENGTH).IsSatisfiedBy(dto))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE} {DomainConstants.RULE_NAME_LENGTH}.");
        if (!new TodoNameRegexRule(DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(dto))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_INVALID_MESSAGE}; '{DomainConstants.RULE_NAME_REGEX}'.");
        if (!new TodoCompositeRule(DomainConstants.RULE_NAME_LENGTH, DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(dto))
            throw new ValidationException(AppConstants.ERROR_RULE_INVALID_MESSAGE);

        #endregion

        //map app -> domain
        var todo = _mapper.Map<TodoItemDto, TodoItem>(dto);

        var validationResult = todo.Validate();
        if (!validationResult.IsValid) throw new ValidationException(validationResult);

        _repoTrxn.Create(ref todo);
        await _repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        Logger.Log(LogLevel.Information, "AddItemAsync Complete - {TodoItem}", todo.SerializeToJson());

        //return mapped domain -> app
        return _mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto?> UpdateItemAsync(TodoItemDto dto)
    {
        Logger.Log(LogLevel.Information, "UpdateItemAsync Start - {TodoItemDto}", dto.SerializeToJson());

        //validate dto (using Rule classes)
        if (!new TodoNameLengthRule(DomainConstants.RULE_NAME_LENGTH).IsSatisfiedBy(dto))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE} {DomainConstants.RULE_NAME_LENGTH}.");
        if (!new TodoNameRegexRule(DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(dto))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_INVALID_MESSAGE}; '{DomainConstants.RULE_NAME_REGEX}'.");
        if (!new TodoCompositeRule(DomainConstants.RULE_NAME_LENGTH, DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(dto))
            throw new ValidationException(AppConstants.ERROR_RULE_INVALID_MESSAGE);

        //retrieve existing
        var dbTodo = await _repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == dto.Id);
        if (dbTodo == null) throw new NotFoundException($"{AppConstants.ERROR_ITEM_NOTFOUND}: {dto.Id}");

        var updateTodo = _mapper.Map<TodoItemDto, TodoItem>(dto);

        //update 
        dbTodo.Name = updateTodo.Name;
        dbTodo.IsComplete = updateTodo.IsComplete;
        dbTodo.Status = updateTodo.Status;

        _repoTrxn.UpdateFull(ref dbTodo); //update full record
        await _repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        Logger.Log(LogLevel.Information, "UpdateItemAsync Complete - {TodoItem}", dbTodo.SerializeToJson());

        //return mapped domain -> app 
        return _mapper.Map<TodoItemDto>(dbTodo);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        Logger.Log(LogLevel.Information, "DeleteItemAsync - {id}", id);

        _repoTrxn.Delete(new TodoItem { Id = id });
        await _repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);
    }

    public async Task<PagedResponse<TodoItemDto>> SearchAsync(SearchRequest<TodoItem> request)
    {
        //normal structured logging
        Logger.Log(LogLevel.Information, "SearchAsync Start - {request}", request.SerializeToJson());
        //performant logging
        Logger.InfoLog($"SearchAsync Start - request:{request.SerializeToJson()}");

        _ = _settings.IntValue;

        var response = await _repoQuery.SearchAsync(request);

        //return mapped domain -> app
        return new PagedResponse<TodoItemDto>()
        {
            PageIndex = response.PageIndex,
            PageSize = response.PageSize,
            Data = _mapper.Map<List<TodoItemDto>>(response.Data),
            Total = response.Total
        };
    }
}
