var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SampleApp_Api>("aca-sampleapp-api-dev1");

builder.AddProject<Projects.SampleApp_Gateway>("aca-gateway-dev1")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

//just launch in aspire for the dashboard even though blazor wasm has no service discovery or other aspire integration
//don't get local debugging since VS is attached to the aspire apps
//builder.AddProject<Projects.SampleApp_UI1>("ui1");

await builder.Build().RunAsync();