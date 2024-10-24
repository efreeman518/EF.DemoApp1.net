using Application.Contracts.Interfaces;
using Application.Contracts.Mappers;
using Application.Services.Logging;
using EntityFramework.Exceptions.Common;
using LanguageExt;
using LanguageExt.Common;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;
using AppConstants = Application.Contracts.Constants.Constants;

namespace Application.Services;

public class TodoService(ILogger<TodoService> logger, IOptionsMonitor<TodoServiceSettings> settings,
    ITodoRepositoryTrxn repoTrxn, ITodoRepositoryQuery repoQuery, ISampleApiRestClient sampleApiRestClient, IBackgroundTaskQueue taskQueue)
    : ServiceBase(logger), ITodoService
{
    public async Task<PagedResponse<TodoItemDto>> GetPageAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
    {
        //avoid compiler warning
        _ = settings.GetHashCode();

        //performant logging
        logger.InfoLog($"GetItemsAsync - pageSize:{pageSize} pageIndex:{pageIndex}");

        //return mapped domain -> app
        return await repoQuery.QueryPageProjectionAsync(TodoItemMapper.Projector, true, pageSize, pageIndex,
            orderBy: t => t.OrderBy(x => x.Name),
            includeTotal: true, cancellationToken: cancellationToken);
    }

    public async Task<Option<TodoItemDto>> GetItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        //performant logging
        logger.TodoItemGetById(id);

        var todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id, cancellationToken: cancellationToken);

        return (todo == null)
            ? Option<TodoItemDto>.None
            : todo.ToDto();
    }

    public async Task<Result<TodoItemDto>> CreateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default)
    {
        //map app -> domain
        var todo = dto.ToEntity();

        //domain entity validation 
        var validationResult = todo.Validate();
        if (!validationResult)
        {
            return new Result<TodoItemDto>(new ValidationException(validationResult.Messages));
        }

        //structured logging
        logger.TodoItemCRUD("AddItemAsync Start", todo.SerializeToJson());

        //create; catch unique constraint exception
        repoTrxn.Create(ref todo);
        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken);
        }
        catch (UniqueConstraintException ex)
        {
            return new Result<TodoItemDto>(ex); //name exists
        }

        //queue some non-scoped work - fire and forget (notification)
        taskQueue.QueueBackgroundWorkItem(async cancellationToken =>
        {
            //await some work
            //await Task.Delay(200, cancellationToken);
            await Task.CompletedTask;
            logger.InfoLog($"Some non-scoped work done");
        });

        //queue some scoped work - fire and forget (update DB)
        taskQueue.QueueScopedBackgroundWorkItem<ITodoRepositoryTrxn>(async (scopedRepositoryTrxn, cancellationToken) =>
        {
            //await some work
            //await Task.Delay(200, cancellationToken);
            await Task.CompletedTask;
            logger.InfoLog("Some scoped work done");
        }, cancellationToken: cancellationToken);

        logger.TodoItemCRUD("AddItemAsync Finish", todo.Id.ToString());

        //return mapped domain -> app
        return todo.ToDto();
    }

    public async Task<Result<TodoItemDto>> UpdateItemAsync(TodoItemDto dto, CancellationToken cancellationToken = default)
    {
        //retrieve existing
        var dbTodo = await repoTrxn.GetEntityAsync<TodoItem>(true, filter: t => t.Id == dto.Id, cancellationToken: cancellationToken);
        if (dbTodo == null) return new Result<TodoItemDto>(new NotFoundException($"{AppConstants.ERROR_ITEM_NOTFOUND}: {dto.Id}"));

        //update
        dbTodo.SetName(dto.Name);
        dbTodo.SetStatus(dto.Status);
        dbTodo.SecureDeterministic = dto.SecureDeterministic;
        dbTodo.SecureRandom = dto.SecureRandom;

        //domain entity validation 
        var validationResult = dbTodo.Validate();
        if (!validationResult)
        {
            return new Result<TodoItemDto>(new ValidationException(validationResult.Messages));
        }

        logger.TodoItemCRUD("UpdateItemAsync Start", dbTodo.SerializeToJson());

        //_repoTrxn.UpdateFull(ref dbTodo); //update full record - only needed if not already tracked
        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, cancellationToken);
        }
        catch (UniqueConstraintException ex)
        {
            return new Result<TodoItemDto>(ex); //name exists on another record
        }

        logger.TodoItemCRUD("UpdateItemAsync Complete", dbTodo.Id.ToString());

        //return mapped domain -> app 
        return dbTodo.ToDto();
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

    //external API call
    public async Task<Result<PagedResponse<TodoItemDto>?>> GetPageExternalAsync(int pageSize = 10, int pageIndex = 0, CancellationToken cancellationToken = default)
    {
        logger.InfoLog($"GetPageExternalAsync - pageSize:{pageSize} pageIndex:{pageIndex}");
        return await sampleApiRestClient.GetPageAsync(pageSize, pageIndex, cancellationToken);
    }
}
