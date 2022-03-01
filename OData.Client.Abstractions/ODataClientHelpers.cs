using OData.Client.Abstractions;
using System.Reflection;

namespace Cblx.OData.Client.Abstractions;
public static class ODataClientHelpers
{
    public static string ResolveEndpointName<T>() => ResolveEndpointName(typeof(T));
    
    public static string ResolveEndpointName(Type type)
    {
        string endpointName = type.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint;
        if (endpointName != null) { return endpointName; }
        endpointName = type.Name;
        if (endpointName.EndsWith("s"))
        {
            endpointName += "es";
        }
        else
        {
            endpointName += "s";
        }
        return endpointName;
    }
}
