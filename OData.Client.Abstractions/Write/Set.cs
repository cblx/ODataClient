using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OData.Client.Abstractions.Write
{
    public class Set<T, TValue> : BodyElement<T> where T: class
    {
        private readonly object value;
        private readonly string propName;
        public Set(Expression<Func<T, TValue>> propExpression, object value)
        {
            this.propName = (propExpression.Body as MemberExpression).Member.Name;
            this.value = value;
        }

        public Set(Expression<Func<T, int?>> propExpression, object value)
        {
            this.propName = (propExpression.Body as MemberExpression).Member.Name;
            this.value = value;
        }

        public Set(string propName, object value)
        {
            this.propName = propName;
            this.value = value;
        }

        public KeyValuePair<string, object> ToKeyValuePair()
        {
            return new KeyValuePair<string, object>(propName, value);
        }
    }
}
