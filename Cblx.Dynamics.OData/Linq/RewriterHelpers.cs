using System.Reflection;
using System.Text.Json.Nodes;
using Cblx.OData.Client.Abstractions.Ids;

namespace Cblx.Dynamics.OData.Linq;

public static class RewriterHelpers
{
    public static MethodInfo CreateEntityMethod { get; } = typeof(RewriterHelpers)
        .GetMethod(nameof(CreateEntity), BindingFlags.Public | BindingFlags.Static)!;  

    public static MethodInfo AuxGetValueMethod { get; } = typeof(RewriterHelpers)
        .GetMethod(nameof(AuxGetValue), BindingFlags.Public | BindingFlags.Static)!;
    
    public static T? CreateEntity<T>(JsonObject jsonObject, string? entityAlias)
    {
        T entity = Activator.CreateInstance<T>();
        foreach (PropertyInfo prop in typeof(T).GetProperties())
        {
            if(!prop.IsCol()){ continue; }
            string fieldName = prop.GetColName();
            prop.SetValue(
                entity, 
                AuxGetValueMethod
                    .MakeGenericMethod(prop.PropertyType)
                    .Invoke(null, new object[] { jsonObject, fieldName })
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