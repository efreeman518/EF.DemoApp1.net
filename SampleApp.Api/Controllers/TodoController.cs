using Application.Contracts.Model;
using Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.Data.Contracts;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;
using AppConstants = Application.Contracts.Constants.Constants;

namespace SampleApp.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ApiVersion("1.0")]
[ApiVersion("1.1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly ITodoService _todoService;
    public TodoItemsController(ITodoService todoService)
    {
        _todoService = todoService;
    }

    /// <summary>
    /// Gets a paged list of TodoItems
    /// </summary>
    /// <param name="pageSize"></param>
    /// <param name="pageIndex"></param>
    /// <returns></returns>
    [MapToApiVersion("1.0")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TodoItemDto>>> GetTodoItems(int pageSize = 10, int pageIndex = 1)
    {
        var items = await _todoService.GetItemsAsync(pageSize, pageIndex);
        return Ok(items);
    }

    [MapToApiVersion("1.1")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TodoItemDto>>> GetTodoItems_1_1(int pageSize = 20, int pageIndex = 1)
    {
        var items = await _todoService.GetItemsAsync(pageSize, pageIndex);
        return Ok(items);
    }

    [HttpGet("{id:Guid}")]
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

    [HttpPost]
    [SwaggerResponse((int)HttpStatusCode.Created, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Validation Error", typeof(ProblemDetails))]
    public async Task<ActionResult<TodoItemDto>> PostTodoItem(TodoItemDto todoItem)
    {
        todoItem = await _todoService.AddItemAsync(todoItem);
        return CreatedAtAction(nameof(PostTodoItem), new { id = todoItem.Id }, todoItem);
    }

    [HttpPut("{id:Guid}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Validation Error", typeof(ProblemDetails))]
    public async Task<ActionResult<TodoItemDto>> PutTodoItem(Guid id, TodoItemDto todoItem)
    {
        if (todoItem.Id != Guid.Empty && todoItem.Id != id)
        {
            return BadRequest($"{AppConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {todoItem.Id}");
            //throw new ValidationException($"{Constants.ERROR_URL_BODY_ID_MISMATCH}: {id} != {todoItem.Id}");
        }

        todoItem.Id = id;
        TodoItemDto? todoUpdated = await _todoService.UpdateItemAsync(todoItem);

        return Ok(todoUpdated);
    }

    [HttpDelete("{id:Guid}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Model is invalid.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult> DeleteTodoItem(Guid id)
    {
        await _todoService.DeleteItemAsync(id);
        return Ok();
    }

    [HttpGet("getuser")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public IActionResult GetUser()
    {
        var user = HttpContext.User;
        return new JsonResult(user, new JsonSerializerOptions { WriteIndented = true });
    }

    [HttpGet("getuserclaims")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public IActionResult GetUserClaims()
    {
        var user = HttpContext.User;
        return new JsonResult(user.Claims, new JsonSerializerOptions { WriteIndented = true });
    }

    [HttpGet("pageexternal")]
    public async Task<ActionResult<PagedResponse<TodoItemDto>>> GetTodoItemsExternal(int pageSize = 10, int pageIndex = 1)
    {
        var items = await _todoService.GetItemsExternalAsync(pageSize, pageIndex);
        return Ok(items);
    }
}
