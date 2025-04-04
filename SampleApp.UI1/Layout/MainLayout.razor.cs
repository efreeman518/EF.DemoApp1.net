using Microsoft.AspNetCore.Components;
using MudBlazor;
using SampleApp.UI1.Utility;
using System.Globalization;

namespace SampleApp.UI1.Layout;

public partial class MainLayout(AppStateService appStateService, NavigationManager navigationManager) : IDisposable
{
    private bool Rtl = false;
    private bool DrawerOpen = false;

    protected override void OnInitialized()
    {
        //rtl
        var rtlLanguages = new[] { "ar", "he", "ur", "fa", "ps", "sd", "iw" }; // Arabic, Hebrew, Urdu, Farsi, Pashto, Sindhi, etc.
        Rtl = rtlLanguages.Contains(CultureInfo.CurrentCulture.Name[..2]);

        appStateService.OnChange += StateHasChanged;
    }

    private void DrawerToggle()
    {
        DrawerOpen = !DrawerOpen;
    }

    private void Nav(string url)
    {
        navigationManager.NavigateTo(url);
    }

    protected virtual void Dispose(bool disposing)
    {
        appStateService.OnChange -= StateHasChanged;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
