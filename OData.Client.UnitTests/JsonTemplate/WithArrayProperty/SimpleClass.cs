namespace Cblx.OData.Client.Tests.JsonTemplate.WithArrayProperty;

public class SimpleClass
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Description { get; set; }
    public ChildClass[] Children { get; set; }
}

// { "Name": null, "Age": 0, "Description": null, Children: [{ ChildName: null }] 