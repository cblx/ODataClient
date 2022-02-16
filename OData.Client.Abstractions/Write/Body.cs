using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
namespace OData.Client.Abstractions.Write;
public class Body<T> where T : class
{
    readonly Dictionary<string, object> data = new Dictionary<string, object>();

    public Body<T> Set(string propName, object value)
    {
        if (IsDate(propName))
        {
            value = ToDateFormat(value);
        }
        data.Add(propName, value);
        return this;
    }

    public Body<T> Set<TValue>(Expression<Func<T, TValue>> prop, TValue value)
    {
        var kvp = new Set<T, TValue>(prop, value).ToKeyValuePair();
        object v = kvp.Value;
        if (IsDate(kvp.Key))
        {
            v = ToDateFormat(v);
        }
        data.Add(kvp.Key, v);
        return this;
    }

    public Body<T> Set<TValue>(string propName, TValue value)
    {
        var kvp = new Set<T, TValue>(propName, value).ToKeyValuePair();
        data.Add(kvp.Key, kvp.Value);
        return this;
    }

    public Body<T> Bind(string nav, Guid id)
    {
        var kvp = new Bind<T>(nav, id).ToKeyValuePair();
        data.Add(kvp.Key, kvp.Value);
        return this;
    }

    public Body<T> Bind<TNav>(Expression<Func<T, TNav>> prop, Guid id) where TNav: class
    {
        string nav = (prop.Body as MemberExpression).Member.Name;
        return Bind(nav, id);
    }

    static bool IsDate(string propName)
    {
        // Use DateTime? for Edm.Date
        PropertyInfo propertyInfo = typeof(T).GetProperty(propName);
        if(propertyInfo == null) { throw new ArgumentOutOfRangeException($"{propName} not found in {typeof(T).Name}"); }
        return propertyInfo.PropertyType == typeof(DateTime?);
    }

    static object ToDateFormat(object o)
    {
        if (o == null) { return o; }
        if (o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            o = o.GetType().GetProperty("Value").GetValue(o);
        }

        if (o is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        if (o is DateTimeOffset dtoff)
        {
            return dtoff.ToString("yyyy-MM-dd");
        }

        return o;
    }

    

    public IDictionary<string, object> ToDictionary() => new ReadOnlyDictionary<string, object>(this.data);
}
