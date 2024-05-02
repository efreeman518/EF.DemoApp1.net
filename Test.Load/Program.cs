// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Test.Load;

Console.WriteLine("Load Tester");

var config = Test.Support.Utility.BuildConfiguration().Build();
string baseUrl = config.GetValue<string>("SampleApi:BaseUrl")!;
TodoLoadTest.Run(baseUrl);
//TodoLoadTestExternal.Run(baseUrl);
