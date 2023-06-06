namespace Cblx.OData.Client.Tests.SelectAndExpand.NestedArray;

public class Target
{
    public Guid Id { get; set; }

    public TargetChild[] Children { get; set; }
}
