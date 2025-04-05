using Microsoft.AspNetCore.Components;
using MudBlazor;
using SampleApp.UI1.Utility;
using System.Globalization;

namespace SampleApp.UI1.Layout;

public partial class MainLayout(AppStateService appStateService, NavigationManager nav) : IDisposable
{
    private bool Rtl = false;
    private bool DrawerOpen = false;

    protected override void OnInitialized()
    {
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

        //settings changes (drak mode) should refresh the UI 
        appStateService.OnChange += StateHasChanged;
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
        appStateService.OnChange -= StateHasChanged;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
