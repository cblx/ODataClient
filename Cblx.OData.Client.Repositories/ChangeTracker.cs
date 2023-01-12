﻿using Cblx.OData.Client.Abstractions.Ids;
using Cblx.OData.Client.Abstractions.Json;
using OData.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client
{
    public class ChangeTracker
    {
        public static ICollection<Func<Type, bool>> CanTrack { get; } = new HashSet<Func<Type, bool>>() { 
            type => type.BaseType == typeof(Id)
        };

        readonly Dictionary<Guid, string> states = new ();
        readonly Dictionary<Guid, object> entities = new ();
        readonly HashSet<Guid> markedForRemove = new ();
        readonly JsonSerializerOptions options = new ();

        public ChangeTracker(){
            options.TypeInfoResolver = JsonContractBuilder.CreateContract();
            options.Converters.Add(new DateOnlyJsonConverter());
        }

        public void Add(object o)
        {
            Guid? id = GetId(o);
            if (id == null || id == Guid.Empty)
            {
                InitId(o);
                id = GetId(o)!;
            }
            entities.Add(id.Value, o);
        }

        public void Remove(object o)
        {
            Guid? id = GetId(o);
            if (id is null)
            {
                throw new InvalidOperationException("Could not find Id for entity in remove");
            }
            markedForRemove.Add(id.Value);
        }

        public TEntity? AttachOrGetCurrent<TEntity>(TEntity? e)
        {
            if (e is null) { return e; }
            Guid? id = GetId(e);
            if (id is null)
            {
                throw new InvalidOperationException("Could not find Id for attached entity");
            }
            if (entities.TryGetValue(id.Value, out object? value)) { return (TEntity)value; }
            states.Add(id.Value, JsonSerializer.Serialize(e, typeof(TEntity), options));
            entities.Add(id.Value, e);
            return e;
        }

        public IEnumerable<TEntity> AttachOrGetCurrentRange<TEntity>(IEnumerable<TEntity?> items)
        {
            var list = new List<TEntity?>();
            foreach (var e in items)
            {
                if(e is null) { continue; }
               list.Add(AttachOrGetCurrent(e));
            }
            return list.ToArray()!;
        }

        internal PropertyInfo GetIdProp(object entity)
        {
            PropertyInfo? idProp = entity.GetType().GetProperty("Id");
            if(idProp is null) { throw new InvalidOperationException("Entity class must have an 'Id' property"); }
            return idProp;
        }

        internal Guid? GetId(object entity)
        {
            object? val = GetIdProp(entity).GetValue(entity);
            if(val == null) { return null; }
            if(val is Guid guid) { return guid; }
            return JsonSerializer.Deserialize<Guid>(JsonSerializer.Serialize(val));
        }

        internal void InitId(object entity)
        {
            PropertyInfo idProp = GetIdProp(entity);
            if (idProp.PropertyType == typeof(Guid))
            {
                idProp.SetValue(entity, Guid.NewGuid());
            }
            else
            {
                idProp.SetValue(entity, Activator.CreateInstance(idProp.PropertyType, Guid.NewGuid()));
            }
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

        public Change? GetChange(Guid id)
        {
            IEnumerable<Change> changes = GetChanges();
            Change? change = changes.FirstOrDefault(c => c.Id.Equals(id));
            return change;
        }

        public IEnumerable<Change> GetChanges()
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

        static object? GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        static bool IsTrackable(PropertyInfo prop)
        {
            if(prop.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly is true)
            {
                return false;
            }
            if (!prop.CanWrite)
            {
                return false;
            }
            if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string) ) { return true; }
            if (CanTrack.Any(c => c(prop.PropertyType))) { return true; }
            return false;
        }

    }
}
