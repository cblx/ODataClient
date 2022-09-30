using Microsoft.Extensions.Options;

namespace Cblx.Dynamics;

public class DynamicsAuthorizationMessageHandler : DelegatingHandler {
    private readonly DynamicsAuthenticator _dynamicsAuthenticator;
    private readonly DynamicsConfig _config;

    public DynamicsAuthorizationMessageHandler(
        DynamicsAuthenticator dynamicsAuthenticator,
        IOptionsSnapshot<DynamicsConfig> configOptions
    )
    {
        _dynamicsAuthenticator = dynamicsAuthenticator;
        _config = configOptions.Value;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = await _dynamicsAuthenticator.GetAuthenticationHeaderValue(_config);
        //request.Headers.
        return await base.SendAsync(request, cancellationToken);
    }
}