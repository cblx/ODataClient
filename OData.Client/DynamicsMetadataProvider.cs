using Cblx.OData.Client.Abstractions;
using OData.Client;

namespace Cblx.Dynamics;

/// <summary>
/// Configuration provider
/// </summary>
public class DynamicsMetadataProvider : IDynamicsMetadataProvider
{
    private readonly DynamicsModel _model;

    public DynamicsMetadataProvider(DynamicsModelConfiguration configuration)
    {
        var builder = new DynamicsModelBuilder();
        configuration.OnModelCreating(builder);
        _model = builder.Model;    
    }

    public string GetEndpoint<TEntity>() where TEntity : class => _model.Entities[typeof(TEntity)].GetEndpointName();

    public string GetEndpoint(Type type) => _model.Entities[type].GetEndpointName();

    public string GetTableName<TEntity>() => _model.Entities[typeof(TEntity)].GetTableName();

    public string GetTableName(Type type) => _model.Entities[type].GetTableName();

    public bool IsEdmDate<TEntity>(string columnName) where TEntity : class => _model.Entities[typeof(TEntity)].IsEdmDate(columnName);
}