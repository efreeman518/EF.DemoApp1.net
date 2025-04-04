using Microsoft.JSInterop;

namespace SampleApp.UI1.Utility;

public class JsInteropUtility(IJSRuntime jsRuntime) : IJsInteropUtility, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private IJSObjectReference? _module;

    /// <summary>
    /// Ensures the JS module is loaded.
    /// </summary>
    private async Task<IJSObjectReference> GetModule()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/utility.js");
        return _module;
    }

    /// <summary>
    /// Calls a JavaScript function from the module.
    /// </summary>
    public async Task<string> GetBrowserCultureNameAsync()
    {
        var module = await GetModule();
        var cultureName = await module.InvokeAsync<string>("GetBrowserCultureName");
        return cultureName ?? "en-US";
    }

    public async Task<bool> GetSystemDarkModeAsync()
    {
        var module = await GetModule();
        return await module.InvokeAsync<bool>("GetSystemDarkMode");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
