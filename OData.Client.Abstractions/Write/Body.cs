using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace OData.Client.Abstractions.Write
{
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

        static bool IsDate(string propName)
        {
            // Use DateTime? for Edm.Date
            return typeof(T).GetProperty(propName).PropertyType == typeof(DateTime?);
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

        public IDictionary<string, object> ToDictionary() => new ReadOnlyDictionary<string, object>(this.data);
    }
}
