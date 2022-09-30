using Microsoft.Extensions.Options;

namespace Cblx.Dynamics;

public class DynamicsAuthorizationMessageHandler : DelegatingHandler {
    private readonly IDynamicsAuthenticator _dynamicsAuthenticator;
    private readonly DynamicsConfig _config;

    public DynamicsAuthorizationMessageHandler(
        IDynamicsAuthenticator dynamicsAuthenticator,
        IOptionsSnapshot<DynamicsConfig> configOptions
    )
    {
        _dynamicsAuthenticator = dynamicsAuthenticator;
        _config = configOptions.Value;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = await _dynamicsAuthenticator.GetAuthenticationHeaderValue(_config);
        var uri = new Uri(_config.ResourceUrl);
        uri = new Uri(uri, $"api/data/v9.0{request.RequestUri!.PathAndQuery}");
        request.RequestUri = uri;
        return await base.SendAsync(request, cancellationToken);
    }
}