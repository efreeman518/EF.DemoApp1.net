using Application.Contracts.Interfaces;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;
using AppConstants = Application.Contracts.Constants.Constants;

namespace Application.Services;

public class TodoService(ILogger<TodoService> logger, IOptionsMonitor<TodoServiceSettings> settings, IValidationHelper validationHelper,
    ITodoRepositoryTrxn repoTrxn, ITodoRepositoryQuery repoQuery, ISampleApiRestClient sampleApiRestClient, IMapper mapper, IBackgroundTaskQueue taskQueue) : ServiceBase(logger), ITodoService
{
    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0)
    {
        //avoid compiler warning
        _ = settings.GetHashCode();

        //performant logging
        Logger.InfoLog($"GetItemsAsync - pageSize:{pageSize} pageIndex:{pageIndex}");

        //return mapped domain -> app
        return await repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(mapper.ConfigurationProvider, pageSize, pageIndex, includeTotal: true);
    }

    public async Task<TodoItemDto> GetItemAsync(Guid id)
    {
        //performant logging
        Logger.InfoLog($"GetItemAsync - id:{id}");

        var todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id)
            ?? throw new NotFoundException($"Id '{id}' not found.");

        //return mapped domain -> app
        return mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto> AddItemAsync(TodoItemDto dto)
    {
        //structured logging
        Logger.Log(LogLevel.Information, "AddItemAsync Start - {TodoItemDto}", dto.SerializeToJson());

        //FluentValidation
        await validationHelper.ValidateAndThrowAsync(dto);

        //map app -> domain
        var todo = mapper.Map<TodoItemDto, TodoItem>(dto);

        var validationResult = todo.Validate();
        if (!validationResult.IsValid) throw new ValidationException(validationResult);

        repoTrxn.Create(ref todo);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        //queue some non-scoped work - fire and forget (notification)
        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            //await some work
            await Task.Delay(3000, token);
            Logger.LogInformation("Some work done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        //queue some scoped work - fire and forget (update DB)
        taskQueue.QueueScopedBackgroundWorkItem<ITodoRepositoryTrxn>(async (scopedRepositoryTrxn, token) =>
        {
            //await some work
            await Task.Delay(3000, token);
            await scopedRepositoryTrxn.QueryPageAsync<TodoItem>(pageSize: 10, pageIndex: 0, includeTotal: true);
            Logger.LogInformation("Some scoped work done at {Time}", DateTime.UtcNow.TimeOfDay);
        });

        Logger.Log(LogLevel.Information, "AddItemAsync Complete - {TodoItem}", todo.SerializeToJson());

        //return mapped domain -> app
        return mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto?> UpdateItemAsync(TodoItemDto dto)
    {
        Logger.Log(LogLevel.Information, "UpdateItemAsync Start - {TodoItemDto}", dto.SerializeToJson());

        //FluentValidation
        await validationHelper.ValidateAndThrowAsync(dto);

        //retrieve existing
        var dbTodo = await repoTrxn.GetEntityAsync<TodoItem>(true, filter: t => t.Id == dto.Id)
            ?? throw new NotFoundException($"{AppConstants.ERROR_ITEM_NOTFOUND}: {dto.Id}");

        //update
        dbTodo.SetName(dto.Name);
        dbTodo.SetStatus(dto.Status);
        dbTodo.SecureDeterministic = dto.SecureDeterministic;
        dbTodo.SecureRandom = dto.SecureRandom;

        //_repoTrxn.UpdateFull(ref dbTodo); //update full record - only needed if not already tracked
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        Logger.Log(LogLevel.Information, "UpdateItemAsync Complete - {TodoItem}", dbTodo.SerializeToJson());

        //return mapped domain -> app 
        return mapper.Map<TodoItemDto>(dbTodo);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        Logger.Log(LogLevel.Information, "DeleteItemAsync Start - {id}", id);

        await repoTrxn.DeleteAsync<TodoItem>(id);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        Logger.Log(LogLevel.Information, "DeleteItemAsync Complete - {id}", id);
    }

    public async Task<PagedResponse<TodoItemDto>> SearchAsync(SearchRequest<TodoItemSearchFilter> request)
    {
        Logger.Log(LogLevel.Information, "SearchAsync - {request}", request.SerializeToJson());

        return await repoQuery.SearchTodoItemAsync(request);
    }

    public async Task<PagedResponse<TodoItemDto>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0)
    {
        //performant logging
        Logger.InfoLog($"GetPageExternalAsync - pageSize:{pageSize} pageIndex:{pageIndex}");

        return await sampleApiRestClient.GetPageAsync(pageSize, pageIndex);
    }
}
