using System.Linq.Expressions;

namespace Cblx.Dynamics.Linq;

public static class DynamicsQueryableExpressionExtensions
{
    public static IQueryable<TResult> ProjectTo<TResult>(this IQueryable queryable) 
    {
        return queryable.Provider.CreateQuery<TResult>(
            Expression.Call(
                null, 
                typeof(DynamicsQueryableExpressionExtensions).GetMethod(nameof(ProjectTo))!.MakeGenericMethod(typeof(TResult)), 
                queryable.Expression)
        );
    }
}
