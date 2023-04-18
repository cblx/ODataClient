using OData.Client.Abstractions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Cblx.Dynamics;

public class DynamicsEntityType
{
    private readonly Lazy<string> _endpointNameLazy;

    internal DynamicsEntityType() {
        _endpointNameLazy = new Lazy<string>(ResolveEndpointName);
    }

    public required Type ClrType { get; init; }
    internal string? TableName { get; set; }
    internal string? EndpointName { get; set; }

    public string GetTableName() => TableName ?? ClrType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ?? ClrType.Name;
    public string GetEndpointName() => EndpointName ?? _endpointNameLazy.Value;

    private string ResolveEndpointName()
    {
        var endpointName = ClrType!.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint;
        if (endpointName != null) { return endpointName; }
        endpointName = ClrType!.Name;
        if (endpointName.EndsWith("y"))
        {
            endpointName = endpointName[..^1] + "ies";
        }
        else if (endpointName.EndsWith("s"))
        {
            endpointName += "es";
        }
        else
        {
            endpointName += "s";
        }
        return endpointName;
    }

    public virtual bool IsEdmDate(string columnName)
    {
        var propertyInfo = ClrType
           .GetProperties()
               .FirstOrDefault(p =>
                   p.Name == columnName
                   ||
                   p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == columnName
               );
        if (propertyInfo == null)
        {
            return false;
        }
        return IsDate(propertyInfo);
    }

    private static bool IsDate(PropertyInfo propertyInfo)
    {
        var type = propertyInfo.PropertyType;
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(DateTime)
            || type == typeof(DateOnly);
    }
}