using Cblx.Blocks;
using Cblx.OData.Client.Abstractions.Ids;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OData.Client;

internal static class JsonTemplateHelper
{
    private static readonly ConcurrentDictionary<Type, JsonObject> _templates = new();

    /// <summary>
    /// Tells if a Domain Entity (for Repositories) can use the new mode based on it's JSON template (ex: when using FlattenJsonConverter).
    /// This should be removed in the future, when the new SelectAndExpandParser and the new ChangeTracker are stable.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsJsonBasedDomainEntity(Type type)
    {
        return type.GetCustomAttributes()
            .Any(attr => 
            attr is FlattenRootAttribute
            ||
            attr is JsonConverterAttribute jsonConverterAttribute &&
            (
                (jsonConverterAttribute.ConverterType?.IsGenericType is true && jsonConverterAttribute.ConverterType.GetGenericTypeDefinition() == typeof(FlattenJsonConverter<>))
                ||
                jsonConverterAttribute.ConverterType == typeof(FlattenJsonConverterFactory)
            ));
    }

    public static JsonObject GetTemplate<T>() => GetTemplate(typeof(T));
    public static JsonObject GetTemplate(Type type)
    {
        return _templates.GetOrAdd(type, t =>
        {
            // Create a new instance of the type with all nested properties instantiated, till a depth of 5.
            // The type must have a parameterless constructor, private or public.
            var instance = Activator.CreateInstance(t, true)!;
            // Recursively initialize all nested properties.
            Initialize(instance, 5);
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
            if (prop.PropertyType.IsClass 
                && prop.PropertyType != typeof(string)
                // In the future we may drop this Id support. Struct Ids are better.
                && !prop.PropertyType.IsAssignableTo(typeof(Id))
                )
            {
                var propInstance = Activator.CreateInstance(prop.PropertyType, true)!;
                Initialize(propInstance, remainingLevels - 1);
                prop.SetValue(instance, propInstance);
            }
        }
    }
}
