namespace Cblx.Dynamics;

public class DynamicsEntityTypeBuilder<TEntity> : DynamicsEntityTypeBuilder
    where TEntity : class
{
    internal DynamicsEntityTypeBuilder(DynamicsEntityType entityType) : base(entityType)
    {
    }
}

public class DynamicsEntityTypeBuilder
{
    internal DynamicsEntityType _entityType;

    internal DynamicsEntityTypeBuilder(DynamicsEntityType entityType)
    {
        _entityType = entityType;
    }
}
