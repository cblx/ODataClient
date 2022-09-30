using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using System.Net;

namespace Cblx.Dynamics.Tests;

public class DynamicsAuthorizationMessageHandlerTests
{
    [Fact]
    public async Task MustPrefixUriWithDynamicsConfigResourceUrl()
    {

        var handler = new DynamicsAuthorizationMessageHandler(
            Mock.Of<IDynamicsAuthenticator>(),
            Mock.Of<IOptionsSnapshot<DynamicsConfig>>(snap => snap.Value == new DynamicsConfig
            {
                ResourceUrl = "https://resourceurl.com/"
            })
        );
        var spyHandler = new SpyHandler();
        var httpClient = HttpClientFactory.Create(spyHandler, handler);
        httpClient.BaseAddress = new UriBuilder("https", "d").Uri;
        await httpClient.GetAsync("entities?$select=id");
        spyHandler.FinalUrl.Should().Be("https://resourceurl.com/api/data/v9.0/entities?$select=id");
    }

    class SpyHandler : HttpMessageHandler
    {
        public string? FinalUrl { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            FinalUrl = request.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}