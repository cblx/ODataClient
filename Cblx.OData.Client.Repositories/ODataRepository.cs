using Cblx.OData.Client.Abstractions.Ids;
using OData.Client;
using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cblx.OData.Client
{
    public class ODataRepository<TEntity, TTable, TId> : ODataRepository<TEntity, TTable>
        where TEntity : class
        where TTable : class, IHasStronglyTypedId<TId>, new()
        where TId : Id
    {
        public ODataRepository(IODataClient oDataClient) : base(oDataClient){}

        public Task<TEntity?> Get(TId id) => Get<TEntity>(id.Guid);
        public Task<T?> Get<T>(TId id) where T: class, TEntity => Get<T>(id.Guid);
    }

    public class ODataRepository<TEntity, TTable>
    where TEntity : class
    where TTable : class, new()
    {
        readonly protected ChangeTracker changeTracker = new();
        readonly protected IODataClient oDataClient;

        public ODataRepository(IODataClient oDataClient)
        {
            this.oDataClient = oDataClient;
        }

        protected async Task<T?> Get<T>(Guid id)
          where T : class, TEntity
        {
            T e = await oDataClient.From<TTable>().FindAsync<T>(id);
            if (e != null) { changeTracker.Attach(e); }
            return e;
        }

        public void Add(TEntity entity)
        {
            changeTracker.Add(entity);
        }

        public void Remove(TEntity entity)
        {
            changeTracker.Remove(entity);
        }

        public async Task SaveChanges()
        {
            IEnumerable<Change> changes = changeTracker.GetChanges();
            foreach (Change change in changes)
            {
                await SaveChangesFor((change.Entity as TEntity)!);
            }

        }

        async Task SaveChangesFor(TEntity entity)
        {
            Guid? id = changeTracker.GetId(entity);
            if (id == null)
            {
                throw new Exception("Could not find Id for entity");
            }
            Change? change = changeTracker.GetChange(id.Value);
            if (change == null) { return; }

            if (change.ChangeType == ChangeType.Remove)
            {
                await oDataClient.Delete<TTable>(id.Value);
            }
            else
            {
                var body = new Body<TTable>();
                foreach (ChangedProperty changedProperty in change.ChangedProperties)
                {
                    string fieldName = changedProperty.PropertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
                    if (fieldName == null) { continue; }
                    string navName = changedProperty.PropertyInfo.GetCustomAttribute<ODataBindAttribute>()?.Name;

                    if (!string.IsNullOrWhiteSpace(navName))
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
                                throw new ArgumentException($"The {fieldName} field must be able to be serialized as Guid. Value was {changedProperty.NewValue}");
                            }
                            body.Bind(navName, guid);
                        }
                        else
                        {
                            if (changedProperty.OldValue != null)
                            {
                                await oDataClient.Unbind<TTable>(id.Value, navName);
                            }
                        }

                    }
                    else
                    {
                        body.Set(fieldName, changedProperty.NewValue);
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
    // Can't be done this way.
    // This could be done if there were another Attribute like [ODataEntity(typeof(TblType))]
    // It is necessary to have a model representing the navigations, so it can be possible
    // to deduce nav endpoints
    //public class ODataRepository<TEntity>
    //where TEntity : class
    //{
    //    readonly protected ChangeTracker changeTracker = new();
    //    readonly protected IODataClient oDataClient;

    //    public ODataRepository(IODataClient oDataClient)
    //    {
    //        this.oDataClient = oDataClient;
    //    }

    //    protected async Task<T> Get<T>(Guid id)
    //      where T : class, TEntity, new()
    //    {
    //        T e = await oDataClient.From<TEntity>().FindAsync<T>(id);
    //        if (e != null) { changeTracker.Attach(e); }
    //        return e;
    //    }

    //    public void Add(TEntity entity)
    //    {
    //        changeTracker.Add(entity);
    //    }

    //    public void Remove(TEntity entity)
    //    {
    //        changeTracker.Remove(entity);
    //    }

    //    public async Task SaveChanges()
    //    {
    //        IEnumerable<Change> changes = changeTracker.GetChanges();
    //        foreach (Change change in changes)
    //        {
    //            await SaveChangesFor(change.Entity as TEntity);
    //        }

    //    }

    //    async Task SaveChangesFor(TEntity entity)
    //    {
    //        Guid? id = changeTracker.GetId(entity);
    //        Change change = changeTracker.GetChange(id.Value);
    //        if (change == null) { return; }

    //        if (change.ChangeType == ChangeType.Remove)
    //        {
    //            await oDataClient.Delete<TEntity>(id.Value);
    //        }
    //        else
    //        {
    //            var body = new Body<TEntity>();
    //            foreach (ChangedProperty changedProperty in change.ChangedProperties)
    //            {
    //                string fieldName = changedProperty.PropertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
    //                if (fieldName == null) { continue; }
    //                string navName = changedProperty.PropertyInfo.GetCustomAttribute<ODataBindAttribute>()?.Name;

    //                if (!string.IsNullOrWhiteSpace(navName))
    //                {
    //                    if (changedProperty.NewValue != null)
    //                    {
    //                        Guid guid;
    //                        try
    //                        {
    //                            guid = (changedProperty.NewValue as Guid?)
    //                                        ??
    //                                        JsonSerializer.Deserialize<Guid>(JsonSerializer.Serialize(changedProperty.NewValue));

    //                        }
    //                        catch
    //                        {
    //                            throw new ArgumentException($"The {fieldName} field must be able to be serialized as Guid. Value was {changedProperty.NewValue}");
    //                        }
    //                        body.Bind(navName, guid);
    //                    }
    //                    else
    //                    {
    //                        if (changedProperty.OldValue != null)
    //                        {
    //                            await oDataClient.Unbind<TEntity>(id.Value, navName);
    //                        }
    //                    }

    //                }
    //                else
    //                {
    //                    body.Set(fieldName, changedProperty.NewValue);
    //                }
    //            }
    //            if (change.ChangeType == ChangeType.Update)
    //            {
    //                await oDataClient.Patch(id.Value, body);
    //            }
    //            if (change.ChangeType == ChangeType.Add)
    //            {
    //                await oDataClient.Post(body);
    //            }
    //        }
    //        changeTracker.AcceptChange(change);
    //    }
    //}
}
