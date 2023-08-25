namespace Cblx.OData.Client.Tests.JsonTemplate.InheritanceAndPrivateSetters;

public class SimpleClass
{
    public string Name { get; private set; }
    public int Age { get; private set; }
    public string Description { get; private set; }

    public Nested Nested { get; private set; }
}


public record Nested
{
    public string Something { get; set; }
}