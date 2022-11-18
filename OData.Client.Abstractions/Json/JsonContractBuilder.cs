using System.Text.Json.Serialization.Metadata;
using System.Reflection;

namespace Cblx.OData.Client.Abstractions.Json;


public static class JsonContractBuilder
{

    public static DefaultJsonTypeInfoResolver CreateContract()
    {
        return new DefaultJsonTypeInfoResolver
        {
            Modifiers = { PrivateConstructorModifier, IncludePropertiesWithPrivateSetModifier }
        };
    }

    private static void PrivateConstructorModifier(JsonTypeInfo jsonTypeInfo)
    {
        if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null)
        {
            jsonTypeInfo.CreateObject = () => Activator.CreateInstance(jsonTypeInfo.Type, true)!;
        }
    }

    private static void IncludePropertiesWithPrivateSetModifier(JsonTypeInfo jsonTypeInfo)
    {
        foreach (var prop in jsonTypeInfo.Properties)
        {
            if (prop.Set is null
                && prop.AttributeProvider is PropertyInfo propertyInfo
                && propertyInfo.GetSetMethod(true) is MethodInfo setMethod)
            {
                prop.Set = (target, value) => setMethod.Invoke(target, new[] { value });
            }
        }
    }
}