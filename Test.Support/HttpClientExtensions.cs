using AngleSharp.Html.Dom;

namespace Test.Support;

//https://github.com/dotnet/AspNetCore.Docs/blob/f7f7e6035d4fa2ecd88a969c17af7d95ae6642d8/aspnetcore/test/integration-tests/samples/2.x/IntegrationTestsSample/tests/RazorPagesProject.Tests/Helpers/HttpClientExtensions.cs

public static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        IHtmlFormElement form,
        IHtmlElement submitButton)
    {
        return client.SendAsync(form, submitButton, []);
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
