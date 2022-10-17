using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Test.Endpoints;

public static class HttpClientExtensions
{
    /// <summary>
    /// HttpClient extension method. Sends the http request and parses the response in to expected TResponse structure
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="client"></param>
    /// <param name="method"></param>
    /// <param name="url"></param>
    /// <param name="payload"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public static async Task<(HttpResponseMessage, TResponse?)> HttpRequestAndResponse<TRequest, TResponse>(this HttpClient client,
        HttpMethod method, string url, TRequest? payload, NameValueCollection? headers = null, bool ensureSuccessStatusCode = true)
    {
        var httpRequest = new HttpRequestMessage(method, url);
        if (payload != null)
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"); //CONTENT-TYPE header

        headers?.AllKeys.ToList().ForEach(key =>
        {
            client.DefaultRequestHeaders.Add(key!, headers[key]);
        });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

        using HttpResponseMessage httpResponse = await client.SendAsync(httpRequest);
        if (ensureSuccessStatusCode)
            httpResponse.EnsureSuccessStatusCode();

        if (typeof(TResponse).GetTypeInfo().IsAssignableFrom(typeof(IHtmlDocument).Ge‌​tTypeInfo()))
        {
            return (httpResponse, (TResponse)await Utility.GetDocumentAsync(httpResponse)); //must cast even though we know TResponse is IHtmlDocument
        }

        TResponse? response = default;
        using Stream s = await httpResponse.Content.ReadAsStreamAsync();
        if (typeof(TResponse).IsPrimitive)
        {
            string? val = null;
            if (httpResponse.IsSuccessStatusCode)
            {
                StreamReader reader = new(s);
                val = reader.ReadToEnd();
                response = (TResponse)Convert.ChangeType(val, typeof(TResponse));
            }
            else
                throw new Exception($"{httpResponse.StatusCode} - {val}");
        }
        else
        {
            response = await JsonSerializer.DeserializeAsync<TResponse>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        if (response == null) throw new Exception("null response");
        return (httpResponse, response);
    }

    //https://github.com/dotnet/AspNetCore.Docs/blob/f7f7e6035d4fa2ecd88a969c17af7d95ae6642d8/aspnetcore/test/integration-tests/samples/2.x/IntegrationTestsSample/tests/RazorPagesProject.Tests/Helpers/HttpClientExtensions.cs

    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        IHtmlFormElement form,
        IHtmlElement submitButton)
    {
        return client.SendAsync(form, submitButton, new Dictionary<string, string>());
    }

    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        IHtmlFormElement form,
        IEnumerable<KeyValuePair<string, string>> formValues)
    {
        var submitElement = form.QuerySelectorAll("[type=submit]").Single();
        var submitButton = (IHtmlElement)submitElement;

        return client.SendAsync(form, submitButton, formValues);
    }

    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        IHtmlFormElement form,
        IHtmlElement submitButton,
        IEnumerable<KeyValuePair<string, string>> formValues)
    {
        foreach (var kvp in formValues)
        {
            var element = form[kvp.Key] as IHtmlInputElement;
            element!.Value = kvp.Value;
        }

        var docRequest = form.GetSubmission(submitButton);
        var target = (Uri)docRequest!.Target;
        if (submitButton.HasAttribute("formaction"))
        {
            var formaction = submitButton.GetAttribute("formaction");
            target = new Uri(formaction!, UriKind.Relative);
        }
        var submision = new HttpRequestMessage(new HttpMethod(docRequest.Method.ToString()), target)
        {
            Content = new StreamContent(docRequest.Body)
        };

        foreach (var header in docRequest.Headers)
        {
            submision.Headers.TryAddWithoutValidation(header.Key, header.Value);
            submision.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return client.SendAsync(submision);
    }
}
