using Cblx.Dynamics.Linq;
using Cblx.OData.Client.Abstractions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlProjectionRewriter : ExpressionVisitor
{
    private readonly IDynamicsMetadataProvider _metadataProvider;

    public FetchXmlProjectionRewriter(IDynamicsMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    private readonly ParameterExpression _jsonParameterExpression =
        Expression.Parameter(typeof(JsonObject), "jsonObject");
    public LambdaExpression Rewrite(Expression expression)
    {
        switch (expression)
        {
            case MethodCallExpression
            {
                Method.Name: nameof(DynamicsQueryable.LateMaterialize)
                             or nameof(DynamicsQueryable.WithPagingCookie)
                             or nameof(DynamicsQueryable.Page)
                             or nameof(DynamicsQueryable.PageCount)
                             or nameof(DynamicsQueryable.IncludeCount)
            } m when m.Method.DeclaringType == typeof(DynamicsQueryable):
                return Rewrite(m.Arguments[0]);
            case MethodCallExpression
            {
                Method.Name:
                        "Distinct"
                        or "Take"
                        or "Where"
                        or "FirstOrDefault"
            } methodCallExpression:
                return Rewrite(methodCallExpression.Arguments[0]);
            case MethodCallExpression
            {
                Method.Name:
                        "Select"
                        or "Join"
                        or "SelectMany"
            } methodCallExpression:
                var projectionExpression = (methodCallExpression.Arguments.Last().UnBox() as LambdaExpression)!;
                Expression body = projectionExpression.Body;
                switch (body)
                {
                    case MemberExpression or MemberInitExpression or NewExpression:
                        return Expression.Lambda(Visit(body), _jsonParameterExpression);
                    case ParameterExpression parameterExpression
                        when _metadataProvider.IsEntity(parameterExpression.Type):
                        {
                            MethodInfo createEntityMethod =
                                RewriterHelpers.CreateEntityMethod.MakeGenericMethod(parameterExpression.Type);
                            MethodCallExpression callCreateEntityExpression = Expression.Call(
                                null,
                                createEntityMethod,
                                _jsonParameterExpression//,
                                                        //Expression.Constant(parameterExpression.Name)
                            );
                            return Expression.Lambda(callCreateEntityExpression, _jsonParameterExpression);
                        }
                    case ParameterExpression:
                        return Expression.Lambda(body, _jsonParameterExpression);
                }
                break;
            case MethodCallExpression
            {
                Method.Name: nameof(DynamicsQueryable.ProjectTo)
            } methodCallExpression when methodCallExpression.Method.DeclaringType == typeof(DynamicsQueryable):
                {
                    Type entityType = methodCallExpression.Method.GetGenericArguments().First();
                    MethodInfo createEntityMethod =
                        RewriterHelpers.CreateEntityMethod.MakeGenericMethod(entityType);
                    MethodCallExpression callCreateEntityExpression = Expression.Call(
                        null,
                        createEntityMethod,
                        _jsonParameterExpression//,
                                                //Expression.Default(typeof(string))
                    );
                    return Expression.Lambda(callCreateEntityExpression, _jsonParameterExpression);
                }
            case ConstantExpression { Value: IQueryable } constantExpression:
                {
                    Type entityType = constantExpression.Value.GetType().GetGenericArguments().First();
                    MethodInfo createEntityMethod =
                        RewriterHelpers.CreateEntityMethod.MakeGenericMethod(entityType);
                    MethodCallExpression callCreateEntityExpression = Expression.Call(
                        null,
                        createEntityMethod,
                        _jsonParameterExpression//,
                                                //Expression.Default(typeof(string))
                    );
                    return Expression.Lambda(callCreateEntityExpression, _jsonParameterExpression);
                }
            case MethodCallExpression methodCallExpression:
                throw new InvalidOperationException($"Unsupported method {methodCallExpression.Method.Name}");
        }
        throw new InvalidOperationException("Invalid expression during projection rewrite");
    }
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method)
        {
            case
            {
                Name: nameof(DynFunctions.FormattedValue)
            } m when m.DeclaringType == typeof(DynFunctions):
                var argument = node.Arguments[0];
                if (argument is UnaryExpression unaryExpression)
                {
                    argument = unaryExpression.Operand;
                }
                if (argument is MemberExpression memberExpression)
                {
                    return VisitMember(memberExpression, $"@{DynAnnotations.FormattedValue}", node.Method.ReturnType);
                }
                else
                {
                    throw new InvalidOperationException("The argument in FormattedValue must be a Dynamics field");
                }
        }
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node) => VisitMember(node, null, null);

    Expression VisitMember(MemberExpression node, string? applyAnnotation, Type? overrideType)
    {
        if (node.Expression is null || !_metadataProvider.IsEntity(node.Expression.Type)) { return node; }

        MemberInfo memberInfo = node.Member;
        string propAlias = $"{node.ToProjectionAttributeAlias()}{applyAnnotation}";

        MethodInfo auxGetValueMethod =
            typeof(RewriterHelpers)
                .GetMethod(nameof(RewriterHelpers.AuxGetValue), BindingFlags.Public | BindingFlags.Static)!;

        PropertyInfo? propertyInfo = memberInfo as PropertyInfo;
        if (propertyInfo == null)
        {
            throw new ArgumentException("Member must be a property");
        }

        Type propertyType = overrideType ?? propertyInfo.PropertyType;
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
                Expression.Constant(propAlias)
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
                Expression.Constant(propAlias)
            );
        }
    }
}