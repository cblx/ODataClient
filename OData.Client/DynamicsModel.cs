namespace Cblx.Dynamics;

public class DynamicsModel
{
    public Dictionary<Type, DynamicsEntityType> Entities { get; } = new();
}