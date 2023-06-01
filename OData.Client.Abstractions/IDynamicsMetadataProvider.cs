using System.Reflection;

namespace Cblx.OData.Client.Abstractions;
public interface IDynamicsMetadataProvider
{
    bool IsEdmDate<TEntity>(string columnName) where TEntity : class;
    string GetEndpoint<TEntity>() where TEntity : class;
    string GetEndpoint(Type type);
    string GetTableName<TEntity>();
    string GetTableName(Type type);
    bool IsEntity(Type type);
    string? FindLogicalNavigationNameByForeignKeyLogicalName(Type type, string foreignKeyLogicalName);
    string? GetLogicalLookupRawNameForMappedNavigationProperty(MemberInfo member);
    string? GetLogicalLookupNameForMappedNavigationProperty(MemberInfo member);
}
