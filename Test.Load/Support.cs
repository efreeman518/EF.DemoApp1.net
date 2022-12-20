using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using System.Text;

namespace Test.Load;
internal static class Support
{
    public static IStep CreateStep(IClientFactory<HttpClient> clientFactory, string name, string url, HttpMethod method, string? body = null)
    {
        string uri = $"https://localhost:44318/{url}";
        var step = Step.Create(name,
            clientFactory: clientFactory,
            execute: context =>
            {
                var request = Http.CreateRequest(method.Method, uri);

                if (!string.IsNullOrEmpty((body)))
                {
                    var stringContent = new StringContent(body, Encoding.UTF8, "application/json");
                    request = request.WithBody(stringContent);
                }
                var resp = Http.Send(request, context);
                return resp;
            });
        return step;
    }
}
