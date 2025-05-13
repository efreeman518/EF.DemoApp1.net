var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SampleApp_Api>("api");

builder.AddProject<Projects.SampleApp_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

//just launch in aspire for the dashboard even though blazor wasm has no service discovery or other aspire integration
builder.AddProject<Projects.SampleApp_UI1>("ui1");

await builder.Build().RunAsync();