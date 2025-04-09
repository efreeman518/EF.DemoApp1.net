using Microsoft.AspNetCore.Components;
using MudBlazor;
using SampleApp.UI1.Utility;
using System.Globalization;

namespace SampleApp.UI1.Layout;

public partial class MainLayout(AppStateService appState, NavigationManager nav) : IDisposable
{
    private bool Rtl = false;
    private bool DrawerOpen = false;
    //private MudTheme Theme = null!;
    //private bool IsDarkMode = appState.Get<bool>("IsDarkMode"); // false;

    protected override void OnInitialized()
    {
        //IsDarkMode = appStateService.IsDarkMode; //.Get<bool>("IsDarkMode");
        //Theme = appState.Get<MudTheme>("Theme")!;

        //var currentPath = nav.ToBaseRelativePath(nav.Uri).Split('?')[0];

        //// 🚨 Skip redirect logic for Azure B2C auth routes
        //if (currentPath.StartsWith("authentication", StringComparison.OrdinalIgnoreCase))
        //    return;

        //check for any anonymous routes and exit
        //var anonymousRoutes = new[] { "authentication/login", "authentication/register", "authentication/forgot-password" };
        //var currentUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        //if (anonymousRoutes.Any(route => currentUrl.StartsWith(route, StringComparison.OrdinalIgnoreCase)))
        //{
        //    return;
        //}

        //Console.WriteLine($"MainLayout loaded for: {nav.ToBaseRelativePath(nav.Uri)}");

        //var authState = await authStateProvider.GetAuthenticationStateAsync();
        //var user = authState.User;

        //if (!user.Identity?.IsAuthenticated ?? false)
        //{
        //    // Redirect to login if not authenticated
        //    var returnUrl = nav.ToBaseRelativePath(nav.Uri);
        //    nav.NavigateTo($"authentication/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        //}

        //rtl
        var rtlLanguages = new[] { "ar", "he", "ur", "fa", "ps", "sd", "iw" }; // Arabic, Hebrew, Urdu, Farsi, Pashto, Sindhi, etc.
        Rtl = rtlLanguages.Contains(CultureInfo.CurrentCulture.Name[..2]);

        //settings changes (dark mode) should refresh the UI 
        appState.OnChange += StateHasChanged;
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
        appState.OnChange -= StateHasChanged;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
