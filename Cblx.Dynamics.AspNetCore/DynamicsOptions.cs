using OData.Client;
using OData.Client.Abstractions;
using System.Net.Http;

namespace Cblx.Dynamics.AspNetCore;

public class DynamicsOptions : ODataClientOptions
{
    public HttpClient? HttpClient { get; internal set; }
    public string? HttpClientName { get; internal set; } = nameof(IODataClient);
}

public class DynamicsOptionsBuilder
{
    public DynamicsOptions Options { get; private set; } = new();
    

    public DynamicsOptionsBuilder UseHttpClient(HttpClient httpClient)
    {
        Options.HttpClient = httpClient;
        Options.HttpClientName = null;
        return this;
    }

    public DynamicsOptionsBuilder UseHttpClient(string httpClientName)
    {
        Options.HttpClient = null;
        Options.HttpClientName = httpClientName;
        return this;
    }
}