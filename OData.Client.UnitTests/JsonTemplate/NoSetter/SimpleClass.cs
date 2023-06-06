namespace Cblx.OData.Client.Tests.JsonTemplate.NoSetter;

public class SimpleClass
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Description { get; set; }
    public string Data => $"{Name}, {Age}";
}