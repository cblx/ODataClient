using System.Linq.Expressions;

namespace Cblx.Dynamics.FetchXml.Linq;

public static class IQueryableExtensions{
    public static string ToFetchXml<T>(this IQueryable<T> queryable)
    {
        var fetchXmlQueryable = queryable as FetchXmlQueryable<T>;
        if(fetchXmlQueryable == null)
        {
            throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
        }

        return fetchXmlQueryable.ToFetchXml();
    }

    public static string ToRelativeUrl<T>(this IQueryable<T> queryable)
    {
        var fetchXmlQueryable = queryable as FetchXmlQueryable<T>;
        if (fetchXmlQueryable == null)
        {
            throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
        }

        return fetchXmlQueryable.ToRelativeUrl();
    }

    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a FetchXmlQueryable"); }
        fetchXmlQueryable = (fetchXmlQueryable.Take(1) as FetchXmlQueryable<T>)!;
        return await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<T>(fetchXmlQueryable.Expression);
    }
    
    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a FetchXmlQueryable"); }
        fetchXmlQueryable = (fetchXmlQueryable.Where(predicate).Take(1) as FetchXmlQueryable<T>)!;
        return await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<T>(fetchXmlQueryable.Expression);
    }

    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a FetchXmlQueryable"); }
        var items = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
        return items.ToList();
    }

    public static async Task<int> CountAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a FetchXmlQueryable"); }
        var items = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
        return items.Count();
    }

    public static async Task<string> GetStringResponseAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable)
        {
            throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
        }
        string str = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.GetStringResponseAsync(fetchXmlQueryable.Expression);
        return str;
    }

    //public static async Task<int> CountAsync<T>(this IQueryable<T> queryable)
    //{
    //    if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable)
    //    {
    //        throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
    //    }
    //    var items = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
    //}
}

