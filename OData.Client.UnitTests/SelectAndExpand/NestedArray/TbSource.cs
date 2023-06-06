namespace Cblx.OData.Client.Tests.SelectAndExpand.NestedArray;

public class TbSource
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public TbSourceChild[] Children { get; set; }
}
