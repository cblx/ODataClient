namespace OData.Client.Abstractions;
/// <summary>
/// Sets the endpoint of this entity.
/// This is not necessary when using Dynamics metadata:
/// .AddDynamics(options => options.DownloadMetadataAndConfigure = true)
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ODataEndpointAttribute : Attribute
{
    public string Endpoint { get; }

    public ODataEndpointAttribute(string endpoint)
    {
        Endpoint = endpoint;
    }
}
