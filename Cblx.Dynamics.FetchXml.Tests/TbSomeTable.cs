using System;
using System.Text.Json.Serialization;
using OData.Client.Abstractions;

namespace Cblx.Dynamics.FetchXml.Tests;

[ODataEndpoint("some_tables")]
[DynamicsEntity("some_table")]
public class TbSomeTable
{
    [JsonPropertyName("some_tableid")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("_other_table_value")]
    public Guid OtherTableId { get; set; }

    [JsonPropertyName("_another_table_value")]
    public Guid AnotherTableId { get; set; }

    [JsonPropertyName("_yet_other_table_value")]
    public Guid? YetOtherTableId { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("some_name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public SomeStatusEnum? Status { get; set; }

    [ReferentialConstraint("_other_table_value")]
    [JsonPropertyName("other_table")]
    public TbOtherTable? OtherTable { get; set; }

    [ReferentialConstraint("_yet_other_table_value")]
    [JsonPropertyName("yet_other_table")]
    public TbOtherTable? YetOtherTable { get; set; }

    [JsonPropertyName("date_only")]
    public DateOnly? DateOnly { get; set; }
}
