#nullable enable
using Cblx.Blocks;

namespace Cblx.OData.Client.Tests.Repositories.Next.StronglyTypedId;

public class ClassificationConfig : FlattenJsonConfiguration<Classification>
{
    public ClassificationConfig()
    {
        HasJsonPropertyName(c => c.Level1, nameof(TbEntity.Classification1));
        HasJsonPropertyName(c => c.Level2, nameof(TbEntity.Classification2));
        HasJsonPropertyName(c => c.Level3, nameof(TbEntity.Classification3));
        HasJsonPropertyName(c => c.Level4, nameof(TbEntity.Classification4));
    }
}