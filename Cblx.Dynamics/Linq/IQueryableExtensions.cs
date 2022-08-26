﻿using Cblx.Dynamics.FetchXml.Linq;
using Cblx.Dynamics.OData.Linq;
using System;
using System.Linq.Expressions;

namespace Cblx.Dynamics.Linq;
using ODataXt = OData.Linq.Extensions.IQueryableExtensions;
using FetchXmlXt = FetchXml.Linq.Extensions.IQueryableExtensions;
public static class IQueryableExtensions
{
    static readonly ArgumentException _invalid = new("This Queryable is not a ODataQueryable nor FetchXmlQueryable");

    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.FirstOrDefaultAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.FirstOrDefaultAsync(queryable),
        _ => throw _invalid
    };

    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate) => queryable switch
    {
        ODataQueryable<T> => ODataXt.FirstOrDefaultAsync(queryable, predicate),
        FetchXmlQueryable<T> => FetchXmlXt.FirstOrDefaultAsync(queryable, predicate),
        _ => throw _invalid
    };

    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.ToListAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.ToListAsync(queryable),
        _ => throw _invalid
    };

    public static Task<int> CountAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.CountAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.CountAsync(queryable),
        _ => throw _invalid
    };

    public static Task<string> GetStringResponseAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.GetStringResponseAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.GetStringResponseAsync(queryable),
        _ => throw _invalid
    };
}
