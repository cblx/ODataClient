using System.Net;

namespace OData.Client.UnitTests;
public class MockHttpMessageHandler : HttpMessageHandler
{
    readonly string _content;
    readonly HttpStatusCode _statusCode;
    public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        this._content = responseContent;
        this._statusCode = statusCode;
    }

    public HttpRequestMessage? LastRequestMessage { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestMessage = request;
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_content),
            StatusCode = _statusCode
        };

        return await Task.FromResult(responseMessage);
    }
}
