#nullable enable
using Cblx.Blocks;
using Cblx.Dynamics;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.Repositories.Next.StronglyTypedId;
[JsonConverter(typeof(FlattenJsonConverter<DomainEntity>))]
[UseNewJsonTemplateMode]
public class DomainEntity
{
    public TbEntityId Id { get; set; } = TbEntityId.NewId();

    [FlattenJsonProperty<ClassificationConfig>]
    public Classification Classification { get; set; } = new Classification();
}
