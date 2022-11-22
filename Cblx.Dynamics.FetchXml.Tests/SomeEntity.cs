using System;
using System.Text.Json.Serialization;

namespace Cblx.Dynamics.FetchXml.Tests;

public class SomeEntity
{
    private SomeEntity() { }

    [JsonPropertyName("some_tableid")]
    public Guid Id { get; private set; } = Guid.Empty;

    [JsonPropertyName("value")]
    public int Value { get; private set; }

    [JsonPropertyName("some_name")]
    public string? Name { get; private set; }

}