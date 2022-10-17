using Domain.Rules;
using Package.Infrastructure.Utility.Exceptions;
using Package.Infrastructure.Utility.Extensions;
using System.Collections.Generic;
using System.Text.Json;
using AppConstants = Application.Contracts.Constants.Constants;
using DomainConstants = Domain.Shared.Constants.Constants;

namespace Application.Services;

public class TodoService : ServiceBase, ITodoService
{
    private readonly TodoServiceSettings _settings;
    private readonly ITodoRepository _repository;
    private readonly IMapper _mapper;

    public TodoService(ILogger<TodoService> logger, IOptions<TodoServiceSettings> settings, ITodoRepository repository, IMapper mapper)
        : base(logger)
    {
        _settings = settings.Value;
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<TodoItemDto>> GetItemsAsync(int pageSize = 10, int pageIndex = 0)
    {
        //normal structured logging
        Logger.Log(LogLevel.Information, "GetItemsAsync - pageSize:{pageSize} pageIndex:{pageIndex}", pageSize, pageIndex);
        //performant logging
        Logger.InfoLog($"GetItemsAsync Start - pageSize:{pageSize} pageIndex:{pageIndex}");

        _ = _settings.IntValue;

        //return mapped domain -> app
        return new PagedResponse<TodoItemDto>
        {
            PageSize = pageSize,
            PageIndex = pageIndex,
            Data = _mapper.Map<List<TodoItem>, List<TodoItemDto>>(await _repository.GetItemsAsync(pageSize, pageIndex)),
            Total = await _repository.GetItemsCountAsync()
        };
    }

    public async Task<TodoItemDto> GetItemAsync(Guid id)
    {
        Logger.Log(LogLevel.Information, "GetItemAsync - id:{id}", id);

        var todo = await _repository.GetItemAsync(t => t.Id == id);
        if (todo == null) throw new NotFoundException($"TodoItem.Id '{id}' not found.");

        //return mapped domain -> app
        return _mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto> AddItemAsync(TodoItemDto dto)
    {
        Logger.Log(LogLevel.Information, "AddItemAsync Start - {TodoItemDto}", JsonSerializer.Serialize(dto));

        //validate app model
        if (dto.Name.Length < DomainConstants.RULE_NAME_LENGTH)
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE} {DomainConstants.RULE_NAME_LENGTH}");
        if (await _repository.ExistsAsync(t => t.Name == dto.Name))
            throw new ValidationException($"{AppConstants.ERROR_ITEM_EXISTS}: '{dto.Name}'");

        //map app -> domain
        var todo = _mapper.Map<TodoItemDto, TodoItem>(dto);

        //validate domain model (using Rule classes)
        if (!new TodoNameLengthRule(DomainConstants.RULE_NAME_LENGTH).IsSatisfiedBy(todo))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE} {DomainConstants.RULE_NAME_LENGTH}.");
        if (!new TodoNameRegexRule(DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(todo))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_INVALID_MESSAGE}; '{DomainConstants.RULE_NAME_REGEX}'.");
        if (!new TodoCompositeRule(DomainConstants.RULE_NAME_LENGTH, DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(todo))
            throw new ValidationException(AppConstants.ERROR_RULE_INVALID_MESSAGE);

        todo = _repository.AddItem(todo);
        await _repository.SaveChangesAsync("userId1");

        Logger.Log(LogLevel.Information, "AddItemAsync Complete - {TodoItem}", JsonSerializer.Serialize(todo));

        //return mapped domain -> app
        return _mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto?> UpdateItemAsync(TodoItemDto dto)
    {
        Logger.Log(LogLevel.Information, "UpdateItemAsync Start - {TodoItemDto}", JsonSerializer.Serialize(dto));

        //retrieve existing
        var dbTodo = await _repository.GetItemAsync(t => t.Id == dto.Id);
        if (dbTodo == null) throw new NotFoundException($"{AppConstants.ERROR_ITEM_NOTFOUND}: {dto.Id}");

        var updateTodo = _mapper.Map<TodoItemDto, TodoItem>(dto);

        //update 
        dbTodo.Name = updateTodo.Name;
        dbTodo.IsComplete = updateTodo.IsComplete;
        dbTodo.Status = updateTodo.Status;

        //validate domain model (using Rule classes)
        if (!new TodoNameLengthRule(DomainConstants.RULE_NAME_LENGTH).IsSatisfiedBy(dbTodo))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_LENGTH_MESSAGE} {DomainConstants.RULE_NAME_LENGTH}.");
        if (!new TodoNameRegexRule(DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(dbTodo))
            throw new ValidationException($"{AppConstants.ERROR_RULE_NAME_INVALID_MESSAGE}; '{DomainConstants.RULE_NAME_REGEX}'.");
        if (!new TodoCompositeRule(DomainConstants.RULE_NAME_LENGTH, DomainConstants.RULE_NAME_REGEX).IsSatisfiedBy(dbTodo))
            throw new ValidationException(AppConstants.ERROR_RULE_INVALID_MESSAGE);

        _repository.UpdateItem(dbTodo); //update full record

        await _repository.SaveChangesAsync("userId1");

        Logger.Log(LogLevel.Information, "UpdateItemAsync Complete - {TodoItem}", JsonSerializer.Serialize(dbTodo));

        //return mapped domain -> app 
        return _mapper.Map<TodoItem, TodoItemDto>(dbTodo);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        Logger.Log(LogLevel.Information, "DeleteItemAsync - {id}", id);

        _repository.DeleteItem(id);
        await _repository.SaveChangesAsync("userId1");
    }
}
