﻿using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Common.Extensions;
using SampleAppGrpc = SampleApp.Grpc.Proto;
using SampleAppModel = Application.Contracts.Model;

namespace SampleApp.Api.Grpc;

//client cert auth only for this service class 
//[Authorize(AuthenticationSchemes = CertificateAuthenticationDefaults.AuthenticationScheme)]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TodoGrpcService : SampleAppGrpc.TodoService.TodoServiceBase
{
    private readonly ILogger<TodoGrpcService> _logger;
    private readonly Application.Contracts.Services.ITodoService _todoService;
    private readonly IMapper _mapper;

    public TodoGrpcService(ILogger<TodoGrpcService> logger, Application.Contracts.Services.ITodoService todoService, IMapper mapper)
    {
        _logger = logger;
        _todoService = todoService;
        _mapper = mapper;
    }

    public override async Task<SampleAppGrpc.ServiceResponsePageTodoItems> Page(SampleAppGrpc.ServiceRequestPage request, ServerCallContext context)
    {
        var page = await _todoService.GetPageAsync(request.Pagesize, request.Pageindex);

        var response = new SampleAppGrpc.ServiceResponsePageTodoItems
        {
            ResponseCode = SampleAppGrpc.ResponseCode.Success,
            Data = new SampleAppGrpc.PagedResponseTodo
            {
                Pagesize = page.PageSize,
                Pageindex = page.PageIndex,
                Total = page.Total
            }
        };
        //grps repeated is 'readonly' so the collection can't be assigned, only added to
        foreach (var todo in page.Data)
        {
            response.Data.Data.Add(_mapper.Map<SampleAppModel.TodoItemDto, SampleAppGrpc.TodoItemDto>(todo));
        }
        return response;
    }

    public override async Task<SampleAppGrpc.ServiceResponseTodoItem> Get(SampleAppGrpc.ServiceRequestId request, ServerCallContext context)
    {
        SampleAppModel.TodoItemDto? todo = await _todoService.GetItemAsync(new Guid(request.Id));
        _ = todo ?? throw new NotFoundException($"TodoItem.Id:{request.Id} not found.");

        return new SampleAppGrpc.ServiceResponseTodoItem
        {
            ResponseCode = SampleAppGrpc.ResponseCode.Success,
            Data = _mapper.Map<SampleAppModel.TodoItemDto, SampleAppGrpc.TodoItemDto>(todo)
        };
    }

    public override async Task<SampleAppGrpc.ServiceResponseTodoItem> Save(SampleAppGrpc.ServiceRequestTodoItem request, ServerCallContext context)
    {
        _logger.Log(LogLevel.Information, "Save {TodoItemDto} - Start", request.Data.SerializeToJson());

        SampleAppModel.TodoItemDto? todo = _mapper.Map<SampleAppGrpc.TodoItemDto, SampleAppModel.TodoItemDto>(request.Data);

        //Save = update/insert
        if (todo.Id == Guid.Empty)
            todo = await _todoService.AddItemAsync(todo);
        else
            todo = await _todoService.UpdateItemAsync(todo);

        var response = new SampleAppGrpc.ServiceResponseTodoItem
        {
            ResponseCode = SampleAppGrpc.ResponseCode.Success,
            Data = _mapper.Map<SampleAppModel.TodoItemDto?, SampleAppGrpc.TodoItemDto>(todo)
        };

        _logger.Log(LogLevel.Information, "Save {TodoItemDto} - Finish", response.Data.SerializeToJson());
        return response;
    }

    public override async Task<Empty> Delete(SampleAppGrpc.ServiceRequestId request, ServerCallContext context)
    {
        await _todoService.DeleteItemAsync(new Guid(request.Id));
        return new Empty();
    }
}