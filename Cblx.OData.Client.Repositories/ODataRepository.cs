using Cblx.Dynamics;
using Cblx.OData.Client.Abstractions;
using Cblx.OData.Client.Abstractions.Ids;
using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cblx.OData.Client;

public class ODataRepository<TEntity, TTable, TId> : ODataRepository<TEntity, TTable>
    where TEntity : class
    where TTable : class, IHasStronglyTypedId<TId>, new()
    where TId : Id
{
    public ODataRepository(IODataClient oDataClient) : base(oDataClient){}

    public Task<TEntity?> GetAsync(TId id) => GetAsync<TEntity>(id.Guid);
    public Task<T?> GetAsync<T>(TId id) where T: class, TEntity => GetAsync<T>(id.Guid);
}

public class ODataRepository<TEntity, TTable>
where TEntity : class
where TTable : class, new()
{
    readonly protected IChangeTracker changeTracker = new ChangeTracker();
    readonly protected IODataClient oDataClient;
    private IDynamicsMetadataProvider MetadataProvider => oDataClient.MetadataProvider;

    public ODataRepository(IODataClient oDataClient)
    {
        this.oDataClient = oDataClient;
    }

    protected async Task<T?> GetAsync<T>(Guid id)
      where T : class, TEntity
    {
        T? e;
        try
        {
            e = await oDataClient.From<TTable>().FindAsync<T>(id);
        } 
        catch (ODataErrorException ex) when (ex.Code == "0x80040217")
        {
            // Ref: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/web-service-error-codes
            // ObjectDoesNotExist = 0x80040217
            return null;
        }
        return changeTracker.AttachOrGetCurrent(e);
    }

    public void Add(TEntity entity)
    {
        changeTracker.Add(entity);
    }

    public void Remove(TEntity entity)
    {
        changeTracker.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        IEnumerable<Change> changes = changeTracker.GetChanges();
        foreach (Change change in changes)
        {
            await SaveChangesForAsync((change.Entity as TEntity)!);
        }

    }

    async Task SaveChangesForAsync(TEntity entity)
    {
        Guid? id = ChangeTracker.GetId(entity) ?? throw new InvalidOperationException("Could not find Id for entity");
        Change? change = changeTracker.GetChange(id.Value);
        if (change == null) { return; }

        if (change.ChangeType == ChangeType.Remove)
        {
            await oDataClient.Delete<TTable>(id.Value);
        }
        else
        {
            var body = new Body<TTable>(MetadataProvider);
            foreach (ChangedProperty changedProperty in change.ChangedProperties)
            {
                string? fieldLogicalName = changedProperty.PropertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
                if (fieldLogicalName == null) { continue; }
                string? navLogicalName =
                    // Old mode: Gets ODataBindAttribute from the Repository Entity
                    changedProperty.PropertyInfo.GetCustomAttribute<ODataBindAttribute>()?.Name
                    // I think this fallback may lead to miss some configuration
                    // when migrating to some kind of Context/Unit of Work model in the future.
                    // If we have some kind of DynamicsContext in the future like a DbContext,
                    // when migrating to the new form, the ODataBind or Fluent Config in the Entity will be necessary.
                    //?? 
                    //// New mode: Search in the related Table model definition
                    //// Note: The current ODataRepository may be deprecated in the future.
                    //// The idea of a Repository that is related to 2 types is somehow strange.
                    //MetadataProvider.FindLogicalNavigationNameForLookup<TTable>(fieldLogicalName)
                    ;


                if (!string.IsNullOrWhiteSpace(navLogicalName))
                {
                    if (changedProperty.NewValue != null)
                    {
                        Guid guid;
                        try
                        {
                            guid = (changedProperty.NewValue as Guid?)
                                        ??
                                        JsonSerializer.Deserialize<Guid>(
                                            JsonSerializer.Serialize(changedProperty.NewValue)
                                        );

                        }
                        catch
                        {
                            throw new ArgumentException($"The {fieldLogicalName} field must be able to be serialized as Guid. Value was {changedProperty.NewValue}");
                        }
                        body.Bind(navLogicalName, guid);
                    }
                    else
                    {
                        if (changedProperty.OldValue != null)
                        {
                            await oDataClient.Unbind<TTable>(id.Value, navLogicalName);
                        }
                    }

                }
                else
                {
                    body.Set(fieldLogicalName, changedProperty.NewValue);
                }
            }
            if (change.ChangeType == ChangeType.Update)
            {
                await oDataClient.Patch(id.Value, body);
            }
            if (change.ChangeType == ChangeType.Add)
            {
                await oDataClient.Post(body);
            }
        }
        changeTracker.AcceptChange(change);
    }
}
