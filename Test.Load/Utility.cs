using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.CSharp;
using Package.Infrastructure.Utility.Extensions;
using System.Text.Json;

namespace Test.Load;
internal static class Utility
{
    public readonly static IConfigurationRoot Config = Test.Support.Utility.BuildConfiguration().Build();

    public static IStep CreateStep<TRequest, TResponse>(IClientFactory<HttpClient> clientFactory, string name, string url, HttpMethod method, Func<IStepContext<HttpClient, Microsoft.FSharp.Core.Unit>, string>? urlBuilder = null, Func<IStepContext<HttpClient, Microsoft.FSharp.Core.Unit>, TRequest>? payloadBuilder = null)
    {
        TRequest? payload = default;

        var step = Step.Create(name,
            clientFactory: clientFactory,
            execute: async context =>
            {
                var sUrl = url;
                //append url based on previous step/request response
                if (urlBuilder != null) sUrl += urlBuilder(context) ?? "";

                //attach payload based on previous step/request response
                if (payloadBuilder != null) payload = payloadBuilder(context);

                //hit the endpoint
                (var responseMsg, var response) = await context.Client.HttpRequestAndResponseAsync<TRequest, TResponse>(method, sUrl, payload, null, true);

                //convention to preserve the response so the following step can use it to build it's url and/or payload
                context.Data[name] = response;

                int size = response != null ? GetByteSize(response) : 0;
                return responseMsg.IsSuccessStatusCode
                    ? Response.Ok(sizeBytes: size, statusCode: (int)responseMsg.StatusCode)
                    : Response.Fail(error: responseMsg.ReasonPhrase, statusCode: (int)responseMsg.StatusCode, sizeBytes: size);

            });
        return step;
    }

    private static int GetByteSize(object o)
    {
        MemoryStream ms = new MemoryStream();
        JsonSerializer.Serialize(ms, o);
        return ms.ToArray().Length;
    }
}
