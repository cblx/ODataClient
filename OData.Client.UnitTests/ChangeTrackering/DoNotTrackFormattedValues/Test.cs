using Cblx.Dynamics;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.ChangeTrackering.DoNotTrackFormattedValues;

public class Tests
{
    [Fact]
    public void ShouldNotTrackFormattedValues()
    {
        var changeTracker = new JsonChangeTracker<Tb>(new DynamicsCodeMetadataProvider());
        var entity = new Entity { Id = Guid.NewGuid(), IdFormattedValue = "Bla" };
        changeTracker.AttachOrGetCurrent(entity);
        entity.IdFormattedValue = null;
        entity.Anything = "Ble";
        Change change = changeTracker.GetChange(entity.Id);
        change.ChangedProperties.Should().HaveCount(1);
        change.ChangedProperties[0].FieldLogicalName.Should().Be("anything");
    }
}

public class Entity
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName($"id@{DynAnnotations.FormattedValue}")]
    public string IdFormattedValue { get; set; }
    [JsonPropertyName("anything")]
    public string Anything { get; set; }
}

public class Tb
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("anything")]
    public string Anything { get; set; }
}