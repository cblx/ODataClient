using FluentAssertions;
using System;
using System.ComponentModel;
using Xunit;
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
        Assert.Collection(change.ChangedProperties, cp => Assert.Equal("Id", cp.PropertyInfo.Name));
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
            cp => Assert.Equal("Id", cp.PropertyInfo.Name),
            cp => Assert.Equal("Name", cp.PropertyInfo.Name)
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
            cp => Assert.Equal("Id", cp.PropertyInfo.Name),
            cp => Assert.Equal("Name", cp.PropertyInfo.Name)
        );
    }

    [Fact]
    public void UnchangedEntity()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = new TrackedClass();
        trackedClass.Name = "João";
        changeTracker.Attach(trackedClass);
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Null(change);
    }

    [Fact]
    public void ChangeExistingEntity()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = new TrackedClass();
        changeTracker.Attach(trackedClass);
        trackedClass.Name = "João";
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Equal(ChangeType.Update, change.ChangeType);
        Assert.Collection(change.ChangedProperties, cp => Assert.Equal("Name", cp.PropertyInfo.Name));
    }

    [Fact]
    public void ChangeExistingEntityWithPrivateSetterAndPrivateConstructor()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.Attach(trackedClass);
        trackedClass.ChangeName("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        Assert.Equal(ChangeType.Update, change.ChangeType);
        Assert.Collection(change.ChangedProperties, cp => Assert.Equal("Name", cp.PropertyInfo.Name));
    }

    [Fact]
    public void ReadOnlyPropertiesShouldNotBeTracked()
    {
        var changeTracker = new ChangeTracker();
        var trackedClass = (TrackedClassPrivates)Activator.CreateInstance(typeof(TrackedClassPrivates), true);
        changeTracker.Attach(trackedClass);
        trackedClass.ChangeName("João");
        trackedClass.ChangeNotTracked("João");
        Change change = changeTracker.GetChange(trackedClass.Id);
        change.ChangeType.Should().Be(ChangeType.Update);
        change.ChangedProperties.Should().ContainSingle(change => change.PropertyInfo.Name == "Name");
        change.ChangedProperties.Should().NotContain(change => change.PropertyInfo.Name == "NotTracked");
    }
}

public class TrackedClass
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public string Description { get; set; }
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

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; }

    public string Description { get; private set; }

    [ReadOnly(true)]
    public string NotTracked { get; private set; }

    public void ChangeNotTracked(string val) => NotTracked = val;

    public void ChangeName(string name) => Name = name;
}
