var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SampleApp_Api>("api");

var gateway = builder.AddProject<Projects.SampleApp_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

//browser can't resolve aspire service names (gateway), defeating the purpose
//builder.AddProject<Projects.SampleApp_UI1>("ui1")
//    .WithExternalHttpEndpoints()
//    .WithReference(gateway)
//    .WaitFor(gateway);

await builder.Build().RunAsync();
