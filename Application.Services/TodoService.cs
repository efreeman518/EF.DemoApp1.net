using Application.Contracts.Interfaces;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;
using AppConstants = Application.Contracts.Constants.Constants;

namespace Application.Services;

public class TodoService : ServiceBase, ITodoService
{
    private readonly TodoServiceSettings _settings;
    private readonly IValidationHelper _validationHelper;
    private readonly ITodoRepositoryTrxn _repoTrxn;
    private readonly ITodoRepositoryQuery _repoQuery;
    private readonly ISampleApiRestClient _sampleApiRestClient;
    private readonly IMapper _mapper;
    private readonly IBackgroundTaskQueue _taskQueue;

    public TodoService(ILogger<TodoService> logger, IOptionsMonitor<TodoServiceSettings> settings, IValidationHelper validationHelper,
        ITodoRepositoryTrxn repoTrxn, ITodoRepositoryQuery repoQuery, ISampleApiRestClient sampleApiRestClient, IMapper mapper, IBackgroundTaskQueue taskQueue)
        : base(logger)
    {
        _settings = settings.CurrentValue;
        _validationHelper = validationHelper;
        _repoTrxn = repoTrxn;
        _repoQuery = repoQuery;
        _sampleApiRestClient = sampleApiRestClient;
        _mapper = mapper;
        _taskQueue = taskQueue;
    }

    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0)
    {
        //avoid compiler warning
        _ = _settings.GetHashCode();

        //performant logging
        Logger.InfoLog($"GetItemsAsync - pageSize:{pageSize} pageIndex:{pageIndex}");

        //return mapped domain -> app
        return await _repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize, pageIndex, includeTotal: true);
    }

    public async Task<TodoItemDto> GetItemAsync(Guid id)
    {
        //performant logging
        Logger.InfoLog($"GetItemAsync - id:{id}");

        var todo = await _repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id)
            ?? throw new NotFoundException($"Id '{id}' not found.");

        //return mapped domain -> app
        return _mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto> AddItemAsync(TodoItemDto dto)
    {
        //structured logging
        Logger.Log(LogLevel.Information, "AddItemAsync Start - {TodoItemDto}", dto.SerializeToJson());

        //FluentValidation
        await _validationHelper.ValidateAndThrowAsync(dto);

        //map app -> domain
        var todo = _mapper.Map<TodoItemDto, TodoItem>(dto);

        var validationResult = todo.Validate();
        if (!validationResult.IsValid) throw new ValidationException(validationResult);

        _repoTrxn.Create(ref todo);
        await _repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        //queue some external work - fire and forget (notification)
        _taskQueue.QueueBackgroundWorkItem(async token =>
        {
            //await some work
            await Task.Delay(3000, token);
            Logger.LogInformation("Some work done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        Logger.Log(LogLevel.Information, "AddItemAsync Complete - {TodoItem}", todo.SerializeToJson());

        //return mapped domain -> app
        return _mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto?> UpdateItemAsync(TodoItemDto dto)
    {
        Logger.Log(LogLevel.Information, "UpdateItemAsync Start - {TodoItemDto}", dto.SerializeToJson());

        //FluentValidation
        await _validationHelper.ValidateAndThrowAsync(dto);

        //retrieve existing
        var dbTodo = await _repoTrxn.GetEntityAsync<TodoItem>(true, filter: t => t.Id == dto.Id)
            ?? throw new NotFoundException($"{AppConstants.ERROR_ITEM_NOTFOUND}: {dto.Id}");

        //update
        dbTodo.SetName(dto.Name);
        dbTodo.SetStatus(dto.Status);
        dbTodo.SecureDeterministic = dto.SecureDeterministic;
        dbTodo.SecureRandom = dto.SecureRandom;

        //_repoTrxn.UpdateFull(ref dbTodo); //update full record - only needed if not already tracked
        await _repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        Logger.Log(LogLevel.Information, "UpdateItemAsync Complete - {TodoItem}", dbTodo.SerializeToJson());

        //return mapped domain -> app 
        return _mapper.Map<TodoItemDto>(dbTodo);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        Logger.Log(LogLevel.Information, "DeleteItemAsync Start - {id}", id);

        await _repoTrxn.DeleteAsync<TodoItem>(id);
        await _repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        Logger.Log(LogLevel.Information, "DeleteItemAsync Complete - {id}", id);
    }

    public async Task<PagedResponse<TodoItemDto>> SearchAsync(SearchRequest<TodoItemSearchFilter> request)
    {
        Logger.Log(LogLevel.Information, "SearchAsync - {request}", request.SerializeToJson());

        return await _repoQuery.SearchTodoItemAsync(request);
    }

    public async Task<PagedResponse<TodoItemDto>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0)
    {
        //performant logging
        Logger.InfoLog($"GetPageExternalAsync - pageSize:{pageSize} pageIndex:{pageIndex}");

        return await _sampleApiRestClient.GetPageAsync(pageSize, pageIndex);
    }
}
