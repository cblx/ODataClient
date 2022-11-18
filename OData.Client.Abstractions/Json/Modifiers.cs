//using System.Reflection;
//using System.Text.Json.Serialization.Metadata;

//namespace Cblx.OData.Client.Abstractions.Json;

//internal static class Modifiers
//{
//    public static void AddPrivateFieldsModifier(JsonTypeInfo jsonTypeInfo)
//    {
//        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object) { return; }
//        foreach (FieldInfo field in jsonTypeInfo.Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
//        {
//            JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
//            jsonPropertyInfo.Get = field.GetValue;
//            jsonPropertyInfo.Set = field.SetValue;

//            jsonTypeInfo.Properties.Add(jsonPropertyInfo);
//        }
//    }
//}
