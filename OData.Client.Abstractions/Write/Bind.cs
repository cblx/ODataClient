using Cblx.OData.Client.Abstractions;
using System.Reflection;
using System.Text.Json.Serialization;
namespace OData.Client.Abstractions.Write;
internal class Bind<T> : BodyElement<T>
    where T : class
{
    private readonly object _foreignId;
    private readonly IDynamicsMetadataProvider _metadataProvider;
    private readonly PropertyInfo _navPropInfo;

    public Bind(IDynamicsMetadataProvider metadataProvider, PropertyInfo navPropInfo, object foreignId)
    {
        _metadataProvider = metadataProvider;
        _navPropInfo = navPropInfo;
        _foreignId = foreignId;
    }

    public Bind(IDynamicsMetadataProvider metadataProvider, string nav, object foreignId)
    {
        _navPropInfo = typeof(T).GetProperties().FirstOrDefault(p => p.Name == nav || p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == nav);
        if(_navPropInfo == null)
        {
            throw new ArgumentOutOfRangeException($"No {nav} property found in {typeof(T).Name} nor a property annotated with JsonPropertyNameAttribute using {nav} as Name");
        }
        _metadataProvider = metadataProvider;
        _foreignId = foreignId;
    }

    public KeyValuePair<string, object> ToKeyValuePair()
    {
        var propType = _navPropInfo.PropertyType;
        string endpointName = _metadataProvider.GetEndpoint(propType);
        string navPropName = _navPropInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? _navPropInfo.Name;
        return new KeyValuePair<string, object>(
                $"{navPropName}@odata.bind",
                $"/{endpointName}({_foreignId})"
        );
    }
}
