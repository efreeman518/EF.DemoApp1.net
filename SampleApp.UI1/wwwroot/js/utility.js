
const GetBrowserCultureName = () => navigator.language || "en-US";

const GetSystemDarkMode = () => window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

// Export modules for import in Blazor
export { GetBrowserCultureName, GetSystemDarkMode };
