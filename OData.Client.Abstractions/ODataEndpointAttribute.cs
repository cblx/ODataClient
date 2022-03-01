namespace OData.Client.Abstractions;
[AttributeUsage(AttributeTargets.Class)]
public class ODataEndpointAttribute : Attribute
{
    public string Endpoint { get; }

    public ODataEndpointAttribute(string endpoint)
    {
        Endpoint = endpoint;
    }
}
