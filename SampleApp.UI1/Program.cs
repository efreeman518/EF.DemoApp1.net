using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using SampleApp.UI1;
using SampleApp.UI1.Services;
using SampleApp.UI1.Utility;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

using (var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
{
    // Load base config
    var baseStream = await http.GetStreamAsync("appsettings.json");
    builder.Configuration.AddJsonStream(baseStream);

    // Optionally override with environment config
    if (builder.HostEnvironment.IsDevelopment())
    {
        try
        {
            var devStream = await http.GetStreamAsync("appsettings.Development.json");
            builder.Configuration.AddJsonStream(devStream);
        }
        catch
        {
            Console.WriteLine("Development config not found, skipping override.");
        }
    }
}

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
    scopes?.ForEach(scope => options.ProviderOptions.DefaultAccessTokenScopes.Add(scope));

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
//browser localstorage
builder.Services.AddBlazoredLocalStorage();
//App State
builder.Services.AddScoped<AppStateService>();
// Add localization support
builder.Services.AddLocalization();
//MudBlazor component support
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = false;
});

//SampleAppGateway client auth handler
builder.Services.AddScoped(provider =>
{
    var tokenProvider = provider.GetRequiredService<IAccessTokenProvider>();
    //token request can contain scopes from a single resource (api) not multiple (apis + MSGraph)
    var scopes = builder.Configuration.GetSection("SampleAppGateway:Scopes").Get<string[]>();
    return new ApiAuthHandler(tokenProvider, scopes!);
});

//SampleAppGateway client - Refit
builder.Services.AddRefitClient<ISampleAppClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.Configuration["SampleAppGateway:BaseUrl"]!))
    .AddHttpMessageHandler<ApiAuthHandler>()
    .ConfigureHttpClient((sp, client) =>
    {
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddTypedClient((client, sp) => RestService.For<ISampleAppClient>(client, new RefitSettings
    {
        //prevent enum serialization as strings
        ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { } // No JsonStringEnumConverter here
        })
    }));

var host = builder.Build();

//setup initial app state- set thread culture/language
var appState = host.Services.GetRequiredService<AppStateService>();
await appState.InitializeAsync();

await host.RunAsync();
