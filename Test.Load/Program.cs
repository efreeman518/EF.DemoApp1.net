// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Test.Load;

Console.WriteLine("Load Tester");

string baseUrl = Utility.Config.GetValue<string>("SampleApi:BaseUrl");
TodoLoadTest.Run(baseUrl);
