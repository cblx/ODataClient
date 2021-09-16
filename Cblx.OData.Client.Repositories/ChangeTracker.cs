using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client
{
    public class ChangeTracker
    {
        public static ICollection<Func<Type, bool>> CanTrack { get; } = new HashSet<Func<Type, bool>>();

        readonly Dictionary<Guid, string> states = new Dictionary<Guid, string>();
        readonly Dictionary<Guid, object> entities = new Dictionary<Guid, object>();
        readonly HashSet<Guid> markedForRemove = new HashSet<Guid>();
        readonly JsonSerializerOptions options = new JsonSerializerOptions();

        public ChangeTracker()
        {
        }

        public void Add(object o)
        {
            Guid? id = GetId(o);
            if (id == null || id == Guid.Empty)
            {
                PropertyInfo idProp = o.GetType().GetProperty("Id");
                if (idProp.PropertyType == typeof(Guid))
                {
                    idProp.SetValue(o, Guid.NewGuid());
                }
                else
                {
                    idProp.SetValue(o, Activator.CreateInstance(idProp.PropertyType, Guid.NewGuid()));
                }
                id = GetId(o);
            }
            entities.Add(id.Value, o);
        }

        public void Remove(object o)
        {
            Guid? id = GetId(o);
            markedForRemove.Add(id.Value);
        }

        public void Attach(object o)
        {
            Guid? id = GetId(o);
            states.Add(id.Value, JsonSerializer.Serialize(o, o.GetType(), options));
            entities.Add(id.Value, o);
        }

        public void AttachRange(IEnumerable<object> items)
        {
            foreach (var o in items)
            {
                Attach(o);
            }
        }

        internal Guid? GetId(object entity)
        {
            object val = entity.GetType().GetProperty("Id").GetValue(entity);
            if(val == null) { return null; }
            if(val is Guid guid) { return guid; }
            return JsonSerializer.Deserialize<Guid>(JsonSerializer.Serialize(val));
        }

        internal void AcceptChange(Change change)
        {
            if (change.ChangeType == ChangeType.Remove)
            {
                states.Remove(change.Id);
                entities.Remove(change.Id);
                markedForRemove.Remove(change.Id);
            }
            else
            {
                states[change.Id] = change.NewState;
            }
        }

        internal Change GetChange(Guid id)
        {
            IEnumerable<Change> changes = GetChanges();
            Change change = changes.FirstOrDefault(c => c.Id.Equals(id));
            return change;
        }

        internal IEnumerable<Change> GetChanges()
        {
            List<Change> changes = new List<Change>();
            foreach (var kvp in entities)
            {
                object currentVersion = kvp.Value;
                Change change = new();
                change.Id = kvp.Key;
                change.Entity = currentVersion;

                if (markedForRemove.Contains(change.Id))
                {
                    change.ChangeType = ChangeType.Remove;
                    changes.Add(change);
                    continue;
                }

                Type type = currentVersion.GetType();
                string currentState = JsonSerializer.Serialize(currentVersion, type, options);
                change.NewState = currentState;
                change.ChangeType = states.ContainsKey(change.Id) ? ChangeType.Update : ChangeType.Add;
                if (change.ChangeType == ChangeType.Add)
                {
                    foreach (var prop in type.GetProperties())
                    {
                        if (!IsTrackable(prop)) { continue; }
                        object currentValue = prop.GetValue(currentVersion);
                        object defaultValue = GetDefault(prop.PropertyType);
                        if (defaultValue == null && currentValue == null) { continue; }
                        if (Equals(defaultValue, currentValue)) { continue; }
                        change.ChangedProperties.Add(new ChangedProperty
                        {
                            NewValue = currentValue,
                            OldValue = defaultValue,
                            PropertyInfo = prop
                        });
                    }
                    if (change.ChangedProperties.Any())
                    {
                        changes.Add(change);
                    }
                }
                if (change.ChangeType == ChangeType.Update)
                {
                    string oldState = states[kvp.Key];
                    if (oldState != currentState)
                    {
                        object oldVersion = JsonSerializer.Deserialize(oldState, type, options);
                        foreach (var prop in type.GetProperties())
                        {
                            if (!IsTrackable(prop)) { continue; }
                            object oldValue = prop.GetValue(oldVersion);
                            object currentValue = prop.GetValue(currentVersion);

                            if (oldValue == null && currentValue == null) { continue; }
                            if (Equals(oldValue, currentValue)) { continue; }
                            change.ChangedProperties.Add(new ChangedProperty
                            {
                                NewValue = currentValue,
                                OldValue = oldValue,
                                PropertyInfo = prop
                            });
                        }
                    }
                    if (change.ChangedProperties.Any())
                    {
                        changes.Add(change);
                    }
                }

            }
            return changes;
        }

        static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        static bool IsTrackable(PropertyInfo prop)
        {
            // If prop has a private setter, it will be included only with the JsonIncludeAttribute
            if (!prop.CanWrite && prop.GetCustomAttribute<JsonIncludeAttribute>() == null)
            {
                return false;
            }
            if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string) ) { return true; }
            if (CanTrack.Any(c => c(prop.PropertyType))) { return true; }
            return false;
        }

    }
}
