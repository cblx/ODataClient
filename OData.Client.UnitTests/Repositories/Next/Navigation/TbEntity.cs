#nullable enable
namespace Cblx.OData.Client.Tests.Repositories.Next.Navigation;

public class TbEntity
{
    public Guid Id { get; set; }

    public Guid? Level1Id { get; set; }
    public Guid? Level2Id { get; set; }
    public Guid? Level3Id { get; set; }
    public Guid? Level4Id { get; set; }
    public object? Level1 { get; set; }
    public object? Level2 { get; set; }
    public object? Level3 { get; set; }
    public object? Level4 { get; set; }
}
