﻿using Cblx.Dynamics.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Cblx.Dynamics")]
[assembly: InternalsVisibleTo("Cblx.Dynamics.FetchXml.Tests")]
namespace Cblx.Dynamics.FetchXml.Linq.Extensions;

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
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable)
        {
            throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
        }

        return fetchXmlQueryable.ToRelativeUrl();
    }

    internal static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        fetchXmlQueryable = (fetchXmlQueryable.Take(1) as FetchXmlQueryable<T>)!;
        return await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<T>(fetchXmlQueryable.Expression);
    }

    internal static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        fetchXmlQueryable = (fetchXmlQueryable.Where(predicate).Take(1) as FetchXmlQueryable<T>)!;
        return await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<T>(fetchXmlQueryable.Expression);
    }
    
    internal static async Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        return (await queryable.ToArrayAsync()).ToList();
    }

    internal static async Task<T[]> ToArrayAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        var items = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
        return items.ToArray();
    }

    internal static async Task<DynamicsResult<T>> ToResultAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        return await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<DynamicsResult<T>>(fetchXmlQueryable.Expression);
    }

    internal static async Task<List<T>> ToUnlimitedListAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        var items = new List<T>();
        DynamicsResult<T>? result = null;
        int page = 1;
        do
        {
            result = await ToResultAsync(
                fetchXmlQueryable
                    .Page(page)
                    .WithPagingCookie(result?.GetPagingCookie())
            );
            items.AddRange(result.Value!);
            page++;
        } while (string.IsNullOrEmpty(result.FetchXmlPagingCookie) is false);
        return items;
    }

    internal static async Task<int> CountAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable) { throw new InvalidOperationException("This Queryable is not a FetchXmlQueryable"); }
        var items = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.ExecuteAsync<IEnumerable<T>>(fetchXmlQueryable.Expression);
        return items.Count();
    }

    internal static async Task<string> GetStringResponseAsync<T>(this IQueryable<T> queryable)
    {
        if (queryable is not FetchXmlQueryable<T> fetchXmlQueryable)
        {
            throw new ArgumentException("Queryable must be a FetchXmlQueryable", nameof(queryable));
        }
        string str = await (fetchXmlQueryable.Provider as FetchXmlQueryProvider)!.GetStringResponseAsync(fetchXmlQueryable.Expression);
        return str;
    }
}

