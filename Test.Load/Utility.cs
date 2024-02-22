using NBomber.Contracts;
using NBomber.CSharp;
using Package.Infrastructure.Common.Extensions;
using System.Text.Json;

namespace Test.Load;
internal static class Utility
{
    public static async Task<Response<object>> RunStep<TRequest, TResponse>(IScenarioContext context, HttpClient httpClient, string stepName, HttpMethod method, string url, Func<IScenarioContext, string>? urlBuilder = null, Func<IScenarioContext, TRequest>? payloadBuilder = null)
    {
        TRequest? payload = default;

        return await Step.Run(stepName, context,
            async () =>
            {
                var sUrl = url;
                //append url based on previous step/request response
                if (urlBuilder != null) sUrl += urlBuilder(context) ?? "";

                //attach payload based on previous step/request response
                if (payloadBuilder != null) payload = payloadBuilder(context);

                //hit the endpoint
                (HttpResponseMessage responseMsg, TResponse? response) = await httpClient.HttpRequestAndResponseAsync<TResponse>(method, sUrl, payload);

                //convention to preserve the response so the following step can use it to build it's url and/or payload
                context.Data[stepName] = response;

                int size = response != null ? GetByteSize(response) : 0;
                return responseMsg.IsSuccessStatusCode
                    ? Response.Ok(sizeBytes: size, statusCode: responseMsg.StatusCode.ToString())
                    : Response.Fail(message: responseMsg.ReasonPhrase, statusCode: responseMsg.StatusCode.ToString(), sizeBytes: size);

            });
    }

    private static int GetByteSize(object o)
    {
        MemoryStream ms = new();
        JsonSerializer.Serialize(ms, o);
        return ms.ToArray().Length;
    }
}
