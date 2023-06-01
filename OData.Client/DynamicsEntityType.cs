using Cblx.Dynamics.OData.Linq;
using OData.Client.Abstractions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Cblx.Dynamics;

public class DynamicsEntityType
{
    private readonly Lazy<string> _endpointNameLazy;
    private readonly Dictionary<string, DynamicsEntityProperty> _properties = new();

    internal DynamicsEntityType(Type clrType) {
        ClrType = clrType;
        _endpointNameLazy = new Lazy<string>(ResolveEndpointName);
        foreach(var prop in ClrType!.GetProperties())
        {
            InitDefaultProperty(prop.Name);
        }
    }

    public Type ClrType { get; init; }
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

    internal DynamicsEntityProperty GetProperty(string name)
    {
        return _properties[name];
    }

    private void InitDefaultProperty(string name)
    {
        if (_properties.ContainsKey(name)) { return; }
        var thisPropInfo = ClrType.GetProperty(name) 
            ?? throw new InvalidOperationException($"Property {name} does not exists in type {ClrType.Name}");
        var logicalName = thisPropInfo.GetColName();
        var relatedLogicalLookupName = thisPropInfo.GetCustomAttribute<ReferentialConstraintAttribute>()?.Property 
                ?? 
                ClrType
                    .GetProperties()
                    .FirstOrDefault(p => 
                    p.Name == $"{name}Id" 
                    // This mode will not support this Attribute
                    // This should be used only for the old mode repository entity
                    //|| p.GetCustomAttribute<ODataBindAttribute>()?.Name == logicalName
                    )?.GetColName();
        _properties[name] = new DynamicsEntityProperty
        {
            LogicalName = logicalName,
            RelatedLogicalLookupName = relatedLogicalLookupName,
            RelatedLogicalNavigationName = FindLogicalNavigationNameByConventionOrAnnotations(logicalName)
        };
    }

    public bool IsEdmDate(string columnName)
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

    internal DynamicsEntityProperty? FindPropertyByLogicalName(string foreignKeyLogicalName) 
        => _properties.Values.FirstOrDefault(p => p.LogicalName == foreignKeyLogicalName);

    private string? FindLogicalNavigationNameByConventionOrAnnotations(string lookupLogicalName)
    {
        var mappedLookupPropertyInfo = ClrType.GetProperties().FirstOrDefault(p => p.GetColName() == lookupLogicalName);
        if (mappedLookupPropertyInfo == null) { return null; }
        
        // find the mapped nav property and use [ReferentialConstraint] or Naming Convention
        var mappedNavigationPropertyInfo = ClrType.GetProperties().FirstOrDefault(
            p =>
                // ReferentialConstraint
                p.GetCustomAttribute<ReferentialConstraintAttribute>()?.Property == lookupLogicalName
                ||
                // Naming convention
                (mappedLookupPropertyInfo.Name.EndsWith("Id") && p.Name == mappedLookupPropertyInfo.Name[..^2])
        );
        if (mappedNavigationPropertyInfo == null) { return null; }
        return mappedNavigationPropertyInfo.GetColName();
    }
}
