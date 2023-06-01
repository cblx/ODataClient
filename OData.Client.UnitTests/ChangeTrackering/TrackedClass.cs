using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests;

public class TrackedClass
{
    [JsonPropertyName(nameof(Id))]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName(nameof(Name))]
    public string Name { get; set; }

    [JsonPropertyName(nameof(Description))]
    public string Description { get; set; }

    public void ChangePrivate() => Private = "yep";

    [Description("Bla")]
    [JsonPropertyName(nameof(Private))]
    public string Private { get; private set; }
}
