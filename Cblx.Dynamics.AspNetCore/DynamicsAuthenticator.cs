using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Cblx.Dynamics.AspNetCore;
public class DynamicsAuthenticator
{
    private int _current = 0;
    private readonly object _toSync = new();
    private readonly ConcurrentDictionary<string, List<ClientAuthCredentials>> _clientsPerDynamics = new();
    private readonly AuthenticationContext _authenticationContext;
    private readonly IOptions<DynamicsConfig> _configOptions;

    public string DefaultResourceUrl => _configOptions.Value.ResourceUrl;
    
    public DynamicsAuthenticator(IOptions<DynamicsConfig> configOptions)
    {
        _configOptions = configOptions;
        _authenticationContext = new AuthenticationContext(configOptions.Value.Authority);
    }

    private List<ClientAuthCredentials> GetClients(string resourceUrl)
    {
        return _clientsPerDynamics.GetOrAdd(resourceUrl, _ => new());
    }

    public async Task AuthenticateHttpClient(HttpClient httpClient, string? resourceUrl = null)
    {
        resourceUrl = resourceUrl ?? DefaultResourceUrl;
        string accessToken = await GetAccessToken(resourceUrl);
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {accessToken}");
        httpClient.BaseAddress = new Uri($"{resourceUrl}api/data/v9.0/");
    }

    private async Task<string> GetAccessToken(string resourceUrl)
    {
        
        await Authenticate(resourceUrl);
        var clients = GetClients(resourceUrl);
        lock (_toSync)
        {
            if (_current >= clients.Count) _current = 0;
            Console.WriteLine("===========");
            Console.WriteLine($"Dynamics User: {_current}");
            Console.WriteLine("===========");
            return clients[_current++].Authentication!.AccessToken;
        }
    }

    async Task Authenticate(string resourceUrl)
    {
        var clients = GetClients(resourceUrl);
        if (clients.Count == 0)
        {
            foreach (var item in _configOptions.Value.UserConfigs)
            {
                var credential = new ClientCredential(item.ClientId, item.ClientSecret);
                var authentication = await _authenticationContext.AcquireTokenAsync(resourceUrl, credential);

                clients.Add(new ClientAuthCredentials
                {
                    Credentials = credential,
                    Authentication = authentication,
                });
            }
        }
        else
        {
            var expiredCredentials = clients.FindAll(c => DateTimeOffset.Now > c.Authentication!.ExpiresOn);
            foreach (var item in expiredCredentials)
            {
                var authentication = await _authenticationContext.AcquireTokenAsync(resourceUrl, item.Credentials);
                item.Authentication = authentication;
            }
        }
    }
}