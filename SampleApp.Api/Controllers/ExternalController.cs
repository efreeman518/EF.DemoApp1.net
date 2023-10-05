using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.Data.Contracts;
using Swashbuckle.AspNetCore.Annotations;
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
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "BadRequest - Guid not valid", typeof(ValidationProblemDetails))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Not Found", typeof(Guid))]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem(Guid id)
    {
        var todoItem = await _apiClient.GetItemAsync(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    [HttpPost]
    [SwaggerResponse((int)HttpStatusCode.Created, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Validation Error", typeof(ProblemDetails))]
    public async Task<ActionResult<TodoItemDto>> SaveTodoItem(TodoItemDto todoItem)
    {
        todoItem = (await _apiClient.SaveItemAsync(todoItem))!;
        return CreatedAtAction(nameof(SaveTodoItem), new { id = todoItem.Id }, todoItem);
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
        TodoItemDto? todoUpdated = await _apiClient.SaveItemAsync(todoItem);

        return Ok(todoUpdated);
    }

    [HttpDelete("{id:Guid}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Model is invalid.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult> DeleteTodoItem(Guid id)
    {
        await _apiClient.DeleteItemAsync(id);
        return Ok();
    }

    [HttpGet("getuser")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public async Task<ActionResult> GetUser()
    {
        return new JsonResult(await _apiClient.GetUserAsync());
    }

    [HttpGet("getuserclaims")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public async Task<ActionResult> GetUserClaims()
    {
        return new JsonResult(await _apiClient.GetUserClaimsAsync());
    }

    [HttpGet("getauthheader")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public async Task<ActionResult> GetAuthHeader()
    {
        return new JsonResult(await _apiClient.GetAuthHeaderAsync());
    }

}
