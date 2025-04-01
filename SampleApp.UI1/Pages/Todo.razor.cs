using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SampleApp.UI1.Model;
using SampleApp.UI1.Services;

namespace SampleApp.UI1.Pages;

[Authorize]
public partial class Todo(IStringLocalizer<Localization.Locals> Localizer, ISampleAppClient sampleAppClient)
{
    MudDataGrid<TodoItemDto> DataGrid { get; set; } = null!;
    private string? searchString;
    private TodoItemDto model = new();
    bool success;
    MudForm form = null!;
    private string[] errors = [];


    private IEnumerable<string> NameValidation(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return Localizer["Required"];
            yield break;
        }
        if (!name.Contains("a", StringComparison.CurrentCulture))
            yield return "Name must include 'a'.";
    }

    private async Task OnValidSubmit(EditContext context)
    {
        model = model.Id == null 
            ? await sampleAppClient.CreateItemAsync(model)
            : await sampleAppClient.UpdateItemAsync((Guid)model.Id, model);

        StateHasChanged();
    }

    private Task OnSearch(string text)
    {
        searchString = text;
        return DataGrid.ReloadServerData();
    }

    private async Task<GridData<TodoItemDto>> ServerReload(GridState<TodoItemDto> state)
    {
        var response = await sampleAppClient.GetPageAsync(state.PageSize, state.Page + 1);
        var data = response.Data.AsEnumerable();

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
