using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Cblx.Dynamics;

public class DynamicsAuthenticator
{
    private int _current = 0;
    private readonly object _toSync = new();
    private readonly ConcurrentDictionary<string, List<ClientAuthCredentials>> _clientsPerDynamics = new();
    //private readonly AuthenticationContext _authenticationContext;
    //private readonly IOptions<DynamicsConfig> _configOptions;

    //public string DefaultResourceUrl => _configOptions.Value.ResourceUrl;
    
    public DynamicsAuthenticator(/*IOptions<DynamicsConfig> configOptions*/)
    {
        //_configOptions = configOptions;
        //_authenticationContext = new AuthenticationContext(configOptions.Value.Authority);
    }

    private List<ClientAuthCredentials> GetClients(DynamicsConfig config)
    {
        string configKey = $"{config.Authority}-{config.ResourceUrl}-{config.Users}";
        return _clientsPerDynamics.GetOrAdd(configKey, _ => new());
    }

    //public async Task AuthenticateHttpClient(HttpClient httpClient, DynamicsConfig config)
    //{
    //    string accessToken = await GetAccessToken(config);
    //    httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {accessToken}");
    //    httpClient.BaseAddress = new Uri($"{config.ResourceUrl}api/data/v9.0/");
    //}

    public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderValue(DynamicsConfig config)
    {
        string accessToken = await GetAccessToken(config);
        return AuthenticationHeaderValue.Parse($"Bearer {accessToken}");
    }

    private async Task<string> GetAccessToken(DynamicsConfig config)
    {
        
        await Authenticate(config);
        var clients = GetClients(config);
        lock (_toSync)
        {
            if (_current >= clients.Count) _current = 0;
            Console.WriteLine("===========");
            Console.WriteLine($"Dynamics User: {_current}");
            Console.WriteLine("===========");
            return clients[_current++].Authentication!.AccessToken;
        }
    }

    async Task Authenticate(DynamicsConfig config)
    {
        var clients = GetClients(config);
        var authenticationContext = new AuthenticationContext(config.Authority);
        if (clients.Count == 0)
        {
            foreach (var item in config.UserConfigs)
            {
                var credential = new ClientCredential(item.ClientId, item.ClientSecret);
                var authentication = await authenticationContext.AcquireTokenAsync(config.ResourceUrl, credential);

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
                var authentication = await authenticationContext.AcquireTokenAsync(config.ResourceUrl, item.Credentials);
                item.Authentication = authentication;
            }
        }
    }
}