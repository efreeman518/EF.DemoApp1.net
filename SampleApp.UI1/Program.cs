using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Refit;
using SampleApp.UI1;
using SampleApp.UI1.Services;
using SampleApp.UI1.Utility;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

//AZURE AD B2C AUTHENTICATION
//https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/standalone-with-azure-active-directory-b2c?view=aspnetcore-9.0
//https://learn.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-user-flows?pivots=b2c-user-flow
//https://www.youtube.com/watch?v=S5PRH_N7pag&list=PL1MU-CoFk_3vDt8J8XivnYrwKlJHcqAu3&index=14
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
    options.ProviderOptions.LoginMode = "redirect"; //integrated page, not a popup

    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("offline_access");

    //access token to include scopes defined in the UI app registration's API Permissions which are linked to
    //scopes exposed by (a different) AzureB2C api app reg that (Exposes an API - scopes) as scopes and included as part of the sign-in flow
    var scopes = builder.Configuration.GetSection("SampleAppGateway:Scopes").Get<List<string>>();
    if(scopes != null)
    {
        scopes.ForEach(scope => options.ProviderOptions.DefaultAccessTokenScopes.Add(scope));
    }
    
});

//ENTRA ID AUTHENTICATION
//https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/standalone-with-microsoft-entra-id?view=aspnetcore-9.0
//https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/additional-scenarios?view=aspnetcore-9.0
//builder.Services.AddMsalAuthentication(options =>
//{
//    builder.Configuration.Bind("EntraID", options.ProviderOptions.Authentication);
//    options.ProviderOptions.LoginMode = "redirect";
//    options.ProviderOptions.DefaultAccessTokenScopes.Add("api://625bd834-e39d-4cde-bea6-6d4c82e2a178/AccessAllEndpoints");
//});

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//JS Interop utility
builder.Services.AddSingleton<IJsInteropUtility, JsInteropUtility>();
//MudBlazor component support
builder.Services.AddMudServices();
//browser localstorage
builder.Services.AddBlazoredLocalStorage();
// Add localization support
builder.Services.AddLocalization();

//SampleAppGateway client auth handler
builder.Services.AddScoped(provider =>
{
    var tokenProvider = provider.GetRequiredService<IAccessTokenProvider>();
    //tken request can contain scopes from a single resource (api) not multiple (apis + MSGraph)
    var scopes = builder.Configuration.GetSection("SampleAppGateway:Scopes").Get<string[]>();
    return new ApiAuthHandler(tokenProvider, scopes!);
});

//SampleAppGateway client - Refit
builder.Services.AddRefitClient<ISampleAppClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.Configuration["SampleAppGateway:BaseUrl"]!))
    .AddHttpMessageHandler<ApiAuthHandler>();

var host = builder.Build();

//Get/set the app culture/language
var localStorage = host.Services.GetRequiredService<ILocalStorageService>();
var cultureName = await localStorage.GetItemAsync<string>("BlazorAppCultureName");
if (string.IsNullOrEmpty(cultureName))
{
    var jsInterop = host.Services.GetRequiredService<IJsInteropUtility>();
    cultureName = await jsInterop.GetBrowserCultureNameAsync();
}

var culture = new CultureInfo(cultureName);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture; //https://github.com/dotnet/aspnetcore/issues/56824

await host.RunAsync();
