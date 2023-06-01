namespace Cblx.OData.Client.Tests;
public class ChangeTrackerTests
{
    [Fact]
    public void AddingEntity()
    {
        var changeTracker = new ClassicChangeTracker();
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
        var changeTracker = new ClassicChangeTracker();
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
        var changeTracker = new ClassicChangeTracker();
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
        var changeTracker = new ClassicChangeTracker();
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
        var changeTracker = new ClassicChangeTracker();
        var trackedClass = new TrackedClass();
        trackedClass.Name = "João";
        changeTracker.AttachOrGetCurrent(trackedClass);
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Null(change);
    }

    [Fact]
    public void ChangeExistingEntity()
    {
        var changeTracker = new ClassicChangeTracker();
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
        var changeTracker = new ClassicChangeTracker();
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
        var changeTracker = new ClassicChangeTracker();
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
