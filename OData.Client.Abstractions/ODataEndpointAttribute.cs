namespace OData.Client.Abstractions;
/// <summary>
/// Sets the endpoint of this entity.
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
