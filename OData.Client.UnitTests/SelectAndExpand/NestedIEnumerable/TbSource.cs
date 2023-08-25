namespace Cblx.OData.Client.Tests.SelectAndExpand.NestedIEnumerable;

public class TbSource
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public TbSourceChild[] Children { get; set; }
}
