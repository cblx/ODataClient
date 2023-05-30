namespace Cblx.OData.Client.Tests.JsonTemplate.Nested;

public class NestedClass
{
    // Some properties here
    public string Name { get; set; }
    public int Age { get; set; }
    public string Description { get; set; }
    // Some nested properties here
    public NestedClass Nested { get; set; }
}
