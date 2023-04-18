namespace Cblx.OData.Client.Abstractions;
public interface IDynamicsMetadataProvider
{
    //string GetColumnName<TEntity>(string propertyName);
    bool IsEdmDate<TEntity>(string columnName) where TEntity : class;
    string GetEndpoint<TEntity>() where TEntity : class;
    string GetEndpoint(Type type);
    string GetTableName<TEntity>();
    string GetTableName(Type type);
}
