namespace Cblx.OData.Client.Tests.SelectAndExpand.Nested;

public class Source
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public SourceChild Child { get; set; }
}
