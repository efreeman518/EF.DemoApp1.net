using Blazored.LocalStorage;
using MudBlazor;
using System.Globalization;

namespace SampleApp.UI1.Utility;

public class AppStateService(ILocalStorageService localStorage, IJsInteropUtility jsInteropUtility)
{
    public event Action? OnChange;

    //private readonly Dictionary<string, object> _settings = [];
    //public T? Get<T>(string key) => _settings.TryGetValue(key, out var value) ? (T)value : default;

    public string CultureName { get; set; } = "en-US"; 
    public MudTheme Theme { get; set; } = ColorThemes.Theme1;
    public bool IsDarkMode { get; set; } = false;

    public async Task InitializeAsync()
    {
        //culture/language
        CultureName = await localStorage.GetItemAsync<string>("SampleAppCultureName")
            ?? await jsInteropUtility.GetBrowserCultureNameAsync()
            ?? "en-US";

        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(CultureName);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(CultureName);

        //_settings["Language"] = await _localStorage.GetItemAsync<string>("Language") ?? "en";
        //_settings["Theme"] = await _localStorage.GetItemAsync<string>("Theme") ?? "light";

        //theme
        //determine MudTheme from the saved name
        var themeName = await localStorage.GetItemAsync<string>("ThemeName") ?? "Theme1";
        var theme = typeof(ColorThemes).GetProperty(themeName);
        if (theme != null)
        {
            Theme = (MudTheme)theme.GetValue(null)!;
        }
        else
        {
            // Fallback to default theme if not found
            Theme = ColorThemes.Theme1;
        }

        //darkmode
        IsDarkMode = await localStorage.GetItemAsync<bool>("DarkMode"); //jsInteropUtility.GetSystemDarkModeAsync()

        //await NotifyStateChangedAsync();
    }

    /// <summary>
    /// Retrieves a setting from the app state or fetches if factory function provided.
    /// </summary>
    public async Task<T?> GetOrFetchSetting<T>(string key, Func<Task<T?>>? factory = null, CancellationToken cancellationToken = default)
    {
        var item = await localStorage.GetItemAsync<T?>(key, cancellationToken);
        if (item == null && factory != null)
        {
            item = await factory();
            await SetSetting(key, item, cancellationToken: cancellationToken);
        }
        return item;
    }

    /// <summary>
    /// Updates a setting and saves to local storage.
    /// </summary>
    public async Task SetSetting<T>(string key, T value, bool nullRemoval = true, CancellationToken cancellationToken = default)
    {
        //remove if null
        if (value is null && nullRemoval)
        {
            await localStorage.RemoveItemsAsync([key], cancellationToken);
        }
        else
        {
            await localStorage.SetItemAsync<T>(key, value, cancellationToken);
        }

        NotifyStateChangedAsync();
    }

    private void NotifyStateChangedAsync()
    {
        OnChange?.Invoke();
    }
}
