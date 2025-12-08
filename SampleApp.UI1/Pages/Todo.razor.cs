using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Utility.UI;
using SampleApp.UI1.Model;
using SampleApp.UI1.Services;

namespace SampleApp.UI1.Pages;

[Authorize]
public partial class Todo(IStringLocalizer<Localization.Locals> Localizer, ISnackbar snackbar, ISampleAppClient sampleAppClient)
{
    private MudTabs? tabsRef;
    MudDataGrid<TodoItemDto> DataGrid { get; set; } = null!;
    private bool isSearchClicked = false;
    private string? searchString;
    private TodoItemDto model = new();
    MudForm form = null!;
    private bool requestActive;

    private string EditTabLabel => model?.Id != null ? Localizer["Edit"] : Localizer["Add"];

    private async Task NewItem()
    {
        model = new TodoItemDto();
        if (tabsRef is not null)
            await tabsRef.ActivatePanelAsync(1);
    }

    private async Task GetItem(Guid? id)
    {
        var result = await RefitCallHelperFull.TryApiCallAsync(() => sampleAppClient.GetItemAsync((Guid)id!));
        if (result.IsSuccess)
        {
            model = result.Data!;
            if (tabsRef is not null)
                await tabsRef.ActivatePanelAsync(1);
        }
        else
        {
            Console.WriteLine(result.Problem);
            snackbar.Add(result.Problem?.Detail ?? "Error.", Severity.Error);
        }
    }

    private async Task OnRowClicked(DataGridRowClickEventArgs<TodoItemDto> args)
    {
        await GetItem(args.Item.Id);
    }

    private async Task ValidateAndSave()
    {
        await form.Validate();

        if (form.IsValid)
        {
            requestActive = true;
            //_statusMessage = string.Empty;

            var result = model.Id == null
                ? await RefitCallHelperFull.TryApiCallAsync(() => sampleAppClient.CreateItemAsync(model))
                : await RefitCallHelperFull.TryApiCallAsync(() => sampleAppClient.UpdateItemAsync((Guid)model.Id, model));

            requestActive = false;
            if (result.IsSuccess)
            {
                model = result.Data!;
                //_statusMessage = "Saved";
                snackbar.Add("Saved.", Severity.Success, config =>
                {
                    config.VisibleStateDuration = 2000;
                });
            }
            else
            {
                Console.WriteLine(result.Problem);
                snackbar.Add(result.Problem?.Detail ?? "Error.", Severity.Error);
            }
        }
        else
        {
            //_statusMessage = "Please fix validation errors.";
            snackbar.Add("Please fix validation errors.", Severity.Error);
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

    private async Task Search()
    {
        isSearchClicked = true;
        await DataGrid.ReloadServerData();
    }

    private async Task<GridData<TodoItemDto>?> ServerLoad(GridState<TodoItemDto> state)
    {
        //prevent initial autoload until there is a GridState property used to manually build the query
        if (!isSearchClicked)
        {
            return new GridData<TodoItemDto>
            {
                TotalItems = 0,
                Items = []
            };
        }

        //build the query
        var filter = new TodoItemSearchFilter();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            filter.Name = searchString;
        }
        var request = new SearchRequest<TodoItemSearchFilter>
        {
            PageSize = state.PageSize,
            PageIndex = state.Page + 1,
            Filter = filter
        };
        if (state.SortDefinitions.Count > 0)
        {
            request.Sorts = state.SortDefinitions.Select(x => new Sort(x.SortBy, x.Descending ? SortOrder.Descending : SortOrder.Ascending));
        }
        if (state.FilterDefinitions.Count > 0)
        {
            //var filter = new TodoItemSearchFilter();
            //populate the filter based on state
            foreach (var filterDefinition in state.FilterDefinitions)
            {
                //TODO: Add filter logic here from the state
            }
        }

        requestActive = true;
        var result = await RefitCallHelperFull.TryApiCallAsync(() => sampleAppClient.SearchAsync(request));
        requestActive = false;

        if (result.Problem is not null)
        {
            snackbar.Add(result.Problem?.Detail ?? "Error.", Severity.Error);
            return new GridData<TodoItemDto>
            {
                TotalItems = 0,
                Items = []
            };
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
