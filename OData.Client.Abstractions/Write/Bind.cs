using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OData.Client.Abstractions.Write
{
    public class Bind<T> : BodyElement<T>
        where T : class
    {
        private readonly object foreignId;
        private readonly PropertyInfo navPropInfo;

        public Bind(PropertyInfo navPropInfo, object foreignId)
        {
            this.navPropInfo = navPropInfo;
            this.foreignId = foreignId;
        }

        public Bind(string nav, object foreignId)
        {
            this.navPropInfo = typeof(T).GetProperty(nav);
            this.foreignId = foreignId;
        }

        public KeyValuePair<string, object> ToKeyValuePair()
        {
            string endpointName = navPropInfo.PropertyType.Name;
            if (endpointName.EndsWith("s"))
            {
                endpointName += "es";
            }
            else
            {
                endpointName += "s";
            }
            return new KeyValuePair<string, object>(
                    $"{navPropInfo.Name}@odata.bind",
                    $"/{endpointName}({foreignId})"
            );
        }
    }
}
