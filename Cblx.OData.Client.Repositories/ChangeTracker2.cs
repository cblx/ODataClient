//using Cblx.OData.Client.Abstractions.Ids;
//using Cblx.OData.Client.Abstractions.Json;
//using System.ComponentModel;
//using System.Reflection;
//using System.Text.Json;

//namespace Cblx.OData.Client;

//internal class ChangeTracker2 : IChangeTracker
//{
//    readonly Dictionary<Guid, string> _states = new();
//    readonly Dictionary<Guid, object> _entities = new();
//    readonly HashSet<Guid> _markedForRemove = new();
//    readonly JsonSerializerOptions _options = new();

//    public ChangeTracker2()
//    {
//        _options.TypeInfoResolver = JsonContractBuilder.CreateContract();
//        _options.Converters.Add(new DateOnlyJsonConverter());
//    }

//    public void Add(object o)
//    {
//        Guid? id = GetId(o);
//        if (id == null || id == Guid.Empty)
//        {
//            InitId(o);
//            id = GetId(o)!;
//        }
//        _entities.Add(id.Value, o);
//    }

//    public void Remove(object o)
//    {
//        Guid? id = GetId(o) ?? throw new InvalidOperationException("Could not find Id for entity in remove");
//        _markedForRemove.Add(id.Value);
//    }

//    public TEntity? AttachOrGetCurrent<TEntity>(TEntity? e)
//    {
//        if (e is null) { return e; }
//        Guid? id = GetId(e) ?? throw new InvalidOperationException("Could not find Id for attached entity");
//        if (_entities.TryGetValue(id.Value, out object? value)) { return (TEntity)value; }
//        _states.Add(id.Value, JsonSerializer.Serialize(e, typeof(TEntity), _options));
//        _entities.Add(id.Value, e);
//        return e;
//    }

//    public IEnumerable<TEntity> AttachOrGetCurrentRange<TEntity>(IEnumerable<TEntity?> items)
//    {
//        var list = new List<TEntity?>();
//        foreach (var e in items)
//        {
//            if (e is null) { continue; }
//            list.Add(AttachOrGetCurrent(e));
//        }
//        return list.ToArray()!;
//    }

//    internal static PropertyInfo GetIdProp(object entity)
//    {
//        var idProp = entity.GetType().GetProperty("Id") ?? throw new InvalidOperationException("Entity class must have an 'Id' property");
//        return idProp;
//    }

//    internal static Guid? GetId(object entity)
//    {
//        object? val = GetIdProp(entity).GetValue(entity);
//        if (val == null) { return null; }
//        if (val is Guid guid) { return guid; }
//        return JsonSerializer.Deserialize<Guid>(JsonSerializer.Serialize(val));
//    }

//    internal static void InitId(object entity)
//    {
//        PropertyInfo idProp = GetIdProp(entity);
//        if (idProp.PropertyType == typeof(Guid))
//        {
//            idProp.SetValue(entity, Guid.NewGuid());
//        }
//        else
//        {
//            idProp.SetValue(entity, Activator.CreateInstance(idProp.PropertyType, Guid.NewGuid()));
//        }
//    }

//    public void AcceptChange(Change change)
//    {
//        if (change.ChangeType == ChangeType.Remove)
//        {
//            _states.Remove(change.Id);
//            _entities.Remove(change.Id);
//            _markedForRemove.Remove(change.Id);
//        }
//        else
//        {
//            _states[change.Id] = change.NewState;
//        }
//    }

//    public Change? GetChange(Guid id)
//    {
//        IEnumerable<Change> changes = GetChanges();
//        Change? change = changes.FirstOrDefault(c => c.Id.Equals(id));
//        return change;
//    }

//    public IEnumerable<Change> GetChanges()
//    {
//        List<Change> changes = new();
//        foreach (var kvp in _entities)
//        {
//            object currentVersion = kvp.Value;
//            var change = new Change
//            {
//                Id = kvp.Key,
//                Entity = currentVersion
//            };

//            if (_markedForRemove.Contains(change.Id))
//            {
//                change.ChangeType = ChangeType.Remove;
//                changes.Add(change);
//                continue;
//            }

//            Type type = currentVersion.GetType();
//            string currentState = JsonSerializer.Serialize(currentVersion, type, _options);
//            change.NewState = currentState;
//            change.ChangeType = _states.ContainsKey(change.Id) ? ChangeType.Update : ChangeType.Add;
//            if (change.ChangeType == ChangeType.Add)
//            {
//                foreach (var prop in type.GetProperties())
//                {
//                    if (!IsTrackable(prop)) { continue; }
//                    var currentValue = prop.GetValue(currentVersion);
//                    var defaultValue = GetDefault(prop.PropertyType);
//                    if (defaultValue == null && currentValue == null) { continue; }
//                    if (Equals(defaultValue, currentValue)) { continue; }
//                    change.ChangedProperties.Add(new ChangedProperty
//                    {
//                        NewValue = currentValue,
//                        OldValue = defaultValue,
//                        PropertyInfo = prop
//                    });
//                }
//                if (change.ChangedProperties.Any())
//                {
//                    changes.Add(change);
//                }
//            }
//            if (change.ChangeType == ChangeType.Update)
//            {
//                string oldState = _states[kvp.Key];
//                if (oldState != currentState)
//                {
//                    var oldVersion = JsonSerializer.Deserialize(oldState, type, _options);
//                    foreach (var prop in type.GetProperties())
//                    {
//                        if (!IsTrackable(prop)) { continue; }
//                        var oldValue = prop.GetValue(oldVersion);
//                        var currentValue = prop.GetValue(currentVersion);

//                        if (oldValue == null && currentValue == null) { continue; }
//                        if (Equals(oldValue, currentValue)) { continue; }
//                        change.ChangedProperties.Add(new ChangedProperty
//                        {
//                            NewValue = currentValue,
//                            OldValue = oldValue,
//                            PropertyInfo = prop
//                        });
//                    }
//                }
//                if (change.ChangedProperties.Any())
//                {
//                    changes.Add(change);
//                }
//            }

//        }
//        return changes;
//    }

//    static object? GetDefault(Type type)
//    {
//        if (type.IsValueType)
//        {
//            return Activator.CreateInstance(type);
//        }
//        return null;
//    }

//    static bool IsTrackable(PropertyInfo prop)
//    {
//        if (prop.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly is true)
//        {
//            return false;
//        }
//        if (prop.CanWrite is false && prop.DeclaringType?.GetProperty(prop.Name)?.CanWrite is false)
//        {
//            return false;
//        }
//        if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)) { return true; }
//        if (ChangeTracker.CanTrack.Any(c => c(prop.PropertyType))) { return true; }
//        return false;
//    }

//}
