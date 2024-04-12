using Application.Contracts.Interfaces;
using Application.Services.Logging;
using LanguageExt.Common;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;
using AppConstants = Application.Contracts.Constants.Constants;

namespace Application.Services;

public class TodoService(ILogger<TodoService> logger, IOptionsMonitor<TodoServiceSettings> settings, IValidationHelper validationHelper,
    ITodoRepositoryTrxn repoTrxn, ITodoRepositoryQuery repoQuery, ISampleApiRestClient sampleApiRestClient, IMapper mapper, IBackgroundTaskQueue taskQueue)
    : ServiceBase(logger), ITodoService
{
    //private readonly ILogger<TodoService> _logger = logger;

    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
    {
        //avoid compiler warning
        _ = settings.GetHashCode();

        //performant logging
        logger.InfoLog($"GetItemsAsync - pageSize:{pageSize} pageIndex:{pageIndex}");

        //return mapped domain -> app
        return await repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(mapper.ConfigurationProvider, true, pageSize, pageIndex,
            orderBy: t => t.OrderBy(x => x.Name),
            includeTotal: true, cancellationToken: cancellationToken);
    }

    public async Task<TodoItemDto?> GetItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        //performant logging
        logger.InfoLog($"GetItemAsync - {id}");

        var todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id, cancellationToken: cancellationToken);

        if (todo == null) return null;

        //return mapped domain -> app
        return mapper.Map<TodoItem, TodoItemDto>(todo);
    }

    public async Task<Result<TodoItemDto?>> AddItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default)
    {
        //structured logging
        logger.TodoItemCRUD("AddItemAsync Start", dto.SerializeToJson());

        //dto - FluentValidation
        var valResultDto = await validationHelper.ValidateAsync(dto, cancellationToken);
        if (!valResultDto.IsValid)
        {
            var error = new ValidationException(string.Join(",", valResultDto.Errors.Select(e => e.ErrorMessage)));
            return new Result<TodoItemDto?>(error);
        }

        //map app -> domain
        var todo = mapper.Map<TodoItemDto, TodoItem>(dto)!;

        //domain entity - entity validation method
        var valResultDomain = todo.Validate();
        if (!valResultDomain.IsValid)
        {
            var error = new ValidationException(string.Join(",", valResultDomain.Messages));
            return new Result<TodoItemDto?>(error);
        }

        repoTrxn.Create(ref todo);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken);

        //queue some non-scoped work - fire and forget (notification)
        taskQueue.QueueBackgroundWorkItem(async cancellationToken =>
        {
            //await some work
            await Task.Delay(200, cancellationToken);
            logger.InfoLog($"Some non-scoped work done");
        });

        //queue some scoped work - fire and forget (update DB)
        taskQueue.QueueScopedBackgroundWorkItem<ITodoRepositoryTrxn>(async (scopedRepositoryTrxn, cancellationToken) =>
        {
            //await some work
            await Task.Delay(200, cancellationToken);
            logger.InfoLog("Some scoped work done");
        }, cancellationToken: cancellationToken);

        logger.TodoItemCRUD("AddItemAsync Finish", todo.Id.ToString());

        //return mapped domain -> app
        return mapper.Map<TodoItem, TodoItemDto>(todo)!;
    }

    public async Task<Result<TodoItemDto?>> UpdateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default)
    {
        logger.TodoItemCRUD("UpdateItemAsync Start", dto.SerializeToJson());

        //FluentValidation
        var valResultDto = await validationHelper.ValidateAsync(dto, cancellationToken);
        if (!valResultDto.IsValid)
        {
            var error = new ValidationException(string.Join(",", valResultDto.Errors.Select(e => e.ErrorMessage)));
            return new Result<TodoItemDto?>(error);
        }

        //retrieve existing
        var dbTodo = await repoTrxn.GetEntityAsync<TodoItem>(true, filter: t => t.Id == dto.Id, cancellationToken: cancellationToken);
        if (dbTodo == null) return new Result<TodoItemDto?>(new NotFoundException($"{AppConstants.ERROR_ITEM_NOTFOUND}: {dto.Id}"));

        //update
        dbTodo.SetName(dto.Name);
        dbTodo.SetStatus(dto.Status);
        dbTodo.SecureDeterministic = dto.SecureDeterministic;
        dbTodo.SecureRandom = dto.SecureRandom;

        //_repoTrxn.UpdateFull(ref dbTodo); //update full record - only needed if not already tracked
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken);

        logger.TodoItemCRUD("UpdateItemAsync Complete", dbTodo.SerializeToJson());

        //return mapped domain -> app 
        return mapper.Map<TodoItemDto>(dbTodo);
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.TodoItemCRUD("DeleteItemAsync Start", id.ToString());

        await repoTrxn.DeleteAsync<TodoItem>(cancellationToken, id);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken);

        logger.TodoItemCRUD("DeleteItemAsync Complete", id.ToString());
    }

    public async Task<PagedResponse<TodoItemDto>> SearchAsync(SearchRequest<TodoItemSearchFilter> request, CancellationToken cancellationToken = default)
    {
        logger.InfoLogExt($"SearchAsync", request.SerializeToJson());
        return await repoQuery.SearchTodoItemAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<TodoItemDto>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
    {
        logger.InfoLog($"GetPageExternalAsync - pageSize:{pageSize} pageIndex:{pageIndex}");
        return await sampleApiRestClient.GetPageAsync(pageSize, pageIndex, cancellationToken);
    }
}
