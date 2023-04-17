using System.Reflection;

namespace Cblx.Dynamics;

public static class TypeExtensions
{
    public static bool IsDynamicsEntity(this Type type) => type.GetCustomAttribute<DynamicsEntityAttribute>() != null;
}
