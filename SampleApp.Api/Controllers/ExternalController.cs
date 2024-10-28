using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.AspNetCore;
using Package.Infrastructure.Common.Contracts;
using AppConstants = Application.Contracts.Constants.Constants;

namespace SampleApp.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ApiVersion("1.0")]
[ApiVersion("1.1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ExternalController(ISampleApiRestClient apiClient) : ControllerBase
{
    private readonly ISampleApiRestClient _apiClient = apiClient;

    /// <summary>
    /// Gets a paged list of TodoItems
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <returns></returns>
    [MapToApiVersion("1.0")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TodoItemDto>>> GetPage(int pageSize = 10, int pageIndex = 1)
    {
        var items = await _apiClient.GetPageAsync(pageSize, pageIndex);
        return Ok(items);
    }

    [MapToApiVersion("1.1")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TodoItemDto>>> GetPage_1_1(int pageSize = 20, int pageIndex = 1)
    {
        var items = await _apiClient.GetPageAsync(pageSize, pageIndex);
        return Ok(items);
    }

    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem(Guid id)
    {
        var todoItem = await _apiClient.GetItemAsync(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    #region External Api calls with different security config
    //[HttpGet("{id:Guid}")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_NoAttribute(Guid id)
    //{
    //    var todoItem = await _apiClient.GetTodoItem_NoAttribute(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Role_SomeAccess1/{id:Guid}")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Role_SomeAccess1(Guid id)
    //{
    //    var todoItem = await _apiClient.GetTodoItem_Role_SomeAccess1(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_AdminPolicy/{id:Guid}")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_AdminPolicy(Guid id)
    //{
    //    var todoItem = await _apiClient.GetTodoItem_Policy_AdminPolicy(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_SomeRolePolicy1/{id:Guid}")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_SomeRolePolicy1(Guid id)
    //{
    //    var todoItem = await _apiClient.GetTodoItem_Policy_SomeRolePolicy1(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_SomeScopePolicy1/{id:Guid}")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_SomeScopePolicy1(Guid id)
    //{
    //    var todoItem = await _apiClient.GetTodoItem_Policy_SomeScopePolicy1(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_ScopeOrRolePolicy1/{id:Guid}")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_ScopeOrRolePolicy1(Guid id)
    //{
    //    var todoItem = await _apiClient.GetTodoItem_Policy_ScopeOrRolePolicy1(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_ScopeOrRolePolicy2/{id:Guid}")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_ScopeOrRolePolicy2(Guid id)
    //{
    //    var todoItem = await _apiClient.GetTodoItem_Policy_ScopeOrRolePolicy2(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    #endregion

    [HttpPost]
    public async Task<ActionResult<TodoItemDto>> SaveTodoItem([FromServices] IHostEnvironment hostEnv, TodoItemDto todoItem)
    {
        var result = await _apiClient.SaveItemAsync(todoItem);
        return result.Match<ActionResult<TodoItemDto>>(
            dto => CreatedAtAction(nameof(TodoItemDto), new { id = dto!.Id }, dto),
            err => BadRequest(ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: HttpContext.TraceIdentifier, includeStackTrace: hostEnv.IsDevelopment()))
            );
    }

    [HttpPut("{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> PutTodoItem([FromServices] IHostEnvironment hostEnv, Guid id, TodoItemDto todoItem)
    {
        if (todoItem.Id != Guid.Empty && todoItem.Id != id)
        {
            return BadRequest($"{AppConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {todoItem.Id}");
        }

        var result = await _apiClient.SaveItemAsync(todoItem);
        return result.Match<ActionResult<TodoItemDto>>(
            dto => Ok(dto),
            err => BadRequest(ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: HttpContext.TraceIdentifier, includeStackTrace: hostEnv.IsDevelopment()))
            );
    }

    [HttpDelete("{id:Guid}")]
    public async Task<ActionResult> DeleteTodoItem(Guid id)
    {
        await _apiClient.DeleteItemAsync(id);
        return Ok();
    }

    [HttpGet("getuser")]
    public async Task<ActionResult> GetUser()
    {
        return Ok(await _apiClient.GetUserAsync());
    }

    [HttpGet("getuserclaims")]
    public async Task<ActionResult> GetUserClaims()
    {
        return Ok(await _apiClient.GetUserClaimsAsync());
    }

    [HttpGet("getauthheader")]
    public async Task<ActionResult> GetAuthHeader()
    {
        return Ok(await _apiClient.GetAuthHeaderAsync());
    }
}
