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
