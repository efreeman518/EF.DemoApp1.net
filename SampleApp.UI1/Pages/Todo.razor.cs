using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Package.Infrastructure.Utility.UI;
using SampleApp.UI1.Model;
using SampleApp.UI1.Services;

namespace SampleApp.UI1.Pages;

[Authorize]
public partial class Todo(IStringLocalizer<Localization.Locals> Localizer, ISampleAppClient sampleAppClient)
{
    MudDataGrid<TodoItemDto> DataGrid { get; set; } = null!;
    private string? searchString;
    private TodoItemDto model = new();
    //bool success;
    MudForm form = null!;
    private bool _requestActive;
    private string? _statusMessage;
    private string _editTabLabel => model?.Id != null ? Localizer["Edit"] : Localizer["Add"];
    //private string[] errors = [];
    //private bool HasErrors => errors != null && errors.Length > 0;

    private async Task OnNameClick(Guid? id)
    {
        // Custom logic, like opening a dialog or navigating
        Console.WriteLine($"Clicked item with ID: {id}");
    }

    private async Task ValidateAndSave()
    {

        await form.Validate();

        if (form.IsValid)
        {
            _requestActive = true;
            _statusMessage = string.Empty;
            //try
            //{
            //    model = model.Id == null
            //        ? await sampleAppClient.CreateItemAsync(model)
            //        : await sampleAppClient.UpdateItemAsync((Guid)model.Id, model);
            //    _statusMessage = "Saved!";
            //}
            //catch (Refit.ApiException ex)
            //{
            //    _statusMessage = $"Error: {ex.ToProblemDetails()}";
            //}

            var result = model.Id == null
                ? await RefitCallHelper.TryApiCallAsync(() => sampleAppClient.CreateItemAsync(model))
                : await RefitCallHelper.TryApiCallAsync(() => sampleAppClient.UpdateItemAsync((Guid)model.Id, model));

            _requestActive = false;
            if (result.IsSuccess)
            {
                model = result.Data!;
                _statusMessage = "Saved";
            }
            else
            {
                var error = result.Problem;
                _statusMessage = error?.Detail ?? "Something went wrong.";
            }
        }
        else
        {
            _statusMessage = "Please fix validation errors.";
        }
    }
    //private IEnumerable<string> NameValidation(string name)
    //{
    //    if (string.IsNullOrWhiteSpace(name))
    //    {
    //        yield return Localizer["Required"];
    //        yield break;
    //    }
    //    if (!name.Contains('a', StringComparison.CurrentCulture))
    //        yield return "Name must include 'a'.";
    //}

    //private async Task OnValidSubmit(EditContext context)
    //{
    //    model = model.Id == null 
    //        ? await sampleAppClient.CreateItemAsync(model)
    //        : await sampleAppClient.UpdateItemAsync((Guid)model.Id, model);

    //    StateHasChanged();
    //}

    private Task OnSearch(string text)
    {
        searchString = text;
        return DataGrid.ReloadServerData();
    }

    private async Task<GridData<TodoItemDto>?> ServerReload(GridState<TodoItemDto> state)
    {
        var result = await RefitCallHelper.TryApiCallAsync(() => sampleAppClient.GetPageAsync(state.PageSize, state.Page + 1));
        //var response = await sampleAppClient.GetPageAsync(state.PageSize, state.Page + 1);

        if (result.Problem is not null)
        {
            var error = result.Problem;
            _statusMessage = error?.Detail ?? "Something went wrong.";
            return null;
        }
        var response = result.Data!;
        var data = response.Data!.AsEnumerable();

        var sortDefinition = state.SortDefinitions.FirstOrDefault();
        if (sortDefinition != null)
        {
            switch (sortDefinition.SortBy)
            {
                case nameof(TodoItemDto.Name):
                    data = data.OrderByDirection(
                        sortDefinition.Descending ? SortDirection.Descending : SortDirection.Ascending,
                        o => o.Name
                    );
                    break;
                case nameof(TodoItemDto.Status):
                    data = data.OrderByDirection(
                        sortDefinition.Descending ? SortDirection.Descending : SortDirection.Ascending,
                        o => o.Status
                    );
                    break;
                case nameof(TodoItemDto.SecureDeterministic):
                    data = data.OrderByDirection(
                        sortDefinition.Descending ? SortDirection.Descending : SortDirection.Ascending,
                        o => o.SecureDeterministic
                    );
                    break;
                case nameof(TodoItemDto.SecureRandom):
                    data = data.OrderByDirection(
                        sortDefinition.Descending ? SortDirection.Descending : SortDirection.Ascending,
                        o => o.SecureRandom
                    );
                    break;
            }
        }

        return new GridData<TodoItemDto>
        {
            TotalItems = response.Total,
            Items = data
        };
    }
}
