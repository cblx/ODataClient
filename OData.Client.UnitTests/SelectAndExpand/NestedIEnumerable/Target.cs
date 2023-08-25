namespace Cblx.OData.Client.Tests.SelectAndExpand.NestedIEnumerable;

public class Target
{
    public Guid Id { get; set; }

    public IEnumerable<TargetChild> Children { get; set; }
}
