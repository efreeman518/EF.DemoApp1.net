using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using LanguageExt.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Package.Infrastructure.Common.Extensions;
public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// HttpClient extension method. Sends the http request and parses the response in to expected TResponse structure
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="client"></param>
    /// <param name="method"></param>
    /// <param name="url"></param>
    /// <param name="payload"></param>
    /// <param name="headers"></param>
    /// <param name="ensureSuccessStatusCode">If true, ignore <paramref name="throwOnException"/></param>
    /// <param name="throwOnException">If true and <paramref name="ensureSuccessStatusCode"/> is false, attempt to read the response stream and include that in the exception</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public static async Task<(HttpResponseMessage, TResponse?)> HttpRequestAndResponseAsync<TResponse>(this HttpClient client,
        System.Net.Http.HttpMethod method, string url, object? payload = null, Dictionary<string, string>? headers = null,
        bool ensureSuccessStatusCode = true, bool throwOnException = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(() => client.CancelPendingRequests());

        var httpRequest = BuildHttpRequest(method, url, payload, headers);
        using HttpResponseMessage httpResponse = await client.SendAsync(httpRequest, cancellationToken);
        if (ensureSuccessStatusCode)
            httpResponse.EnsureSuccessStatusCode();

        if (typeof(TResponse) == typeof(IHtmlDocument))
        {
            return (httpResponse, (TResponse)await GetDocumentAsync(httpResponse, cancellationToken)); //must cast even though we know TResponse is IHtmlDocument
        }

        TResponse? response = default;
        using Stream s = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        if (httpResponse.IsSuccessStatusCode)
        {
            if (s.Length > 0)
            {
                if (typeof(TResponse).IsPrimitive)
                {
                    StreamReader reader = new(s);
                    string val = await reader.ReadToEndAsync(cancellationToken);
                    response = (TResponse)Convert.ChangeType(val, typeof(TResponse));
                }
                else
                {
                    response = await JsonSerializer.DeserializeAsync<TResponse>(s, _jsonSerializerOptions, cancellationToken);
                }
            }
        }
        else
        {
            string? val = null;
            if (s.Length > 0)
            {
                StreamReader reader = new(s);
                val = await reader.ReadToEndAsync(cancellationToken);
            }
            if (throwOnException)
            {
                throw new HttpRequestException($"{val}", null, httpResponse.StatusCode);
            }
        }

        //might not be expecting a response payload (Http Delete - TResponse = object)
        return (httpResponse, response);
    }

    /// <summary>
    /// Optional method to return the response as a Result<TResponse> instead of throwing an exception
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="client"></param>
    /// <param name="method"></param>
    /// <param name="url"></param>
    /// <param name="payload"></param>
    /// <param name="headers"></param>
    /// <param name="ensureSuccessStatusCode"></param>
    /// <param name="throwOnException"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<(HttpResponseMessage, Result<TResponse?>)> HttpRequestAndResponseResultAsync<TResponse>(this HttpClient client,
        System.Net.Http.HttpMethod method, string url, object? payload = null, Dictionary<string, string>? headers = null,
        bool ensureSuccessStatusCode = true, CancellationToken cancellationToken = default)
    {
        var httpRequest = BuildHttpRequest(method, url, payload, headers);
        using HttpResponseMessage httpResponse = await client.SendAsync(httpRequest, cancellationToken);
        if (ensureSuccessStatusCode && !httpResponse.IsSuccessStatusCode)
        {
            return (httpResponse, new Result<TResponse?>(new HttpRequestException(httpResponse.ReasonPhrase, null, httpResponse.StatusCode)));
        }

        if (typeof(TResponse) == typeof(IHtmlDocument))
        {
            return (httpResponse, (TResponse)await GetDocumentAsync(httpResponse, cancellationToken)); //must cast even though we know TResponse is IHtmlDocument
        }

        TResponse? response = default;
        using Stream s = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        if (httpResponse.IsSuccessStatusCode)
        {
            if (s.Length > 0)
            {
                if (typeof(TResponse).IsPrimitive)
                {
                    StreamReader reader = new(s);
                    string val = await reader.ReadToEndAsync(cancellationToken);
                    response = (TResponse)Convert.ChangeType(val, typeof(TResponse));
                }
                else
                {
                    response = await JsonSerializer.DeserializeAsync<TResponse>(s, _jsonSerializerOptions, cancellationToken);
                }
            }

            //might not be expecting a response payload (Http Delete - TResponse = object)
            return (httpResponse, response);
        }
        else
        {
            string? val = null; //possible ProblemDetails
            if (s.Length > 0)
            {
                StreamReader reader = new(s);
                val = await reader.ReadToEndAsync(cancellationToken);
            }

            return (httpResponse, new Result<TResponse?>(new HttpRequestException(val ?? httpResponse.ReasonPhrase, null, httpResponse.StatusCode)));
        }
    }

    private static HttpRequestMessage BuildHttpRequest(System.Net.Http.HttpMethod method, string url, object? payload = null, Dictionary<string, string>? headers = null)
    {
        var httpRequest = new HttpRequestMessage(method, url);
        if (payload != null)
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"); //CONTENT-TYPE header

        headers?.ToList().ForEach(entry =>
        {
            httpRequest.Headers.Add(entry.Key, entry.Value);
        });
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

        return httpRequest;
    }

    /// <summary>
    /// Returns searchable IHtmlDocument
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    private static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = await BrowsingContext.New().OpenAsync(ResponseFactory, cancellationToken);
        return (IHtmlDocument)document;

        void ResponseFactory(VirtualResponse htmlResponse)
        {
            htmlResponse
                .Address(response.RequestMessage!.RequestUri)
                .Status(response.StatusCode);

            MapHeaders(response.Headers);
            MapHeaders(response.Content.Headers);

            htmlResponse.Content(content);

            void MapHeaders(HttpHeaders headers)
            {
                foreach (var header in headers)
                {
                    foreach (var value in header.Value)
                    {
                        htmlResponse.Header(header.Key, value);
                    }
                }
            }
        }
    }
}
