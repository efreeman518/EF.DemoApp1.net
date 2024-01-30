using Application.Contracts.Interfaces;
using Application.Services.Logging;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;
using AppConstants = Application.Contracts.Constants.Constants;

namespace Application.Services;

public class TodoService(ILogger<TodoService> logger, IOptionsMonitor<TodoServiceSettings> settings, IValidationHelper validationHelper,
    ITodoRepositoryTrxn repoTrxn, ITodoRepositoryQuery repoQuery, ISampleApiRestClient sampleApiRestClient, IMapper mapper, IBackgroundTaskQueue taskQueue)
    : ServiceBase(logger), ITodoService
{
    private readonly ILogger<TodoService> _logger = logger;

    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0)
    {
        //avoid compiler warning
        _ = settings.GetHashCode();

        //performant logging
        _logger.InfoLog($"GetItemsAsync - pageSize:{pageSize} pageIndex:{pageIndex}");

        //return mapped domain -> app
        return await repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(mapper.ConfigurationProvider, pageSize, pageIndex, includeTotal: true);
    }

    public async Task<TodoItemDto> GetItemAsync(Guid id)
    {
        //performant logging
        _logger.InfoLog($"GetItemAsync - {id}");

        var todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id)
            ?? throw new NotFoundException($"Id '{id}' not found.");

        //return mapped domain -> app
        return mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto> AddItemAsync(TodoItemDto dto)
    {
        //structured logging
        _logger.TodoItemCRUD("AddItemAsync Start", dto.SerializeToJson());

        //dto - FluentValidation
        await validationHelper.ValidateAndThrowAsync(dto);

        //map app -> domain
        var todo = mapper.Map<TodoItemDto, TodoItem>(dto);

        //domain entity - entity validation method
        var validationResult = todo.Validate();
        if (!validationResult.IsValid) throw new ValidationException(validationResult);

        repoTrxn.Create(ref todo);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        //queue some non-scoped work - fire and forget (notification)
        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            //await some work
            await Task.Delay(3000, token);
            _logger.InfoLog($"Some work done");
        });

        //queue some scoped work - fire and forget (update DB)
        taskQueue.QueueScopedBackgroundWorkItem<ITodoRepositoryTrxn>(async (scopedRepositoryTrxn, token) =>
        {
            //await some work
            await Task.Delay(3000, token);
            _logger.InfoLog("Some scoped work done");
        });

        _logger.TodoItemCRUD("AddItemAsync Finish", todo.Id.ToString());

        //return mapped domain -> app
        return mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<TodoItemDto?> UpdateItemAsync(TodoItemDto dto)
    {
        _logger.TodoItemCRUD("UpdateItemAsync Start", dto.SerializeToJson());

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

        _logger.TodoItemCRUD("UpdateItemAsync Complete", dbTodo.SerializeToJson());

        //return mapped domain -> app 
        return mapper.Map<TodoItemDto>(dbTodo);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        _logger.TodoItemCRUD("DeleteItemAsync Start", id.ToString());

        await repoTrxn.DeleteAsync<TodoItem>(CancellationToken.None, id);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins);

        _logger.TodoItemCRUD("DeleteItemAsync Complete", id.ToString());
    }

    public async Task<PagedResponse<TodoItemDto>> SearchAsync(SearchRequest<TodoItemSearchFilter> request)
    {
        _logger.InfoLogExt($"SearchAsync", request.SerializeToJson());
        return await repoQuery.SearchTodoItemAsync(request);
    }

    public async Task<PagedResponse<TodoItemDto>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0)
    {
        _logger.InfoLog($"GetPageExternalAsync - pageSize:{pageSize} pageIndex:{pageIndex}");
        return await sampleApiRestClient.GetPageAsync(pageSize, pageIndex);
    }
}
