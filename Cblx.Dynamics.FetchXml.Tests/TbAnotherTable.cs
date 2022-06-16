using System;
using System.Text.Json.Serialization;
using OData.Client.Abstractions;

namespace Cblx.Dynamics.FetchXml.Tests;

[ODataEndpoint("another_tables")]
[DynamicsEntity("another_table")]
public class TbAnotherTable
{
    [JsonPropertyName("another_tableid")]
    public Guid Id { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}
