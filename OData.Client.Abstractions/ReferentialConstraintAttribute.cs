namespace Cblx.Dynamics;
/// <summary>
/// Sets the rerential constraint for a nav property
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReferentialConstraintAttribute : Attribute
{
    public string Property { get; }
    public ReferentialConstraintAttribute(string property)
    {
        Property = property;
    }
}