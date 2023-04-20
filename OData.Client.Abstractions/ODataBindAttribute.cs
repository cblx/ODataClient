namespace Cblx.Dynamics;
/// <summary>
/// Sets the navigation field logical name for this FK Property.
/// 
/// This is used for mapping a FK Property to the related nav field without mapping another Property for the Navigation.
/// 
/// This is not necessary in the following conditions:
/// - Having another Property for the Navigation using the Naming Convention (Thing/ThingId)
/// - Having another Property for the Navigation annotated with [ReferentialConstraint(fkFieldLogicalName)]
/// - Annotating this Property with [ForeignKey(nameof(AnotherPorpertyForTheNavigation))]
/// - Mapping with Fluent API
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ODataBindAttribute : Attribute
{
    public string Name { get; private set; }
    public ODataBindAttribute(string name)
    {
        Name = name;
    }
}
