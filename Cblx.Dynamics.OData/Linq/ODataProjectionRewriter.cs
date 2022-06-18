using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Cblx.Dynamics.OData.Linq;

public class ODataProjectionRewriter : ExpressionVisitor
{
    private readonly ParameterExpression _jsonParameterExpression = Expression.Parameter(typeof(JsonObject), "jsonObject");

    public LambdaExpression Rewrite(Expression expression)
    {
        switch (expression)
        {
            case MethodCallExpression
            {
                Method:
                {
                    Name:
                        "Distinct"
                        or "Take"
                        or "Where"
                        or "FirstOrDefault"
                }
            } methodCallExpression: return Rewrite(methodCallExpression.Arguments[0]);
            case MethodCallExpression
            {
                Method:
                {
                    Name: "Select"
                }
            } methodCallExpression:
                var projectionExpression = (methodCallExpression.Arguments.Last().UnBox() as LambdaExpression)!;
                Expression body = projectionExpression.Body;
                switch (body)
                {
                    case MemberExpression memberExpression:
                        {
                            Expression rewrittenNewExpression = Visit(memberExpression);
                            return Expression.Lambda(rewrittenNewExpression, _jsonParameterExpression);
                        }
                    case NewExpression newExpression:
                        {
                            Expression rewrittenNewExpression = Visit(newExpression);
                            return Expression.Lambda(rewrittenNewExpression, _jsonParameterExpression);
                        }
                    case ParameterExpression parameterExpression
                        when parameterExpression.Type.IsDynamicsEntity():
                        {
                            MethodInfo createEntityMethod =
                                RewriterHelpers.CreateEntityMethod.MakeGenericMethod(parameterExpression.Type);
                            MethodCallExpression callCreateEntityExpression = Expression.Call(
                                null,
                                createEntityMethod,
                                _jsonParameterExpression,
                                Expression.Constant(parameterExpression.Name)
                            );
                            return Expression.Lambda(callCreateEntityExpression, _jsonParameterExpression);
                        }
                    case ParameterExpression:
                        return Expression.Lambda(body, _jsonParameterExpression);
                }
                break;
            case ConstantExpression { Value: IQueryable } constantExpression:
                {
                    Type entityType = constantExpression.Value.GetType().GetGenericArguments().First();
                    MethodInfo createEntityMethod =
                        RewriterHelpers.CreateEntityMethod.MakeGenericMethod(entityType);
                    MethodCallExpression callCreateEntityExpression = Expression.Call(
                        null,
                        createEntityMethod,
                        _jsonParameterExpression,
                        Expression.Default(typeof(string))
                    );
                    return Expression.Lambda(callCreateEntityExpression, _jsonParameterExpression);
                }
        }
        throw new Exception("Invalid expression during projection rewrite");
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        MemberInfo memberInfo = node.Member;
        //string propAlias = node.ToProjectionAttributeAlias();
        string colName = node.Member.GetColName();

        MethodInfo auxGetValueMethod =
            typeof(RewriterHelpers)
                .GetMethod(nameof(RewriterHelpers.AuxGetValue), BindingFlags.Public | BindingFlags.Static)!;

        PropertyInfo? propertyInfo = memberInfo as PropertyInfo;
        if (propertyInfo == null)
        {
            throw new ArgumentException("Member must be a property");
        }

        Type propertyType = propertyInfo.PropertyType;
        bool isNullable = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        Type? innerType = isNullable ? propertyType.GetGenericArguments()[0] : propertyType;

        if (innerType.IsEnum)
        {
            MethodInfo genericAuxGetValueMethod =
                auxGetValueMethod.MakeGenericMethod(isNullable ? typeof(int?) : typeof(int));
            var callGetValue = Expression.Call(
                null,
                genericAuxGetValueMethod,
                _jsonParameterExpression,
                Expression.Constant(colName)
            );

            string toEnumMethodName =
                isNullable ? nameof(RewriterHelpers.ToNullableEnum) : nameof(RewriterHelpers.ToEnum);
            MethodInfo toEnumMethod = typeof(RewriterHelpers)
                .GetMethod(toEnumMethodName, BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(innerType);

            return Expression.Call(
                null,
                toEnumMethod,
                callGetValue
            );
        }
        // AuxGetValue<T>(jsonObject, "item.Prop")
        else
        {
            MethodInfo genericAuxGetValueMethod = auxGetValueMethod.MakeGenericMethod(propertyType);
            return Expression.Call(
                null,
                genericAuxGetValueMethod,
                _jsonParameterExpression,
                Expression.Constant(colName)
            );
        }
    }
}