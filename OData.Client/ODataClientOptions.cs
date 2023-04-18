using OData.Client.Abstractions;

namespace OData.Client;
public class DynamicsOptions
{
    public string HttpClientName { get; set; } = nameof(IODataClient);
    public bool ShowLog { get; set; } = false;
    public bool ReadResponsesAsString { get; set; } = false;
}
