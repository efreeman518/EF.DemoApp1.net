using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.AspNetCore;
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


    [AllowAnonymous]
    [HttpPost]
    [Route("encodefinished")]
    public async Task<ActionResult> WebhookEncodeJobFinished()
    {
        try
        {
            //Request.Body.Position = 0;
            var rawRequestBody = await (new StreamReader(Request.Body)).ReadToEndAsync();

            //await _svcHttpProxy.HttpAsync<string, bool>(HttpMethod.Post, "content/media/encodefinished", payload, GetUrlSubdomainPartnerCode().Item4);
            return Ok();
        }
        catch (Exception ex)
        {
            //return InternalServerError(ex);
        }
        return Ok();

    }


    [AllowAnonymous]
    [HttpPost]
    [Route("encodetransfererror")]
    public async Task<ActionResult> WebhookEncodeTransferError()
    {
        try
        {
            //Request.Body.Position = 0;
            var rawRequestBody = await (new StreamReader(Request.Body)).ReadToEndAsync();
            //await _svcHttpProxy.HttpAsync<string, bool>(HttpMethod.Post, "content/media/encodetransfererror", payload, GetUrlSubdomainPartnerCode().Item4);
            return Ok();
        }
        catch (Exception ex)
        {
            //return InternalServerError(ex);
        }
        return Ok();

    }


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

    //[HttpGet("{id:Guid}")]
    //[SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(TodoItemDto))]
    //[SwaggerResponse((int)HttpStatusCode.BadRequest, "BadRequest - Guid not valid", typeof(ValidationProblemDetails))]
    //[SwaggerResponse((int)HttpStatusCode.NotFound, "Not Found", typeof(Guid))]
    //public async Task<ActionResult<TodoItemDto>> GetTodoItem(Guid id)
    //{
    //    var todoItem = await _apiClient.GetItemAsync(id);
    //    return (todoItem != null)
    //        ? Ok(todoItem)
    //        : NotFound(id);
    //}




    [HttpGet("{id:Guid}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "BadRequest - Guid not valid", typeof(ValidationProblemDetails))]
    [SwaggerResponse((int)HttpStatusCode.NotFound, "Not Found", typeof(Guid))]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem_NoAttribute(Guid id)
    {
        var todoItem = await _apiClient.GetTodoItem_NoAttribute(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    [HttpGet("GetTodoItem_Role_SomeAccess1/{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem_Role_SomeAccess1(Guid id)
    {
        var todoItem = await _apiClient.GetTodoItem_Role_SomeAccess1(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    [HttpGet("GetTodoItem_Policy_AdminPolicy/{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_AdminPolicy(Guid id)
    {
        var todoItem = await _apiClient.GetTodoItem_Policy_AdminPolicy(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    [HttpGet("GetTodoItem_Policy_SomeRolePolicy1/{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_SomeRolePolicy1(Guid id)
    {
        var todoItem = await _apiClient.GetTodoItem_Policy_SomeRolePolicy1(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    [HttpGet("GetTodoItem_Policy_SomeScopePolicy1/{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_SomeScopePolicy1(Guid id)
    {
        var todoItem = await _apiClient.GetTodoItem_Policy_SomeScopePolicy1(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    [HttpGet("GetTodoItem_Policy_ScopeOrRolePolicy1/{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_ScopeOrRolePolicy1(Guid id)
    {
        var todoItem = await _apiClient.GetTodoItem_Policy_ScopeOrRolePolicy1(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }

    [HttpGet("GetTodoItem_Policy_ScopeOrRolePolicy2/{id:Guid}")]
    public async Task<ActionResult<TodoItemDto>> GetTodoItem_Policy_ScopeOrRolePolicy2(Guid id)
    {
        var todoItem = await _apiClient.GetTodoItem_Policy_ScopeOrRolePolicy2(id);
        return (todoItem != null)
            ? Ok(todoItem)
            : NotFound(id);
    }





    [HttpPost]
    [SwaggerResponse((int)HttpStatusCode.Created, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Validation Error", typeof(ProblemDetails))]
    public async Task<ActionResult<TodoItemDto>> SaveTodoItem([FromServices] IHostEnvironment hostEnv, TodoItemDto todoItem)
    {
        //todoItem = (await _apiClient.SaveItemAsync(todoItem))!;
        //return CreatedAtAction(nameof(SaveTodoItem), new { id = todoItem.Id }, todoItem);

        var result = await _apiClient.SaveItemAsync(todoItem);
        return result.Match<ActionResult<TodoItemDto>>(
            dto => CreatedAtAction(nameof(TodoItemDto), new { id = dto!.Id }, dto),
            err => hostEnv.BuildProblemDetailsResponse(exception: err, traceId: HttpContext.TraceIdentifier) //throw err // BadRequest(err.Message)
            );
        
    }

    [HttpPut("{id:Guid}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(TodoItemDto))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Validation Error", typeof(ProblemDetails))]
    public async Task<ActionResult<TodoItemDto>> PutTodoItem([FromServices] IHostEnvironment hostEnv, Guid id, TodoItemDto todoItem)
    {
        if (todoItem.Id != Guid.Empty && todoItem.Id != id)
        {
            return BadRequest($"{AppConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {todoItem.Id}");
        }

        //TodoItemDto? todoUpdated = await _apiClient.SaveItemAsync(todoItem);
        //return Ok(todoUpdated);

        var result = await _apiClient.SaveItemAsync(todoItem);
        return result.Match<ActionResult<TodoItemDto>>(
            dto => Ok(dto),
            err => hostEnv.BuildProblemDetailsResponse(exception: err, traceId: HttpContext.TraceIdentifier) //throw err // BadRequest(err.Message)
            );
    }

    [HttpDelete("{id:Guid}")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Model is invalid.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult> DeleteTodoItem(Guid id)
    {
        await _apiClient.DeleteItemAsync(id);
        return Ok();
    }

    //private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions{  WriteIndented = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

    [HttpGet("getuser")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public async Task<ActionResult> GetUser()
    {
        return Ok(await _apiClient.GetUserAsync());
    }

    [HttpGet("getuserclaims")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public async Task<ActionResult> GetUserClaims()
    {
        return Ok(await _apiClient.GetUserClaimsAsync());
    }

    [HttpGet("getauthheader")]
    [SwaggerResponse((int)HttpStatusCode.OK, "Success")]
    public async Task<ActionResult> GetAuthHeader()
    {
        return Ok(await _apiClient.GetAuthHeaderAsync());
    }

}
