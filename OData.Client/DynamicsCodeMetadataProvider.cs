using Cblx.OData.Client.Abstractions;
using System.Reflection;

namespace Cblx.Dynamics;

/// <summary>
/// Configuration provider - Infer types and names from annotations and C# typings.
/// Currently used for old mode compatibilty, when using AddDynamics without a Model Configuration
/// </summary>
public class DynamicsCodeMetadataProvider : IDynamicsMetadataProvider
{
    public virtual bool IsEdmDate<TEntity>(string columnName) where TEntity : class => new DynamicsEntityType { ClrType = typeof(TEntity) }.IsEdmDate(columnName);
  
    public virtual string GetEndpoint<TEntity>() where TEntity : class => GetEndpoint(typeof(TEntity));

    public virtual string GetEndpoint(Type type) => new DynamicsEntityType { ClrType = type }.GetEndpointName();

    public string GetTableName<TEntity>() => GetTableName(typeof(TEntity));
    public string GetTableName(Type type) => new DynamicsEntityType { ClrType = type }.GetTableName();

    public bool IsEntity(Type type) => type.GetCustomAttribute<DynamicsEntityAttribute>() != null;
}
