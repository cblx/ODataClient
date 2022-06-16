using System;
using System.Text.Json.Serialization;
using OData.Client.Abstractions;

namespace Cblx.Dynamics.FetchXml.Tests;

[ODataEndpoint("other_tables")]
[DynamicsEntity("other_table")]
public class TbOtherTable
{
    [JsonPropertyName("other_tableid")]
    public Guid Id { get; set; }

    [JsonPropertyName("_another_table_value")]
    public Guid AnotherTableId { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}
