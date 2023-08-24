using Cblx.Dynamics;

namespace Cblx.OData.Client.Tests.ChangeTrackering;

public class Tb
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class ChangeTracker2Tests
{
    private readonly DynamicsCodeMetadataProvider _metadataProvider = new();

    [Fact]
    public void AddingEntity()
    {
        var changeTracker = new JsonChangeTracker<Tb>(_metadataProvider);
        var trackedClass = new TrackedClass();
        changeTracker.Add(trackedClass);
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        Assert.Collection(change.ChangedProperties,
            cp => Assert.Equal("Id", cp.FieldLogicalName)
        );
    }

    [Fact]
    public void AddingAndChangingEntity()
    {
        var changeTracker = new JsonChangeTracker<Tb>(new DynamicsCodeMetadataProvider());
        var trackedClass = new TrackedClass();
        changeTracker.Add(trackedClass);
        trackedClass.Name = "João";
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        // Now we set all fields, even if they are the default value
        Assert.Collection(change.ChangedProperties,
            cp => Assert.Equal("Id", cp.FieldLogicalName),
            cp => Assert.Equal("Name", cp.FieldLogicalName)
        );
    }

    [Fact]
    public void AddingAndChangingInheritedEntity()
    {
        var changeTracker = new JsonChangeTracker<Tb>(new DynamicsCodeMetadataProvider());
        var trackedClass = new TrackedInheritedClass();
        changeTracker.Add(trackedClass);
        trackedClass.Name = "João";
        trackedClass.ChangePrivate();
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        // Now we set all fields, even if they are the default value
        Assert.Collection(change.ChangedProperties,
            cp => Assert.Equal(nameof(TrackedInheritedClass.Id), cp.FieldLogicalName),
            cp => Assert.Equal(nameof(TrackedInheritedClass.Name), cp.FieldLogicalName),
            cp => Assert.Equal(nameof(TrackedInheritedClass.Private), cp.FieldLogicalName)
        );
    }

    [Fact]
    public void AddingAndChangingEntityWithPrivateSetterAndPrivateConstructor()
    {
        var changeTracker = new JsonChangeTracker<Tb>(_metadataProvider);
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.Add(trackedClass);
        trackedClass.ChangeName("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.NotNull(change);
        Assert.Equal(ChangeType.Add, change.ChangeType);
        // Now we set all fields, even if they are the default value
        Assert.Collection(change.ChangedProperties,
            cp => Assert.Equal(nameof(TrackedClassPrivates.Id), cp.FieldLogicalName),
            cp => Assert.Equal(nameof(TrackedClassPrivates.Name), cp.FieldLogicalName)
        );
    }

    [Fact]
    public void UnchangedEntity()
    {
        var changeTracker = new JsonChangeTracker<Tb>(_metadataProvider);
        var trackedClass = new TrackedClass();
        trackedClass.Name = "João";
        changeTracker.AttachOrGetCurrent(trackedClass);
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Null(change);
    }

    [Fact]
    public void ChangeExistingEntity()
    {
        var changeTracker = new JsonChangeTracker<Tb>(_metadataProvider);
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
        var changeTracker = new JsonChangeTracker<Tb>(_metadataProvider);
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.AttachOrGetCurrent(trackedClass);
        trackedClass.ChangeName("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Equal(ChangeType.Update, change.ChangeType);
        Assert.Collection(change.ChangedProperties, cp => Assert.Equal("Name", cp.FieldLogicalName));
    }

    [Fact(DisplayName = "In this new version we don't we fully trust in the JSON template. Só de [ReadOnlyAttribute] does not have effect")]
    public void ReadOnlyAnnotatedPropertiesWillBeBeTracked()
    {
        var changeTracker = new JsonChangeTracker<Tb>(_metadataProvider);
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.AttachOrGetCurrent(trackedClass);
        trackedClass.ChangeName("João");
        trackedClass.ChangeNotTracked("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        change.ChangeType.Should().Be(ChangeType.Update);
        change.ChangedProperties.Should().Satisfy(
            change => change.FieldLogicalName == nameof(TrackedClassPrivates.Name),
            change => change.FieldLogicalName == nameof(TrackedClassPrivates.NotTracked)
        );
    }
}
