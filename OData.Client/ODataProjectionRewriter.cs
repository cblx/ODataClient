using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Cblx.Dynamics.OData.Linq;

public class ODataProjectionRewriter : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, ParameterExpression> _jsonParameterExpressions = new();
    public bool HasFormattedValues { get; private set; }

    private ParameterExpression GetJsonParameterExpression(ParameterExpression parameterExpression)
    {
        if (!_jsonParameterExpressions.ContainsKey(parameterExpression))
        {
            _jsonParameterExpressions.Add(parameterExpression, Expression.Parameter(typeof(JsonObject), $"{parameterExpression.Name}_jsonObject"));
        }
        return _jsonParameterExpressions[parameterExpression];
    }

    public LambdaExpression Rewrite(Expression expression)
    {
        switch (expression)
        {
            case LambdaExpression lambda:
                {
                    Expression body = lambda.Body;
                    var jsonParameterExpression = GetJsonParameterExpression(lambda.Parameters[0]);
                    switch (body)
                    {
                        case MemberExpression:
                        case MemberInitExpression:
                        case MethodCallExpression:
                        case NewExpression:
                            {
                                Expression rewrittenExpression = Visit(body);
                                return Expression.Lambda(rewrittenExpression, jsonParameterExpression);
                            }
                        case ParameterExpression parameterExpression
                            when parameterExpression.Type.IsDynamicsEntity():
                            {
                                MethodInfo createEntityMethod =
                                    RewriterHelpers.CreateEntityMethod.MakeGenericMethod(parameterExpression.Type);
                                MethodCallExpression callCreateEntityExpression = Expression.Call(
                                    null,
                                    createEntityMethod,
                                    jsonParameterExpression,
                                    Expression.Constant(parameterExpression.Name)
                                );
                                return Expression.Lambda(callCreateEntityExpression, jsonParameterExpression);
                            }
                        case ParameterExpression:
                            return Expression.Lambda(body, jsonParameterExpression);
                    }
                    break;
                }
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
            } methodCallExpression:
                return Rewrite(methodCallExpression.Arguments[0]);
            case MethodCallExpression
            {
                Method:
                {
                    Name: "Select"
                }
            } methodCallExpression:
                var projectionExpression = (methodCallExpression.Arguments.Last().UnBox() as LambdaExpression)!;
                return Rewrite(projectionExpression);
            case ConstantExpression { Value: IQueryable } constantExpression:
                {
                    var jsonParameterExpression = Expression.Parameter(typeof(JsonObject), $"jsonObject");
                    Type entityType = constantExpression.Value.GetType().GetGenericArguments().First();
                    MethodInfo createEntityMethod =
                        RewriterHelpers.CreateEntityMethod.MakeGenericMethod(entityType);
                    MethodCallExpression callCreateEntityExpression = Expression.Call(
                        null,
                        createEntityMethod,
                        jsonParameterExpression,
                        Expression.Default(typeof(string))
                    );
                    return Expression.Lambda(callCreateEntityExpression, jsonParameterExpression);
                }
        }
        throw new InvalidOperationException("Invalid expression during projection rewrite");
    }

    static MethodInfo _linqSelect = typeof(Enumerable)
        .GetMethods()
        .First(m => m.Name == "Select" && m.GetParameters().Length == 2);
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node)
        {
            case var methodCall
                    when
                        methodCall.Method.IsGenericMethod
                        &&
                        methodCall.Method.GetGenericMethodDefinition() == _linqSelect
                    :
                {
                    var arg = methodCall.Arguments[0];
                    // Removes all previous .Wheres from this Select call
                    while(arg is MethodCallExpression methodCallExpression)
                    {
                        arg = methodCallExpression.Arguments[0];
                    }
                    if (
                        arg is MemberExpression memberExpression
                        && memberExpression.Expression is ParameterExpression parameterExpression
                        && parameterExpression.Type.IsDynamicsEntity())
                    {
                        // Get the path and the json parameter for rewriting
                        (var path, var jsonParameter) = GetMemberPathStack(memberExpression, null);

                        // In the inner select, like children.Select(..)
                        // an child => child.some.thing turns into json_child => AuxGetValue(json_child, ["some","thing"])
                        var rewrittenSelection = Rewrite(methodCall.Arguments[1]);

                        // Make 
                        var getAsArrayCall = Expression.Call(
                            null,
                            RewriterHelpers.GetAsArrayMethod,
                            jsonParameter,
                            Expression.Constant(path)
                        );

                        return Expression.Call(
                            null,
                            _linqSelect.MakeGenericMethod(typeof(JsonObject), methodCall.Method.GetGenericArguments().Last()),
                            getAsArrayCall,
                            rewrittenSelection
                        );
                    }

                    return base.VisitMethodCall(node);
                }
            case MethodCallExpression
            {
                Method.DeclaringType.Name: nameof(DynFunctions)
            } methodCallExpression:
                switch (methodCallExpression.Method.Name)
                {
                    case nameof(DynFunctions.FormattedValue):
                        HasFormattedValues = true;
                        var argument = methodCallExpression.Arguments[0];
                        if (argument is UnaryExpression unaryExpression)
                        {
                            argument = unaryExpression.Operand;
                        }
                        if (argument is MemberExpression memberExpression)
                        {
                            return VisitMember(memberExpression, "@OData.Community.Display.V1.FormattedValue", methodCallExpression.Method.ReturnType);
                        }
                        else
                        {
                            throw new InvalidOperationException("The argument in FormattedValue must be a Dynamics field");
                        }
                    default: throw new InvalidOperationException($"The dynamics function {methodCallExpression.Method.Name} is not implemented");
                }
        }
        return base.VisitMethodCall(node);
    }

    (Stack<string> pathStack, ParameterExpression jsonParameter) GetMemberPathStack(MemberExpression node, string? applyAnnotation)
    {
        Stack<string> fieldsStack = new();
        fieldsStack.Push($"{node.Member.GetColName()}{applyAnnotation}");
        while (node.Expression is MemberExpression parentMemberExpression)
        {
            fieldsStack.Push(parentMemberExpression.Member.GetColName());
            node = parentMemberExpression;
        }
        var rootParameter = node.Expression as ParameterExpression;
        if(rootParameter is null) { throw new InvalidOperationException($"The member expression {node} must be used with a valid entity parameter. Check if the entity class is annotated with DynamicsEntityAttribute."); }
        var jsonParameterExpression = GetJsonParameterExpression(rootParameter);
        return (fieldsStack, jsonParameterExpression);
    }

    Expression VisitMember(MemberExpression node, string? applyAnnotation, Type? overrideType)
    {
        if(node.Expression?.Type.IsDynamicsEntity() is false) { return node; }
        MemberInfo memberInfo = node.Member;
        (var fieldsStack, var jsonParameterExpression) = GetMemberPathStack(node, applyAnnotation);
       
        PropertyInfo? propertyInfo = memberInfo as PropertyInfo;
        if (propertyInfo == null)
        {
            throw new InvalidOperationException("Member must be a property");
        }

        Type propertyType = overrideType ?? propertyInfo.PropertyType;
        bool isNullable = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        Type? innerType = isNullable ? propertyType.GetGenericArguments()[0] : propertyType;

        MethodInfo auxGetValueMethod = RewriterHelpers.AuxGetValueMethod;
        if (innerType.IsEnum)
        {
            MethodInfo genericAuxGetValueMethod =
                auxGetValueMethod.MakeGenericMethod(isNullable ? typeof(int?) : typeof(int));
            var callGetValue = Expression.Call(
                null,
                genericAuxGetValueMethod,
                jsonParameterExpression,
                Expression.Constant(fieldsStack)
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
                jsonParameterExpression,
                Expression.Constant(fieldsStack)
            );
        }
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return VisitMember(node, null, null);
    }
}