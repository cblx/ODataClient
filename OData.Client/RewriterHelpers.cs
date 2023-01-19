using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cblx.OData.Client.Abstractions.Ids;

namespace Cblx.Dynamics.OData.Linq;

public static class RewriterHelpers
{
    public static MethodInfo CreateEntityMethod { get; } = typeof(RewriterHelpers)
        .GetMethod(nameof(CreateEntity), BindingFlags.Public | BindingFlags.Static)!;

    public static MethodInfo AuxGetValueMethod { get; } = typeof(RewriterHelpers)
        .GetMethod(nameof(AuxGetValue), BindingFlags.Public | BindingFlags.Static)!;

    public static MethodInfo GetAsArrayMethod { get; } = typeof(RewriterHelpers)
      .GetMethod(nameof(GetAsArray), BindingFlags.Public | BindingFlags.Static)!;

    public static T? CreateEntity<T>(JsonObject jsonObject, string? entityAlias)
    {
        T entity = Activator.CreateInstance<T>();
        foreach (PropertyInfo prop in typeof(T).GetProperties())
        {
            if (!prop.IsCol()) { continue; }
            string fieldName = prop.GetColName();
            var fieldsStack = new Stack<string>(new[] { fieldName });
            prop.SetValue(
                entity,
                AuxGetValueMethod
                    .MakeGenericMethod(prop.PropertyType)
                    .Invoke(null, new object[] { jsonObject, fieldsStack })
            );
        }
        return entity;
    }

    public static JsonObject[] GetAsArray(JsonObject jsonObject, Stack<string> fieldsStack)
    {
        fieldsStack = CloneStack(fieldsStack);
        JsonNode? jsonNode = jsonObject;
        while (fieldsStack.TryPop(out var p))
        {
            if (!jsonObject.ContainsKey(p)) { return Array.Empty<JsonObject>(); }
            jsonNode = jsonNode[p];
            if (jsonNode == null) { return Array.Empty<JsonObject>(); }
            if (jsonNode is JsonObject jsonNodeObject)
            {
                jsonObject = jsonNodeObject;
            }
            if(jsonNode is JsonArray jsonArray)
            {
                return jsonArray.Select(node => node!.AsObject()).ToArray();
            }
        }
        return Array.Empty<JsonObject>();
    }

    public static T? AuxGetValue<T>(JsonObject jsonObject, Stack<string> fieldsStack)
    {
        fieldsStack = CloneStack(fieldsStack);
        JsonNode? jsonNode = jsonObject;
        while (fieldsStack.TryPop(out var p))
        {
            if (!jsonObject.ContainsKey(p)) { return default; }
            jsonNode = jsonNode[p];
            if (jsonNode == null) { return default; }
            if (jsonNode is JsonObject jsonNodeObject)
            {
                jsonObject = jsonNodeObject;
            }
        }

        Type type = typeof(T);
        type = Nullable.GetUnderlyingType(type) ?? type;
        switch (type)
        {
            case var t when t.IsAssignableTo(typeof(Id)):
                Guid? guid = jsonNode.GetValue<Guid?>();
                if (guid == null) { return default; }
                return (T)Activator.CreateInstance(typeof(T), guid)!;
            case var t when t.IsAssignableTo(typeof(Enum)):
                int? val = jsonNode.GetValue<int?>();
                if (val == null) { return default; }
                return (T)Enum.Parse(type, val.ToString()!);
            case var t when t == typeof(DateOnly):
                return jsonNode.Deserialize<T>();
            default: return jsonNode.GetValue<T>();
        }
    }

    static Stack<T> CloneStack<T>(Stack<T> original)
    {
        var arr = new T[original.Count];
        original.CopyTo(arr, 0);
        Array.Reverse(arr);
        return new Stack<T>(arr);
    }

    //public static T? AuxGetValueOld<T>(JsonObject jsonObject, Stack<string> fieldsStack)
    //{

    //    string prop = fieldsStack.Pop();
    //    if (!jsonObject.ContainsKey(prop)) { return default; }
    //    JsonNode? jsonNode = jsonObject[prop];
    //    if (jsonNode == null) { return default; }

    //    Type type = typeof(T);
    //    type = Nullable.GetUnderlyingType(type) ?? type;
    //    if (type.IsAssignableTo(typeof(Id)))
    //    {
    //        Guid? guid = jsonNode.GetValue<Guid?>();
    //        if (guid == null) { return default; }
    //        return (T)Activator.CreateInstance(typeof(T), guid)!;
    //    }else if (type.IsAssignableTo(typeof(Enum)))
    //    {
    //        int? val = jsonNode.GetValue<int?>();
    //        if(val == null){ return default; }
    //        return (T)Enum.Parse(type, val.ToString()!);
    //    }
    //    return jsonNode.GetValue<T>();
    //}

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