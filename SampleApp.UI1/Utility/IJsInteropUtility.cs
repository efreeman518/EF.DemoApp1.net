namespace SampleApp.UI1.Utility;

public interface IJsInteropUtility
{
    Task<string> GetBrowserCultureNameAsync();
    Task<bool> GetSystemDarkModeAsync();
}
