using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace OData.Client;

internal static class JsonTemplateHelper
{
    private static readonly ConcurrentDictionary<Type, JsonObject> _templates = new();

    public static JsonObject GetTemplate<T>()
    {
        return _templates.GetOrAdd(typeof(T), t =>
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
        if (remainingLevels == 0 || type.IsPrimitive || type.IsEnum || type == typeof(string))
        {
            return;
        }
        foreach (var prop in type.GetProperties())
        {
            if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
            {
                var propInstance = Activator.CreateInstance(prop.PropertyType, true)!;
                Initialize(propInstance, remainingLevels - 1);
                prop.SetValue(instance, propInstance);
            }
        }
    }
}
