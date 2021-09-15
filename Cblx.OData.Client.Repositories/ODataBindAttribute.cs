using System;

namespace Cblx.OData.Client
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ODataBindAttribute : Attribute {
        public string Name { get; private set; }
        public ODataBindAttribute(string name)
        {
            Name = name;
        }
    }
}
