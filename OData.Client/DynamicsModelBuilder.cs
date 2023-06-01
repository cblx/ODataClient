namespace Cblx.Dynamics;

public class DynamicsModelBuilder
{
    public DynamicsModel Model { get; } = new();

    internal DynamicsModelBuilder() { }

    public DynamicsEntityTypeBuilder Entity(Type type)
    {
        InitEntity(type);
        var builder = new DynamicsEntityTypeBuilder(Model.Entities[type]);
        return builder;
    }

    public DynamicsEntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class
    {
        InitEntity(typeof(TEntity));
        var builder = new DynamicsEntityTypeBuilder<TEntity>(Model.Entities[typeof(TEntity)]);
        return builder;
    }

    private void InitEntity(Type type)
    {
        if(Model.Entities.ContainsKey(type)) { return; }
        Model.Entities[type] = new DynamicsEntityType(type);
    }
        
}
