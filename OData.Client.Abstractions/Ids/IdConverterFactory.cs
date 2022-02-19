using System.Text.Json;
using System.Text.Json.Serialization;
namespace Cblx.OData.Client.Abstractions.Ids;
public class IdConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        bool canConvert = typeToConvert.BaseType == typeof(Id);
        return canConvert;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) 
        => (Activator.CreateInstance(typeof(IdConverterGeneric<>).MakeGenericType(typeToConvert)) as JsonConverter)!;
}
