#nullable enable
using Cblx.Blocks;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.Repositories.Next.StronglyTypedId;
[JsonConverter(typeof(FlattenJsonConverter<DomainEntity>))]
public class DomainEntity
{
    public TbEntityId Id { get; set; } = TbEntityId.NewId();

    [Flatten<ClassificationConfig>]
    public Classification Classification { get; set; } = new Classification();
}
