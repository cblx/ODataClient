#nullable enable
using Cblx.Blocks;
using Cblx.Dynamics;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.Repositories.Next.Navigation;
[JsonConverter(typeof(FlattenJsonConverter<DomainEntity>))]
[UseNewJsonTemplateMode]
public class DomainEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [FlattenJsonProperty<ClassificationConfig>]
    public Classification Classification { get; set; } = new Classification();
}
