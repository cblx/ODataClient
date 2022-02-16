namespace OData.Client.Abstractions;
[AttributeUsage(AttributeTargets.Class)]
public class ODataTableAttribute : Attribute
{
    public string Endpoint { get; }

    public ODataTableAttribute(string endpoint)
    {
        Endpoint = endpoint;
    }
}
