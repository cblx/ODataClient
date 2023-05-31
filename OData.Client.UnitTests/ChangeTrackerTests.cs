using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests;
public class ChangeTrackerTests
{
    [Fact]
    public void AddingEntity()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = new TrackedClass();
        changeTracker.Add(trackedClass);
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        Assert.Collection(change.ChangedProperties, cp => Assert.Equal("Id", cp.FieldLogicalName));
    }

    [Fact]
    public void AddingAndChangingEntity()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = new TrackedClass();
        changeTracker.Add(trackedClass);
        trackedClass.Name = "João";
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        Assert.Collection(change.ChangedProperties,
            cp => Assert.Equal("Id", cp.FieldLogicalName),
            cp => Assert.Equal("Name", cp.FieldLogicalName)
        );
    }

    [Fact]
    public void AddingAndChangingInheritedEntity()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = new TrackedInheritedClass();
        changeTracker.Add(trackedClass);
        trackedClass.Name = "João";
        trackedClass.ChangePrivate();
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        Assert.Collection(change.ChangedProperties,
            cp => Assert.Equal("Id", cp.FieldLogicalName),
            cp => Assert.Equal("Name", cp.FieldLogicalName),
            cp => Assert.Equal("Private", cp.FieldLogicalName)
        );
    }

    [Fact]
    public void AddingAndChangingEntityWithPrivateSetterAndPrivateConstructor()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.Add(trackedClass);
        trackedClass.ChangeName("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        Assert.Collection(change.ChangedProperties,
            cp => Assert.Equal("Id", cp.FieldLogicalName),
            cp => Assert.Equal("Name", cp.FieldLogicalName)
        );
    }

    [Fact]
    public void UnchangedEntity()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = new TrackedClass();
        trackedClass.Name = "João";
        changeTracker.AttachOrGetCurrent(trackedClass);
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Null(change);
    }

    [Fact]
    public void ChangeExistingEntity()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = new TrackedClass();
        changeTracker.AttachOrGetCurrent(trackedClass);
        trackedClass.Name = "João";
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Equal(ChangeType.Update, change.ChangeType);
        Assert.Collection(change.ChangedProperties, cp => Assert.Equal("Name", cp.FieldLogicalName));
    }

    [Fact]
    public void ChangeExistingEntityWithPrivateSetterAndPrivateConstructor()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.AttachOrGetCurrent(trackedClass);
        trackedClass.ChangeName("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Equal(ChangeType.Update, change.ChangeType);
        Assert.Collection(change.ChangedProperties, cp => Assert.Equal("Name", cp.FieldLogicalName));
    }

    [Fact]
    public void ReadOnlyPropertiesShouldNotBeTracked()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.AttachOrGetCurrent(trackedClass);
        trackedClass.ChangeName("João");
        trackedClass.ChangeNotTracked("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        change.ChangeType.Should().Be(ChangeType.Update);
        change.ChangedProperties.Should().ContainSingle(change => change.FieldLogicalName == "Name");
        change.ChangedProperties.Should().NotContain(change => change.FieldLogicalName == "NotTracked");
    }
}

public class TrackedInheritedClass : TrackedClass
{

}

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

#pragma warning disable S3453 // Classes should not have only "private" constructors
public partial class TrackedClassPrivates
#pragma warning restore S3453 // Classes should not have only "private" constructors
{

}

public partial class TrackedClassPrivates
{
    private TrackedClassPrivates()
    {

    }

    [JsonPropertyName(nameof(Id))]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [JsonPropertyName(nameof(Name))]
    public string Name { get; private set; }

    [JsonPropertyName(nameof(Description))]
    public string Description { get; private set; }

    [ReadOnly(true)]
    public string NotTracked { get; private set; }

    public void ChangeNotTracked(string val) => NotTracked = val;

    public void ChangeName(string name) => Name = name;
}
