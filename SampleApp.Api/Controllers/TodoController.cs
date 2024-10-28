using Application.Contracts.Model;
using Application.Contracts.Services;
using Asp.Versioning;
using LazyCache;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Package.Infrastructure.AspNetCore;
using Package.Infrastructure.Auth.Tokens;
using Package.Infrastructure.Common.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppConstants = Application.Contracts.Constants.Constants;

namespace SampleApp.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ApiVersion("1.0")]
[ApiVersion("1.1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TodoItemsController(ITodoService todoService) : ControllerBase()
{
    private readonly ITodoService _todoService = todoService;

    /// <summary>
    /// Gets a paged list of TodoItems
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <returns>A paged list of TodoItems</returns>
    [MapToApiVersion("1.0")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TodoItemDto>>> GetPage(int pageSize = 10, int pageIndex = 1)
    {
        var items = await _todoService.GetPageAsync(pageSize, pageIndex);
        return Ok(items);
    }

    /// <summary>
    /// Gets a paged list of TodoItems
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <returns>A paged list of TodoItems</returns>
    [MapToApiVersion("1.1")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TodoItemDto>>> GetPage_1_1(int pageSize = 20, int pageIndex = 1)
    {
        var items = await _todoService.GetPageAsync(pageSize, pageIndex);
        return Ok(items);
    }

    /// <summary>
    /// Gets a single TodoItem by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>A TodoItem</returns>
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem(Guid id)
    {
        var option = await _todoService.GetItemAsync(id);
        return option.Match<ActionResult<TodoItemDto>>(
            Some: dto => Ok(dto),
            None: () => NotFound(id));
    }

    #region auth policy endpoints

    //[HttpGet("GetTodoItem_Role_SomeAccess1/{id:Guid}")]
    //[Authorize(Roles = "SomeAccess1")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Role_SomeAccess1(Guid id)
    //{
    //    var todoItem = await _todoService.GetItemAsync(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_AdminPolicy/{id:Guid}")]
    //[Authorize(Policy = "AdminPolicy")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_AdminPolicy(Guid id)
    //{
    //    var todoItem = await _todoService.GetItemAsync(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_SomeRolePolicy1/{id:Guid}")]
    //[Authorize(Policy = "SomeRolePolicy1")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_SomeRolePolicy1(Guid id)
    //{
    //    var todoItem = await _todoService.GetItemAsync(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_SomeScopePolicy1/{id:Guid}")]
    //[Authorize(Policy = "SomeScopePolicy1")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_SomeScopePolicy1(Guid id)
    //{
    //    var todoItem = await _todoService.GetItemAsync(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_ScopeOrRolePolicy1/{id:Guid}")]
    //[Authorize(Policy = "ScopeOrRolePolicy1")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_ScopeOrRolePolicy1(Guid id)
    //{
    //    var todoItem = await _todoService.GetItemAsync(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    //[HttpGet("GetTodoItem_Policy_ScopeOrRolePolicy2/{id:Guid}")]
    //[Authorize(Policy = "ScopeOrRolePolicy2")]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_ScopeOrRolePolicy2(Guid id)
    //{
    //    var todoItem = await _todoService.GetItemAsync(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}

    #endregion

    /// <summary>
    /// Saves a new TodoItem
    /// </summary>
    /// <param name="hostEnv"></param>
    /// <param name="todoItem"></param>
    /// <returns>The new TodoItem that was saved</returns>
    [HttpPost]
    public async Task<ActionResult<TodoItemDto>> PostTodoItem([FromServices] IHostEnvironment hostEnv, TodoItemDto todoItem)
    {
        var result = await _todoService.CreateItemAsync(todoItem);
        return result.Match<ActionResult<TodoItemDto>>(
            dto => CreatedAtAction(nameof(PostTodoItem), new { id = dto!.Id }, dto),
            err => BadRequest(ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: HttpContext.TraceIdentifier, includeStackTrace: hostEnv.IsDevelopment()))
            );
    }

    /// <summary>
    /// Updates an existing TodoItem
    /// </summary>
    /// <param name="id"></param>
    /// <param name="todoItem"></param>
    /// <returns>The updated TodoItem</returns>
    [HttpPut("{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> PutTodoItem(Guid id, TodoItemDto todoItem)
    {
        if (todoItem.Id != Guid.Empty && todoItem.Id != id)
        {
            return BadRequest($"{AppConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {todoItem.Id}");
        }

        var result = await _todoService.UpdateItemAsync(todoItem);
        return result.Match<ActionResult<TodoItemDto>>(
            dto => dto is null ? NotFound(id) : Ok(dto),
            err => BadRequest(err.Message));
    }

    /// <summary>
    /// Deletes a specific TodoItem by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>OK</returns>
    [HttpDelete("{id:Guid}")]
    public async Task<ActionResult> DeleteTodoItem(Guid id)
    {
        await _todoService.DeleteItemAsync(id);
        return Ok();
    }

    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

    /// <summary>
    /// Gets the current user
    /// </summary>
    /// <returns>User data in json</returns>
    [HttpGet("getuser")]
    public IActionResult GetUser()
    {
        var user = HttpContext.User;
        return new JsonResult(user.Identity, jsonSerializerOptions);
    }

    /// <summary>
    /// Gets the current user's claims
    /// </summary>
    /// <returns>Claims data in json</returns>
    [HttpGet("getuserclaims")]
    public IActionResult GetUserClaims()
    {
        var user = HttpContext.User;
        return new JsonResult(user.Claims, jsonSerializerOptions);
    }

    /// <summary>
    /// Gets the current user's auth header in json
    /// </summary>
    /// <returns></returns>
    [HttpGet("getauthheader")]
    [ProducesResponseType(typeof(StringValues), (int)HttpStatusCode.OK)]
    public IActionResult GetAuthHeader()
    {
        var authHeaders = HttpContext.Request.Headers.Authorization;
        return Ok(authHeaders);
    }

    /// <summary>
    /// Retrieve an access token for the given resource Id (Entra app reg client Id used to protect the target api).
    /// DefaultAzureCredential is used to get the credentials (Azure managed identity, env vars, VS loggged in user, etc.) to request the access token
    /// </summary>
    /// <param name="resourceId">Entra app reg client id that is protecting the target api</param>
    /// <param name="scope"></param>
    /// <returns></returns>
    [HttpGet("generatetoken")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateToken(string resourceId = "8bffeaa6-2d18-4059-9335-ce805e2c1595", string scope = ".default")
    {
        var tokenProvider = new AzureDefaultCredTokenProvider(new CachingService());
        var token = await tokenProvider.GetAccessTokenAsync(resourceId, scope);
        return Ok(token);
    }

}
