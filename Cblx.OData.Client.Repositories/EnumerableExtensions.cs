namespace Cblx.OData.Client;

public static class EnumerableExtensions
{
    public static async Task<List<T>> AttachedTo<T>(this Task<List<T>> task, IChangeTracker changeTracker)
    {
        var list = await task;
        return changeTracker.AttachOrGetCurrentRange(list).ToList();
    }

    public static async Task<T[]> AttachedTo<T>(this Task<T[]> task, IChangeTracker changeTracker)
    {
        var list = await task;
        return changeTracker.AttachOrGetCurrentRange(list).ToArray();
    }

    public static async Task<T?> AttachedTo<T>(this Task<T?> task, IChangeTracker changeTracker)
    {
        var entity = await task;
        return changeTracker.AttachOrGetCurrent(entity);
    }
}