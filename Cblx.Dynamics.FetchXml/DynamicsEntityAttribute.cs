using Cblx.Dynamics.FetchXml.Linq;

namespace Cblx.Dynamics.FetchXml;

[AttributeUsage(AttributeTargets.Class)]
public class DynamicsEntityAttribute : Attribute
{
    public string Name { get; }
    public DynamicsEntityAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ReferentialConstraintAttribute : Attribute
{
    public string Property { get; }
    public string RawPropertyName { get; }
    public ReferentialConstraintAttribute(string property)
    {
        Property = property;
        RawPropertyName = Helpers.TrimLookupName(property);
    }
}