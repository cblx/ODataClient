using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;
using Cblx.OData.Client.Abstractions.Ids;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlGroupProjectionRewriter : ExpressionVisitor
{
    private readonly ParameterExpression _jsonParameterExpression = Expression.Parameter(typeof(JsonObject), "jsonObject");
    private LambdaExpression? groupExpression;
    private LambdaExpression? groupByExpression;

    private FetchXmlGroupMemberDictionaryVisitor groupMemberDictionaryVisitor = new();
    private FetchXmlGroupMemberDictionaryVisitor groupByMemberDictionaryVisitor = new();

    public FetchXmlGroupProjectionRewriter(LambdaExpression? groupExpression, LambdaExpression? groupByExpression)
    {
        this.groupExpression = groupExpression;
        this.groupByExpression = groupByExpression;
        groupMemberDictionaryVisitor.Visit(this.groupExpression);
        groupByMemberDictionaryVisitor.Visit(this.groupByExpression);
    }

    public LambdaExpression Rewrite(Expression expression)
    {
        MethodCallExpression? methodCallExpression = expression as MethodCallExpression;
        if (methodCallExpression == null) { throw new Exception("Invalid expression during projection rewrite"); }
        LambdaExpression? lambdaExpression = methodCallExpression.Arguments.Last().UnBox() as LambdaExpression;
        if (lambdaExpression == null) { throw new Exception("Invalid expression during projection rewrite"); }
        //NewExpression? newExpression;
        Expression? rewrittenNewExpression;
        if (lambdaExpression.Body is MemberInitExpression memberInitExpression)
        {
            rewrittenNewExpression = Visit(memberInitExpression);
        }
        else if(lambdaExpression.Body is NewExpression newExpression)
        {
            rewrittenNewExpression = Visit(newExpression);
        }
        else { throw new Exception("Invalid expression during projection rewrite"); }
        return Expression.Lambda(rewrittenNewExpression, _jsonParameterExpression);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Expression Resolve(
           MethodCallExpression methodCallExpression,
           string aggregation,
           bool distinct = false
       )
        {
            LambdaExpression lambda = (methodCallExpression.Arguments[1] as LambdaExpression)!;
            MemberExpression memberExpression = (lambda.Body as MemberExpression)!;
            memberExpression = groupMemberDictionaryVisitor.MemberDictionary[memberExpression.Member.Name];
            string entityAlias = memberExpression.GetEntityAlias();
            string attributeAlias =
                distinct ?
                $"{entityAlias}.{memberExpression.Member.Name}.Distinct.{aggregation}"
                : $"{entityAlias}.{memberExpression.Member.Name}.{aggregation}";

            var prop = Expression.Property(
               _jsonParameterExpression,
               "Item",
               Expression.Constant(attributeAlias)
           );
            MethodInfo getValueMethod = typeof(JsonObject).GetMethod("GetValue")!;
            MethodInfo genericGetValueMethod = getValueMethod.MakeGenericMethod(node.Method.ReturnType);
            return Expression.Call(
                prop,
                genericGetValueMethod
            );
        }
        switch (node.Method.Name)
        {
            case "Count":
                MethodCallExpression? methodCallExpression = node.Arguments[0] as MethodCallExpression;
                bool distinct = false;
                MethodCallExpression? selectCallExpression;
                if (methodCallExpression?.Method.Name == "Distinct")
                {
                    selectCallExpression = (methodCallExpression.Arguments[0] as MethodCallExpression)!;
                    distinct = true;
                }
                else
                {
                    selectCallExpression = (node.Arguments[0] as MethodCallExpression)!;
                }
                if (selectCallExpression?.Method.Name != "Select")
                {
                    throw new Exception("Count is currently only supported on members, ex: g.Select(item => item.Member).Count() or g.Select(item => item.Member).Distinct().Count()");
                }
                return Resolve(selectCallExpression, "CountColumn", distinct);
            case "Sum":
                return Resolve(node, "Sum");
        }
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        node = node.Member.Name == "Key" ? 
                    // when getting the key => g.Key
                    groupByMemberDictionaryVisitor.MemberDictionary.First().Value :
                    // when getting a key prop => g.Key.Value 
                    groupByMemberDictionaryVisitor
                        .MemberDictionary[node.Member.Name];
        MemberInfo memberInfo = node.Member;
        MemberInfo parentMemberInfo = (node.Expression as MemberExpression)!.Member;

        string propAlias = $"{parentMemberInfo.Name}.{memberInfo.Name}";
        //var prop = Expression.Property(
        //    _jsonParameterExpression,
        //    "Item",
        //    Expression.Constant($"{parentMemberInfo.Name}.{memberInfo.Name}")
        //);

        MethodInfo auxGetValueMethod =
                typeof(RewriterHelpers)
                .GetMethod(nameof(RewriterHelpers.AuxGetValue), BindingFlags.Public | BindingFlags.Static)!;

        PropertyInfo? propertyInfo = memberInfo as PropertyInfo;
        if (propertyInfo == null) { throw new ArgumentException("Member must be a property"); }
        Type propertyType = propertyInfo.PropertyType;
        bool isNullable = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        Type? innerType = isNullable ?
                            propertyType.GetGenericArguments()[0] : propertyType;
        if (propertyType.IsAssignableTo(typeof(Id)))
        {
            MethodInfo genericGetValueMethod = auxGetValueMethod.MakeGenericMethod(typeof(Guid));
            var callGetValue = Expression.Call(
                null,
                genericGetValueMethod,
                _jsonParameterExpression,
                Expression.Constant(propAlias)
            );
            return Expression.Convert(
                callGetValue,
                innerType
            //propertyType.GetConstructor(new[] { typeof(Guid) })!,
            //callGetValue
            );
        }
        else if (innerType.IsEnum)
        {
            MethodInfo genericGetValueMethod = auxGetValueMethod.MakeGenericMethod(
                isNullable ? typeof(int?) : typeof(int)
            );
            var callGetValue = Expression.Call(
                null,
                genericGetValueMethod,
                _jsonParameterExpression,
                Expression.Constant(propAlias)
            );

            string toEnumMethodName = isNullable ? nameof(RewriterHelpers.ToNullableEnum) : nameof(RewriterHelpers.ToEnum);
            MethodInfo toEnumMethod = typeof(RewriterHelpers)
                    .GetMethod(toEnumMethodName, BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(innerType);

            return Expression.Call(
                null,
                toEnumMethod,
                callGetValue
            );

            //return Expression.Convert(
            //    callGetValue,
            //    isNullable ? typeof(Nullable<>).MakeGenericType(innerType) : innerType
            //);
        }
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