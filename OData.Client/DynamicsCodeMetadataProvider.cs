using Cblx.OData.Client.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

namespace Cblx.Dynamics;

/// <summary>
/// Configuration provider - Infer types and names from annotations and C# typings.
/// Currently used for old mode compatibilty, when using AddDynamics without a Model Configuration
/// </summary>
public class DynamicsCodeMetadataProvider : IDynamicsMetadataProvider
{
    private static readonly ConcurrentDictionary<Type, DynamicsEntityType> _dynamicsEntityTypes = new();

    public virtual bool IsEdmDate<TEntity>(string columnName) where TEntity : class 
        => GetEntityType(typeof(TEntity)).IsEdmDate(columnName);

    public virtual string GetEndpoint<TEntity>() where TEntity : class 
        => GetEndpoint(typeof(TEntity));

    public virtual string GetEndpoint(Type type) 
        => GetEntityType(type).GetEndpointName();

    public string GetTableName<TEntity>() 
        => GetTableName(typeof(TEntity));

    public string GetTableName(Type type) 
        => GetEntityType(type).GetTableName();

    public bool IsEntity(Type type) 
        => type.GetCustomAttribute<DynamicsEntityAttribute>() != null;

    public string? GetLogicalLookupRawNameForMappedNavigationProperty(MemberInfo member)
        => GetEntityType(member.DeclaringType!).GetProperty(member.Name).RelatedLogicalLookupRawName;

    public string? GetLogicalLookupNameForMappedNavigationProperty(MemberInfo member)
        => GetEntityType(member.DeclaringType!).GetProperty(member.Name).RelatedLogicalLookupName;

    private static DynamicsEntityType GetEntityType(Type type) 
        => _dynamicsEntityTypes.GetOrAdd(type, t => new DynamicsEntityType(t));

    public string? FindLogicalNavigationNameByForeignKeyLogicalName(Type type, string foreignKeyLogicalName) 
        => GetEntityType(type).FindPropertyByLogicalName(foreignKeyLogicalName)?.RelatedLogicalNavigationName;
}