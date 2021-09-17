using System;
using Xunit;

namespace Cblx.OData.Client.Tests
{
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
        public void AddingAndChanginEntity()
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
    }
  
    public class TrackedClass
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
