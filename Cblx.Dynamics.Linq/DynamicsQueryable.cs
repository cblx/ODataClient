using System.Linq.Expressions;

namespace Cblx.Dynamics.Linq;

public static class DynamicsQueryable
{
    public static IQueryable<T> LateMaterialize<T>(this IQueryable<T> queryable)
    {
        return queryable.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                typeof(DynamicsQueryable).GetMethod(nameof(LateMaterialize))!.MakeGenericMethod(typeof(T)),
                queryable.Expression)
        );
    }
    
    public static IQueryable<T> IncludeCount<T>(this IQueryable<T> queryable)
    {
        return queryable.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                typeof(DynamicsQueryable).GetMethod(nameof(IncludeCount))!.MakeGenericMethod(typeof(T)),
                queryable.Expression)
        );
    }

    public static IQueryable<T> Page<T>(this IQueryable<T> queryable, int page)
    {
        return queryable.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                typeof(DynamicsQueryable).GetMethod(nameof(Page))!.MakeGenericMethod(typeof(T)),
                queryable.Expression,
                Expression.Constant(page))
        );
    }

    public static IQueryable<T> PageCount<T>(this IQueryable<T> queryable, int pageCount)
    {
        return queryable.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                typeof(DynamicsQueryable).GetMethod(nameof(PageCount))!.MakeGenericMethod(typeof(T)),
                queryable.Expression,
                Expression.Constant(pageCount))
        );
    }

    public static IQueryable<T> WithPagingCookie<T>(this IQueryable<T> queryable, string? pagingCookie)
    {
        return queryable.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                typeof(DynamicsQueryable).GetMethod(nameof(WithPagingCookie))!.MakeGenericMethod(typeof(T)),
                queryable.Expression,
                Expression.Constant(pagingCookie, typeof(string)))
        );
    }


    public static IQueryable<TResult> ProjectTo<TResult>(this IQueryable queryable)
    {
        return queryable.Provider.CreateQuery<TResult>(
            Expression.Call(
                null,
                typeof(DynamicsQueryable).GetMethod(nameof(ProjectTo))!.MakeGenericMethod(typeof(TResult)),
                queryable.Expression)
        );
    }
}