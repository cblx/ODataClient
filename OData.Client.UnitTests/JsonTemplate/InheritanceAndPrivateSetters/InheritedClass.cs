namespace Cblx.OData.Client.Tests.JsonTemplate.InheritanceAndPrivateSetters;

public class InheritedClass : SimpleClass
{
    public string MyProperty { get; private set; }
}