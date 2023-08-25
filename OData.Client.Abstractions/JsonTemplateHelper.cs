using Cblx.Dynamics;
using Cblx.OData.Client.Abstractions.Ids;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OData.Client;

internal static class JsonTemplateHelper
{
    private static readonly ConcurrentDictionary<string, JsonObject> _templates = new();

    /// <summary>
    /// Tells if a Domain Entity (for Repositories) can use the new mode based on it's JSON template (ex: when using FlattenJsonConverter).
    /// This should be removed in the future, when the new SelectAndExpandParser and the new ChangeTracker are stable.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsJsonBasedDomainEntity(Type type)
    {
        return type
            .GetCustomAttributes()
            .Any(attr => attr is UseNewJsonTemplateModeAttribute);
    }

    public static JsonObject GetTemplate<T>(int depth = 1) => GetTemplate(typeof(T), depth);
    public static JsonObject GetTemplate(Type type, int depth = 1)
    {
        string key = $"{type.FullName}_{depth}";
        return _templates.GetOrAdd(key, t =>
        {
            // Create a new instance of the type with all nested properties instantiated, till a depth of 5.
            // The type must have a parameterless constructor, private or public.
            var instance = Activator.CreateInstance(type, true)!;
            // Recursively initialize all nested properties.
            Initialize(instance, depth);
            var json = JsonSerializer.Serialize(instance);
            return JsonSerializer.Deserialize<JsonObject>(json)!;
        });
    }

    private static void Initialize(object instance, int remainingLevels)
    {
        var type = instance.GetType();
        if (remainingLevels == 0 
            || type.IsPrimitive 
            || type.IsEnum 
            || type == typeof(string)
            // In the future we may drop this Id support.
            // Ideally we should not have a Strongly Typed Id in this Library
            // We may offer an struct Id in a future version.
            // So we can drop this "record" Id support and then remove it in the future.
            || type.IsAssignableTo(typeof(Id))
            )
        {
            return;
        }
        foreach (var prop in type.GetProperties())
        {
            if(!prop.DeclaringType!.GetProperty(prop.Name)!.CanWrite)
            {
                continue;
            }
            if (prop.PropertyType.IsArray || (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var elementType = prop.PropertyType.IsArray ? 
                    prop.PropertyType.GetElementType()! :
                    prop.PropertyType.GetGenericArguments()[0]!;
                if (IsComplexType(elementType))
                {
                    var arrayPropInstance = Array.CreateInstance(elementType, 1);
                    var itemInstance = Activator.CreateInstance(elementType, true)!;
                    Initialize(itemInstance, remainingLevels - 1);
                    arrayPropInstance.SetValue(itemInstance, 0);
                    prop.DeclaringType.GetProperty(prop.Name)!.SetValue(instance, arrayPropInstance);
                }
            }
            else if (IsComplexType(prop.PropertyType))
            {
                var propInstance = Activator.CreateInstance(prop.PropertyType, true)!;
                Initialize(propInstance, remainingLevels - 1);
                prop.DeclaringType.GetProperty(prop.Name)!.SetValue(instance, propInstance);
            }
        }
    }

    private static bool IsComplexType(Type t) => t.IsClass
                    && t != typeof(string)
                    // In the future we may drop this Id support. Struct Ids are better.
                    && !t.IsAssignableTo(typeof(Id));
}
