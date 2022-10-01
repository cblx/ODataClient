using Microsoft.Extensions.Options;

namespace Cblx.Dynamics;

public class DynamicsAuthorizationMessageHandler : DelegatingHandler {
    private readonly IDynamicsAuthenticator _dynamicsAuthenticator;
    private readonly DynamicsConfig _config;

    public DynamicsAuthorizationMessageHandler(
        IDynamicsAuthenticator dynamicsAuthenticator,
        DynamicsConfig config
    )
    {
        _dynamicsAuthenticator = dynamicsAuthenticator;
        _config = config;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        
        request.Headers.Authorization = await _dynamicsAuthenticator.GetAuthenticationHeaderValue(_config);
        return await base.SendAsync(request, cancellationToken);
    }
}