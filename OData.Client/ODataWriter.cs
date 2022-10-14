using Cblx.OData.Client.Abstractions.Ids;
using System.Collections;
using System.Linq.Expressions;
using System.Text;

namespace Cblx.OData.Client;

internal static class ODataHelpers
{
    public static bool TryParseValue(object? o, out string? stringValue)
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
        switch (o)
        {
            case object v when v.GetType().IsEnum:
                stringValue = Convert.ToInt32(v).ToString();
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
                stringValue = b.ToString().ToLower();
                return true;
            case DateTimeOffset dtoff:
                string strDateTimeOffset = $"{dtoff:O}";
                strDateTimeOffset = strDateTimeOffset
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                stringValue = strDateTimeOffset;
                return true;
            case DateTime dt:
                string strDateTime = $"{dt:O}";
                strDateTime = strDateTime
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                stringValue = strDateTime;
                return true;
            case Guid guid:
                stringValue = $"{guid}";
                return true;
            case int i:
                stringValue = $"{i}";
                return true;
            case Id id:
                stringValue = $"{id.Guid}";
                return true;
            case IEnumerable collection:
                var sb = new StringBuilder("%5B");
                bool shouldPlaceComma = false;
                foreach (object obj in collection)
                {
                    if (shouldPlaceComma)
                    {
                        sb.Append(",");
                    }
                    TryParseValue(obj, out string? strItem);
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
