using Microsoft.AspNetCore.Components;
using MudBlazor;
using SampleApp.UI1.Utility;
using System.Globalization;

namespace SampleApp.UI1.Layout;

public partial class MainLayout(AppStateService appState, NavigationManager nav) : IDisposable
{
    private bool Rtl = false;
    private bool DrawerOpen = true;
    private MudTheme? theme;
    private bool isDarkMode;
    private string backgroundClass => isDarkMode ? "background-dark" : "background-light";

    protected override async Task OnInitializedAsync()
    {
        theme = await appState.GetSetting("Theme", ColorThemes.Theme1);
        isDarkMode = await appState.GetSetting("IsDarkMode", true);

        //rtl
        var rtlLanguages = new[] { "ar", "he", "ur", "fa", "ps", "sd", "iw" }; // Arabic, Hebrew, Urdu, Farsi, Pashto, Sindhi, etc.
        Rtl = rtlLanguages.Contains(CultureInfo.CurrentCulture.Name[..2]);

        //settings changes (dark mode) should refresh the UI 
        appState.SettingChanged += OnSettingChanged;
    }

    private async void OnSettingChanged(string? key = null)
    {
        if (key == "IsDarkMode")
        {
            isDarkMode = await appState.GetSetting("IsDarkMode", true);
            //InvokeAsync(StateHasChanged); // no need to await in event handler; ensure safe re-render
            StateHasChanged();
        }
    }

    private void DrawerToggle()
    {
        DrawerOpen = !DrawerOpen;
    }

    private void Nav(string url)
    {
        nav.NavigateTo(url);
    }

    protected virtual void Dispose(bool disposing)
    {
        appState.SettingChanged -= OnSettingChanged;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
