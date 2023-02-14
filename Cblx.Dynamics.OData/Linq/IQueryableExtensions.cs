using Cblx.Dynamics.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Cblx.Dynamics")]
[assembly: InternalsVisibleTo("Cblx.Dynamics.OData.Tests")]
namespace Cblx.Dynamics.OData.Linq.Extensions;
public static class IQueryableExtensions
{
    internal static string ToRelativeUrl<T>(this IQueryable<T> queryable)
    {
        var fetchXmlQueryable = queryable as ODataQueryable<T>;
        if (fetchXmlQueryable == null)
        {
            throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
        }

        return fetchXmlQueryable.ToRelativeUrl();
    }

    internal static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not ODataQueryable<T> oDataQueryable) { throw new InvalidOperationException("This Queryable is not a ODataQueryable"); }
        oDataQueryable = (oDataQueryable.Take(1) as ODataQueryable<T>)!;
        return await (oDataQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<T>(oDataQueryable.Expression);
    }

    internal static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate)
    {
        if (queryable is not ODataQueryable<T> oDataQueryable) { throw new InvalidOperationException("This Queryable is not a ODataQueryable"); }
        oDataQueryable = (oDataQueryable.Where(predicate).Take(1) as ODataQueryable<T>)!;
        return await (oDataQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<T>(oDataQueryable.Expression);
    }

    internal static async Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        return (await queryable.ToArrayAsync()).ToList();
    }

    internal static async Task<T[]> ToArrayAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not ODataQueryable<T> oDataQueryable) { throw new InvalidOperationException("This Queryable is not a ODataQueryable"); }
        var items = await (oDataQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<IEnumerable<T>>(oDataQueryable.Expression);
        return items.ToArray();
    }

    internal static async Task<DynamicsResult<T>> ToResultAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not ODataQueryable<T> oDataQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        return await (oDataQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<DynamicsResult<T>>(oDataQueryable.Expression);
    }

    internal static async Task<int> CountAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not ODataQueryable<T> oDataQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        var items = await (oDataQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<IEnumerable<T>>(oDataQueryable.Expression);
        return items.Count();
    }

    internal static async Task<string> GetStringResponseAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not ODataQueryable<T> fetchXmlQueryable)
        {
            throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
        }
        string str = await (fetchXmlQueryable.Provider as ODataQueryProvider)!.GetStringResponseAsync(fetchXmlQueryable.Expression);
        return str;
    }
}

