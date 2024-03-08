using Cblx.Dynamics.FetchXml.Linq;
using Cblx.Dynamics.OData.Linq;
using System.Linq.Expressions;

namespace Cblx.Dynamics.Linq;
using ODataXt = OData.Linq.Extensions.IQueryableExtensions;
using FetchXmlXt = FetchXml.Linq.Extensions.IQueryableExtensions;
public static class IQueryableExtensions
{
    static readonly ArgumentException _invalid = new("This Queryable is not a ODataQueryable nor FetchXmlQueryable");

    // Avisa que esse método não será mais suportado e que seu nome mudou para evitar conflito com EF Core.
    [Obsolete("This method is not supported anymore, and it has been renamed to avoid conflict with EF Core. Consider using Cblx.EntityFrameworkCore.Dataverse Ef Core provider for Linq Queries")]
    public static Task<T?> CompatFirstOrDefaultAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.FirstOrDefaultAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.FirstOrDefaultAsync(queryable),
        _ => throw _invalid
    };

    [Obsolete("This method is not supported anymore, and it has been renamed to avoid conflict with EF Core. Consider using Cblx.EntityFrameworkCore.Dataverse Ef Core provider for Linq Queries")]
    public static Task<T?> CompatFirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate) => queryable switch
    {
        ODataQueryable<T> => ODataXt.FirstOrDefaultAsync(queryable, predicate),
        FetchXmlQueryable<T> => FetchXmlXt.FirstOrDefaultAsync(queryable, predicate),
        _ => throw _invalid
    };

    [Obsolete("This method is not supported anymore, and it has been renamed to avoid conflict with EF Core. Consider using Cblx.EntityFrameworkCore.Dataverse Ef Core provider for Linq Queries")]
    public static Task<List<T>> CompatToListAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.ToListAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.ToListAsync(queryable),
        _ => throw _invalid
    };

    [Obsolete("This method is not supported anymore, and it has been renamed to avoid conflict with EF Core. Consider using Cblx.EntityFrameworkCore.Dataverse Ef Core provider for Linq Queries")]
    public static Task<T[]> CompatToArrayAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.ToArrayAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.ToArrayAsync(queryable),
        _ => throw _invalid
    };

    public static Task<List<T>> ToUnlimitedListAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => throw new NotImplementedException("ToUnlimitedListAsync is not yet implemented for OData Queryables"),
        FetchXmlQueryable<T> => FetchXmlXt.ToUnlimitedListAsync(queryable),
        _ => throw _invalid
    };

    public static Task<DynamicsResult<T>> ToResultAsync<T>(this IQueryable<T> queryable) => queryable switch
    {
        ODataQueryable<T> => ODataXt.ToResultAsync(queryable),
        FetchXmlQueryable<T> => FetchXmlXt.ToResultAsync(queryable),
        _ => throw _invalid
    };

    [Obsolete("This method is not supported anymore, and it has been renamed to avoid conflict with EF Core. Consider using Cblx.EntityFrameworkCore.Dataverse Ef Core provider for Linq Queries")]
    public static Task<int> CompatCountAsync<T>(this IQueryable<T> queryable) => queryable switch
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

