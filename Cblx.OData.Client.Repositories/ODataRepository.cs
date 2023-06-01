using Cblx.OData.Client.Abstractions;
using Cblx.OData.Client.Abstractions.Ids;
using OData.Client;
using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System.Text.Json;

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
    protected IChangeTracker ChangeTracker { get; }
    readonly protected IODataClient oDataClient;
    private IDynamicsMetadataProvider MetadataProvider => oDataClient.MetadataProvider;

    public ODataRepository(IODataClient oDataClient)
    {
        this.oDataClient = oDataClient;
        ChangeTracker = CreateTracker();
    }

    private IChangeTracker CreateTracker()
    {
        if(JsonTemplateHelper.IsJsonBasedDomainEntity(typeof(TEntity)))
        {
            return new JsonChangeTracker<TTable>(oDataClient.MetadataProvider);
        }
        return new ClassicChangeTracker();
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
        return ChangeTracker.AttachOrGetCurrent(e);
    }

    public void Add(TEntity entity) => ChangeTracker.Add(entity);

    public void Remove(TEntity entity) => ChangeTracker.Remove(entity);

    public async Task SaveChangesAsync()
    {
        var changes = ChangeTracker.GetChanges();
        foreach (Change change in changes)
        {
            await SaveChangesForAsync((change.Entity as TEntity)!);
        }

    }

    async Task SaveChangesForAsync(TEntity entity)
    {
        Guid? id = ClassicChangeTracker.GetId(entity) ?? throw new InvalidOperationException("Could not find Id for entity");
        var change = ChangeTracker.GetChange(id.Value);
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
                if (changedProperty.FieldLogicalName == null) { continue; }
                if (!string.IsNullOrWhiteSpace(changedProperty.NavigationLogicalName))
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
                            throw new ArgumentException($"The {changedProperty.FieldLogicalName} field must be able to be serialized as Guid. Value was {changedProperty.NewValue}");
                        }
                        body.Bind(changedProperty.NavigationLogicalName, guid);
                    }
                    else
                    {
                        if (changedProperty.OldValue != null)
                        {
                            await oDataClient.Unbind<TTable>(id.Value, changedProperty.NavigationLogicalName);
                        }
                    }
                }
                else
                {
                    body.Set(changedProperty.FieldLogicalName, changedProperty.NewValue);
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
        ChangeTracker.AcceptChange(change);
    }
}
