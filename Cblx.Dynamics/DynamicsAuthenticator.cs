﻿using System.Collections.Concurrent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Cblx.Dynamics;

public class DynamicsAuthenticator : IDynamicsAuthenticator
{
    private int _current = 0;
    private readonly object _toSync = new();
    private readonly ConcurrentDictionary<string, List<ClientAuthCredentials>> _clientsPerDynamics = new();
    
    public DynamicsAuthenticator()
    {
    }

    private List<ClientAuthCredentials> GetClients(DynamicsConfig config)
    {
        string configKey = $"{config.Authority}-{config.ResourceUrl}-{config.Users}-{config.ClientId}-{config.ClientSecret}";
        return _clientsPerDynamics.GetOrAdd(configKey, _ => new());
    }
  
    public async Task<string> GetAccessToken(DynamicsConfig config)
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