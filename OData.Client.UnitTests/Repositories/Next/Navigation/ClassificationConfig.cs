#nullable enable
using Cblx.Blocks;

namespace Cblx.OData.Client.Tests.Repositories.Next.Navigation;

public class ClassificationConfig : FlattenJsonConfiguration<Classification>
{
    public ClassificationConfig()
    {
        HasJsonPropertyName(c => c.Level1, nameof(TbEntity.Level1Id));
        HasJsonPropertyName(c => c.Level2, nameof(TbEntity.Level2Id));
        HasJsonPropertyName(c => c.Level3, nameof(TbEntity.Level3Id));
        HasJsonPropertyName(c => c.Level4, nameof(TbEntity.Level4Id));
    }
}