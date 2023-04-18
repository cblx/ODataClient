namespace Cblx.Dynamics;

public static class DynamicsEntityTypeBuilderExtensions
{
    public static DynamicsEntityTypeBuilder<TEntity> ToTable<TEntity>(this DynamicsEntityTypeBuilder<TEntity> builder, string name) where TEntity: class
    {
        builder._entityType.TableName = name;
        return builder;
    }

    public static DynamicsEntityTypeBuilder ToTable(this DynamicsEntityTypeBuilder builder, string name)
    {
        builder._entityType.TableName = name;
        return builder;
    }

    public static DynamicsEntityTypeBuilder<TEntity> HasEndpointName<TEntity>(this DynamicsEntityTypeBuilder<TEntity> builder, string name) where TEntity : class
    {
        builder._entityType.EndpointName = name;
        return builder;
    }

    public static DynamicsEntityTypeBuilder HasEndpointName(this DynamicsEntityTypeBuilder builder, string name)
    {
        builder._entityType.EndpointName = name;
        return builder;
    }
}