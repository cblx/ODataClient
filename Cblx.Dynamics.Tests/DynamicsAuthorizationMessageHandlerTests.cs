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
        var dynamicsConfig = new DynamicsConfig
        {
            ResourceUrl = "https://resourceurl.com/"
        };
        var handler = new DynamicsAuthorizationMessageHandler(
            Mock.Of<IDynamicsAuthenticator>(),
            dynamicsConfig
        );
        var spyHandler = new SpyHandler();
        var httpClient = HttpClientFactory.Create(spyHandler, handler);
        httpClient.BaseAddress = DynamicsBaseAddress.FromResourceUrl(dynamicsConfig.ResourceUrl);
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