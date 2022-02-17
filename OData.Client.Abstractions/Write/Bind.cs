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
        this.foreignId = foreignId;
    }

    public KeyValuePair<string, object> ToKeyValuePair()
    {
        var propType = navPropInfo.PropertyType;
        string endpointName = propType.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? propType.Name;
        if (endpointName.EndsWith("s"))
        {
            endpointName += "es";
        }
        else
        {
            endpointName += "s";
        }
        return new KeyValuePair<string, object>(
                $"{navPropInfo.Name}@odata.bind",
                $"/{endpointName}({foreignId})"
        );
    }
}
