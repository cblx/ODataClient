using System.Reflection;
using OData.Client.Abstractions;

namespace Cblx.Dynamics.OData.Linq;

public static class TypeExtensions
{

    public static string GetEndpointName(this Type entityType)
    {
        return entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ??
                           throw new Exception($"No endpoint found for Entity {entityType.Name}");
    }
}
