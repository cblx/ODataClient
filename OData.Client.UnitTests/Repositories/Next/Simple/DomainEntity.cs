#nullable enable
using Cblx.Blocks;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.Repositories.Next;
[JsonConverter(typeof(FlattenJsonConverter<DomainEntity>))]
public class DomainEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Flatten<ClassificationConfig>]
    public Classification Classification { get; set; } = new Classification();
}
