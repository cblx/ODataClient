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
    //string? FindLogicalNavigationNameForLookup<TEntity>(string logicalLookupName);
    string? GetLogicalLookupRawNameForMappedNavigationProperty(MemberInfo member);
    string? GetLogicalLookupNameForMappedNavigationProperty(MemberInfo member);
}
