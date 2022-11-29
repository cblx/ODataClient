using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cblx.Dynamics.FetchXml.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    readonly string content;
    readonly HttpStatusCode statusCode;

    public MockHttpMessageHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        this.content = content;
        this.statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content),
            StatusCode = statusCode
        };

        return await Task.FromResult(responseMessage);
    }
}