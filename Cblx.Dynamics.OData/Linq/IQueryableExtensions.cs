﻿using System.Linq.Expressions;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Cblx.Dynamics")]
[assembly: InternalsVisibleTo("Cblx.Dynamics.OData.Tests")]
namespace Cblx.Dynamics.OData.Linq.Extensions;
public static class IQueryableExtensions
{
    //public static string ToFetchXml<T>(this IQueryable<T> queryable)
    //{
    //    var fetchXmlQueryable = queryable as ODataQueryable<T>;
    //    if (fetchXmlQueryable == null)
    //    {
    //        throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
    //    }

    //    return fetchXmlQueryable.ToFetchXml();
    //}

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
        if (queryable is not ODataQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a ODataQueryable"); }
        fetchXmlQueryable = (fetchXmlQueryable.Take(1) as ODataQueryable<T>)!;
        return await (fetchXmlQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<T>(fetchXmlQueryable.Expression);
    }

    internal static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate)
    {
        if (queryable is not ODataQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a ODataQueryable"); }
        fetchXmlQueryable = (fetchXmlQueryable.Where(predicate).Take(1) as ODataQueryable<T>)!;
        return await (fetchXmlQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<T>(fetchXmlQueryable.Expression);
    }

    internal static async Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not ODataQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a ODataQueryable"); }
        var items = await (fetchXmlQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
        return items.ToList();
    }

    internal static async Task<int> CountAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not ODataQueryable<T> fetchXmlQueryable) { throw new Exception("This Queryable is not a FetchXmlQueryable"); }
        var items = await (fetchXmlQueryable.Provider as ODataQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
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

    //public static async Task<int> CountAsync<T>(this IQueryable<T> queryable)
    //{
    //    if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable)
    //    {
    //        throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
    //    }
    //    var items = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
    //}
}

