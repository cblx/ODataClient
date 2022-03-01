using System.Linq.Expressions;
using System.Reflection;

namespace OData.Client.Abstractions;
static class ExpressionHelpers
{
    public static MemberInfo GetMemberInfo(this LambdaExpression lambda)
    {
        Expression exp = lambda.Body;
        if(exp is UnaryExpression unaryExpression)
        {
            exp = unaryExpression.Operand;
        }
        return (exp as MemberExpression).Member;
    }
}
