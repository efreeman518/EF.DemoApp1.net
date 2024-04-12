using Application.Contracts.Model;
using Application.Contracts.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.AspNetCore;
using Package.Infrastructure.Common.Contracts;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;
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
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(PagedResponse<TodoItemDto>))]
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
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(PagedResponse<TodoItemDto>))]
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
    //[Authorize]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "BadRequest - Guid not valid", typeof(ValidationProblemDetails))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Not Found", typeof(Guid))]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem(Guid id)
    {
        var todoItem = await _todoService.GetItemAsync(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    /// <summary>
    /// Saves a new TodoItem
    /// </summary>
    /// <param name="hostEnv"></param>
    /// <param name="todoItem"></param>
    /// <returns>The new TodoItem that was saved</returns>
    [HttpPost]
    [SwaggerResponse((int)HttpStatusCode.Created, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Model Binding Error", typeof(ValidationProblemDetails))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Validation Error", typeof(ProblemDetails))]
    public async Task<ActionResult<TodoItemDto>> PostTodoItem([FromServices] IHostEnvironment hostEnv, TodoItemDto todoItem)
    {
        var result = await _todoService.AddItemAsync(todoItem);
        return result.Match<ActionResult<TodoItemDto>>(
            dto => CreatedAtAction(nameof(PostTodoItem), new { id = dto!.Id }, dto),
            err => hostEnv.BuildProblemDetailsResponse(exception: err, traceId: HttpContext.TraceIdentifier) //throw err // BadRequest(err.Message)
            );

    }

    /// <summary>
    /// Updates an existing TodoItem
    /// </summary>
    /// <param name="id"></param>
    /// <param name="todoItem"></param>
    /// <returns>The updated TodoItem</returns>
    [HttpPut("{id:Guid}")]
    //[Authorize(Roles = "SomeAccess1")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Validation Error", typeof(ProblemDetails))]
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
    //[Authorize(Policy = "SomeAccess1Policy")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Model is invalid.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult> DeleteTodoItem(Guid id)
    {
        await _todoService.DeleteItemAsync(id);
        return Ok();
    }

    /// <summary>
    /// Gets the current user
    /// </summary>
    /// <returns>User data in json</returns>
    [HttpGet("getuser")]
    //[Authorize]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public IActionResult GetUser()
    {
        var user = HttpContext.User;
        return new JsonResult(user, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Gets the current user's claims
    /// </summary>
    /// <returns>Claims data in json</returns>
    [HttpGet("getuserclaims")]
    //[Authorize(Roles = "SomeAccess1")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public IActionResult GetUserClaims()
    {
        var user = HttpContext.User;
        return new JsonResult(user.Claims, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Gets the current user's auth header in json
    /// </summary>
    /// <returns></returns>
    [HttpGet("getauthheader")]
    //[Authorize(Policy = "SomeAccess1Policy")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public IActionResult GetAuthHeader()
    {
        var authHeaders = HttpContext.Request.Headers.Authorization;
        return new JsonResult(authHeaders, new JsonSerializerOptions { WriteIndented = true });
    }

}
