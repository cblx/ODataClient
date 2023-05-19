using Cblx.OData.Client.Abstractions.Ids;
using System.Collections;
using System.Text;

namespace Cblx.OData.Client;

internal static class ODataHelpers
{
    public static string? ParseValueAsString(object? o) => ParseValue(o, true);
    
    public static string? ParseValue(object? o)
    {
        if(TryParseValue(o, out var value))
        {
            return value;
        }
        throw new InvalidOperationException($"The value '{o}' could not be parsed in OData Expression");
    }

    public static string? ParseValue(object? o, bool asString)
    {
        if (TryParseValue(o, out var value, asString))
        {
            return value;
        }
        throw new InvalidOperationException($"The value '{o}' could not be parsed in OData Expression");
    }

    public static bool TryParseValue(object? o, out string? stringValue, bool asString = false)
    {
        if (o == null)
        {
            stringValue = "null";
            return true;
        }

        if (o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            o = o.GetType().GetProperty("Value")!.GetValue(o, null);
        }
        // Some functions
        string AsString(string s){
            return asString ? $"'{s}'" : s;
        }
        switch (o)
        {
            case object v when v.GetType().IsEnum:
                stringValue = AsString(Convert.ToInt32(v).ToString());
                return true;
            case string str:
                str = str.Replace("'", "''")
                    .Replace("%", "%25")
                    .Replace("#", "%23")
                    .Replace("+", "%2B")
                    .Replace("/", "%2F")
                    .Replace("?", "%3F")
                    .Replace("&", "%26")
                    ;
                stringValue = $"'{str}'";
                return true;
            case bool b:
                stringValue = AsString(b.ToString().ToLower());
                return true;
            case DateTimeOffset dtoff:
                string strDateTimeOffset = $"{dtoff:O}";
                strDateTimeOffset = strDateTimeOffset
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                stringValue = AsString(strDateTimeOffset);
                return true;
            case DateTime dt:
                string strDateTime = $"{dt:O}";
                strDateTime = strDateTime
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                stringValue = AsString(strDateTime);
                return true;
            case Guid guid:
                stringValue = AsString($"{guid}");
                return true;
            case int i:
                stringValue = AsString($"{i}");
                return true;
            case Id id:
                stringValue = AsString($"{id.Guid}");
                return true;
            case IEnumerable collection:
                var sb = new StringBuilder("%5B");
                bool shouldPlaceComma = false;
                foreach (object obj in collection)
                {
                    if (shouldPlaceComma)
                    {
                        sb.Append(',');
                    }
                    _ = TryParseValue(obj, out string? strItem, asString);
                    sb.Append(strItem);
                    shouldPlaceComma = true;
                }
                sb.Append("%5D");
                stringValue = sb.ToString();
                return true;

        }
        stringValue = null;
        return false;
    }
}
