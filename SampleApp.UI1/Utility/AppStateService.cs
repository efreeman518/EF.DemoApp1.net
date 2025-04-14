using Blazored.LocalStorage;
using System.Globalization;

namespace SampleApp.UI1.Utility;

public class AppStateService(ILocalStorageService localStorage, IJsInteropUtility jsInteropUtility)
{
    public event Action<string?>? SettingChanged;

    //private readonly Dictionary<string, object> _settings = [];
    //public T? Get<T>(string key) => _settings.TryGetValue(key, out var value) ? (T)value : default;

    //called from Program.cs before host.RunAsync
    public async Task InitializeAsync()
    {
        var cultureName = await GetSetting("SampleAppCultureName", null, async () => await jsInteropUtility.GetBrowserCultureNameAsync());
        if (cultureName != CultureInfo.CurrentCulture.Name)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(cultureName!);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(cultureName!);
        }
    }

    /// <summary>
    /// Retrieves a setting from the app state or fetches if factory function provided.
    /// </summary>
    public async ValueTask<T?> GetSetting<T>(string key, T? defaultVal, Func<Task<T?>>? factory = null, CancellationToken cancellationToken = default)
    {
        var item = await localStorage.GetItemAsync<T?>(key, cancellationToken);
        if (item is null)
        {
            if (defaultVal is not null)
            {
                item = defaultVal;
            }
            else if (factory != null)
            {
                //only run the factory is the item is null and no non-null default provided
                item = await factory();
            }
            await SetSetting(key, item, cancellationToken: cancellationToken);
        }

        return item;
    }

    /// <summary>
    /// Updates a setting and saves to local storage.
    /// </summary>
    public async Task SetSetting<T>(string key, T value, bool nullRemoval = true, CancellationToken cancellationToken = default)
    {
        //update in local storage
        //remove if null
        if (value is null && nullRemoval)
        {
            await localStorage.RemoveItemsAsync([key], cancellationToken);
        }
        else
        {
            await localStorage.SetItemAsync<T>(key, value, cancellationToken);
        }

        NotifySettingsChangedAsync(key);
    }

    private void NotifySettingsChangedAsync(string? key = null)
    {
        SettingChanged?.Invoke(key);
    }
}
