using Cblx.OData.Client.Abstractions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Cblx.Dynamics;

/// <summary>
/// Infer types and names from annotations and C# typings
/// </summary>
public class DynamicsCodeMetadataProvider : IDynamicsMetadataProvider
{
    public virtual bool IsEdmDate<TEntity>(string columnName) where TEntity : class
    {
        var propertyInfo = typeof(TEntity)
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
