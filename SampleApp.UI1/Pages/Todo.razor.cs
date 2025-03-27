using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SampleApp.UI1.Model;
using SampleApp.UI1.Services;

namespace SampleApp.UI1.Pages;

[Authorize]
public partial class Todo(IStringLocalizer<Localization.Locals> Localizer, ISampleAppClient sampleAppClient)
{
    MudDataGrid<TodoItemDto> DataGrid { get; set; } = null!;
    string? searchString = null;

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
