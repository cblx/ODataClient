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
