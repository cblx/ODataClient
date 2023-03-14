namespace Cblx.OData.Client.Abstractions;
public interface IDynamicsMetadataProvider
{
    //string GetColumnName<TEntity>(string propertyName);
    bool IsEdmDate<TEntity>(string columnName) where TEntity : class;
}
