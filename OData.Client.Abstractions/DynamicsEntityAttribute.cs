namespace Cblx.Dynamics;

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
        RawPropertyName = TrimLookupName(property);
    }

    static string TrimLookupName(string lookupName)
    {
        if (lookupName.StartsWith("_"))
        {
            return lookupName[1..^6];
        }
        return lookupName;
    }
}