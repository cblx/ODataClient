using Cblx.OData.Client.Abstractions;
using System.Reflection;
using System.Text.Json.Serialization;
namespace OData.Client.Abstractions.Write;
public class Bind<T> : BodyElement<T>
    where T : class
{
    private readonly object foreignId;
    private readonly PropertyInfo navPropInfo;

    public Bind(PropertyInfo navPropInfo, object foreignId)
    {
        this.navPropInfo = navPropInfo;
        this.foreignId = foreignId;
    }

    public Bind(string nav, object foreignId)
    {
        this.navPropInfo = typeof(T).GetProperties().FirstOrDefault(p => p.Name == nav || p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == nav);
        if(this.navPropInfo == null)
        {
            throw new ArgumentOutOfRangeException($"No {nav} property found in {typeof(T).Name} nor a property annotated with JsonPropertyNameAttribute using {nav} as Name");
        }
        this.foreignId = foreignId;
    }

    public KeyValuePair<string, object> ToKeyValuePair()
    {
        var propType = navPropInfo.PropertyType;
        string endpointName = ODataClientHelpers.ResolveEndpointName(propType);
        string navPropName = navPropInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? navPropInfo.Name;
        return new KeyValuePair<string, object>(
                $"{navPropName}@odata.bind",
                $"/{endpointName}({foreignId})"
        );
    }
}
