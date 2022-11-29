using System.Reflection;
using System.Text.Json.Nodes;
using Cblx.OData.Client.Abstractions.Ids;

namespace Cblx.Dynamics.FetchXml.Linq;

public static class RewriterHelpers
{
    public static MethodInfo CreateEntityMethod { get; } = typeof(RewriterHelpers)
        .GetMethod(nameof(CreateEntity), BindingFlags.Public | BindingFlags.Static)!;  

    public static MethodInfo AuxGetValueMethod { get; } = typeof(RewriterHelpers)
        .GetMethod(nameof(AuxGetValue), BindingFlags.Public | BindingFlags.Static)!;
    
    public static T? CreateEntity<T>(JsonObject jsonObject/*, string? entityAlias*/)
    {
        T entity = (T)Activator.CreateInstance(typeof(T), true)!;
        string firstKey = jsonObject
            // avoid fields like @odata.etag
            .Where(field => !field.Key.StartsWith("@"))
            .First().Key;
        string entityAlias = firstKey.Contains('.') ? firstKey.Split('.').First() : "";
        foreach (PropertyInfo prop in typeof(T).GetProperties())
        {
            if(!prop.IsCol()){ continue; }
            prop.SetValue(
                entity, 
                AuxGetValueMethod
                    .MakeGenericMethod(prop.PropertyType)
                    .Invoke(null, new object[]
                    {
                        jsonObject,
                        string.IsNullOrEmpty(entityAlias) ? prop.Name : $"{entityAlias}.{prop.Name}"
                    })
            );            
        }
        return entity;
    }
    
  
    public static T? AuxGetValue<T>(JsonObject jsonObject, string prop)
    {
        if (!jsonObject.ContainsKey(prop)) { return default; }
        JsonNode? jsonNode = jsonObject[prop];
        if (jsonNode == null) { return default; }

        Type type = typeof(T);
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsAssignableTo(typeof(Id)))
        {
            Guid? guid = jsonNode.GetValue<Guid?>();
            if (guid == null) { return default; }
            return (T)Activator.CreateInstance(typeof(T), guid)!;
        }else if (type.IsAssignableTo(typeof(Enum)))
        {
            int? val = jsonNode.GetValue<int?>();
            if(val == null){ return default; }
            return (T)Enum.Parse(type, val.ToString()!);
        }
        return jsonNode.GetValue<T>();
    }

    public static TEnum? ToNullableEnum<TEnum>(int? value)
        where TEnum : struct, Enum
    {
        if (value == null) { return default; }
        return (TEnum?)Enum.Parse(typeof(TEnum), value.ToString()!);
    }

    public static TEnum ToEnum<TEnum>(int value)
        where TEnum : Enum
    {
        return (TEnum)Enum.Parse(typeof(TEnum), value.ToString()!);
    }

}