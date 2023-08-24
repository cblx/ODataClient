using Cblx.Dynamics;
using Cblx.OData.Client.Abstractions;
using Cblx.OData.Client.Abstractions.Json;
using OData.Client;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cblx.OData.Client;

internal class JsonChangeTracker<TTable> : IChangeTracker
{
    readonly Dictionary<Guid, string> _states = new();
    readonly Dictionary<Guid, object> _entities = new();
    readonly HashSet<Guid> _markedForRemove = new();
    readonly JsonSerializerOptions _options = new();
    private readonly IDynamicsMetadataProvider _metadataProvider;

    public JsonChangeTracker(IDynamicsMetadataProvider metadataProvider)
    {
        // If there are no Converters set in the Entity, this allows to 
        // use private constructors and setters
        _options.TypeInfoResolver = JsonContractBuilder.CreateContract();
        _metadataProvider = metadataProvider;
        // Do not use a converter for DateOnly and TimeOnly as the .NET7 already has a built-in converter
    }

    public void Add(object o)
    {
        Guid? id = GetId(o);
        if (id == null || id == Guid.Empty)
        {
            InitId(o);
            id = GetId(o)!;
        }
        _entities.Add(id.Value, o);
    }

    public void Remove(object o)
    {
        Guid? id = GetId(o) ?? throw new InvalidOperationException("Could not find Id for entity in remove");
        _markedForRemove.Add(id.Value);
    }

    public TEntity? AttachOrGetCurrent<TEntity>(TEntity? e)
    {
        if (e is null) { return e; }
        Guid? id = GetId(e) ?? throw new InvalidOperationException("Could not find Id for attached entity");
        if (_entities.TryGetValue(id.Value, out object? value)) { return (TEntity)value; }
        _states.Add(id.Value, JsonSerializer.Serialize(e, typeof(TEntity), _options));
        _entities.Add(id.Value, e);
        return e;
    }

    public IEnumerable<TEntity> AttachOrGetCurrentRange<TEntity>(IEnumerable<TEntity?> items)
    {
        var list = new List<TEntity?>();
        foreach (var e in items)
        {
            if (e is null) { continue; }
            list.Add(AttachOrGetCurrent(e));
        }
        return list.ToArray()!;
    }

    internal static PropertyInfo GetIdProp(object entity)
    {
        var idProp = entity.GetType().GetProperty("Id") ?? throw new InvalidOperationException("Entity class must have an 'Id' property");
        return idProp;
    }

    internal static Guid? GetId(object entity)
    {
        object? val = GetIdProp(entity).GetValue(entity);
        if (val == null) { return null; }
        if (val is Guid guid) { return guid; }
        return JsonSerializer.Deserialize<Guid>(JsonSerializer.Serialize(val));
    }

    internal static void InitId(object entity)
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

    public void AcceptChange(Change change)
    {
        if (change.ChangeType == ChangeType.Remove)
        {
            _states.Remove(change.Id);
            _entities.Remove(change.Id);
            _markedForRemove.Remove(change.Id);
        }
        else
        {
            _states[change.Id] = change.NewState;
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
        List<Change> changes = new();
        foreach (var kvp in _entities)
        {
            object currentVersion = kvp.Value;
            var change = new Change
            {
                Id = kvp.Key,
                Entity = currentVersion
            };

            if (_markedForRemove.Contains(change.Id))
            {
                change.ChangeType = ChangeType.Remove;
                changes.Add(change);
                continue;
            }

            Type type = currentVersion.GetType();
            string currentState = JsonSerializer.Serialize(currentVersion, type, _options);
            change.NewState = currentState;
            change.ChangeType = _states.ContainsKey(change.Id) ? ChangeType.Update : ChangeType.Add;

            // The JSON is built with objects and collections filled.
            // this way we can discover if it is a simple type or a complex type
            var jsonTemplate = JsonTemplateHelper.GetTemplate(type);
            var jsonCurrent = JsonSerializer.SerializeToNode(currentVersion, _options)!.AsObject();
            if (change.ChangeType == ChangeType.Add)
            {
                foreach (var prop in jsonTemplate)
                {
                    if (prop.Key.EndsWith(DynAnnotations.FormattedValue)) { continue; }
                    // Nested entities (sub-entity in an aggregate) not supported yet
                    if (prop.Value is JsonObject) { continue; }
                    if(prop.Value is JsonArray) { continue; }
                    if (jsonCurrent[prop.Key] is null) { continue; }
                    // In the old ChangeTracker, we used to check for default value.
                    // If the property value was the default value in the Entity creation, then we would not add the property as changed.
                    change.ChangedProperties.Add(new ChangedProperty
                    {
                        NewValue = jsonCurrent[prop.Key],
                        OldValue = null,
                        FieldLogicalName = prop.Key,
                        NavigationLogicalName = GetNavigationLogicalName(prop.Key),
                    });
                }
                if (change.ChangedProperties.Any())
                {
                    changes.Add(change);
                }
            }
            if (change.ChangeType == ChangeType.Update)
            {
                string oldState = _states[kvp.Key];
                if (oldState != currentState)
                {
                    var jsonOld = JsonSerializer.Deserialize<JsonObject>(oldState, _options)!;
                    foreach (var prop in jsonTemplate)
                    {
                        if (prop.Key.EndsWith(DynAnnotations.FormattedValue)) { continue; }
                        // Compare these two JsonNode, continue (ignore) if same values are found
                        if (jsonCurrent[prop.Key]?.ToJsonString() == jsonOld[prop.Key]?.ToJsonString()) { continue; }
                        change.ChangedProperties.Add(new ChangedProperty
                        {
                            NewValue = jsonCurrent[prop.Key],
                            OldValue = jsonOld[prop.Key],
                            FieldLogicalName = prop.Key,
                            NavigationLogicalName = GetNavigationLogicalName(prop.Key),
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

    private string? GetNavigationLogicalName(string fkLogicalName) => _metadataProvider.FindLogicalNavigationNameByForeignKeyLogicalName(typeof(TTable), fkLogicalName);
}
