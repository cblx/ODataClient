﻿#nullable enable
using Cblx.Blocks;
using Cblx.OData.Client.Abstractions.Ids;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.Repositories.Next.Simple;
[JsonConverter(typeof(FlattenJsonConverter<DomainEntity>))]
public class DomainEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Flatten<ClassificationConfig>]
    public Classification Classification { get; set; } = new Classification();
}
