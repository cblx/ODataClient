namespace Cblx.OData.Client.Abstractions;
public interface IDynamicsMetadataProvider
{
    //string GetColumnName<TEntity>(string propertyName);
    bool IsEdmDate<TEntity>(string columnName) where TEntity : class;
    string GetEndpoint<TEntity>() where TEntity : class;
    string GetEndpoint(Type entityType);
    string GetTableName<TEntity>();
    string GetTableName(Type type);
}
