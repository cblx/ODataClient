using Cblx.OData.Client.Abstractions.Ids;
namespace System;
public static class GuidExtensions
{
    public static TId ToId<TId>(this Guid guid) where TId : Id => (Activator.CreateInstance(typeof(TId), guid) as TId)!;

    public static TId ToId<TId>(this Guid? guid) where TId : Id
    {
        if (guid == null) { return null; }
        return Activator.CreateInstance(typeof(TId), guid.Value) as TId;
    }
}
