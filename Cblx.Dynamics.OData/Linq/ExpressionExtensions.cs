//using System.Linq.Expressions;
//using System.Reflection;
//using System.Text.Json.Serialization;
//using Cblx.OData.Client.Abstractions.Ids;

//namespace Cblx.Dynamics.OData.Linq;

//public static class ExpressionExtensions
//{
//    /// <summary>
//    /// Get rid of Convert expressions (casting)
//    /// </summary>
//    /// <param name="expression"></param>
//    /// <returns></returns>
//    public static Expression UnBox(this Expression expression)
//    {
//        if(expression is UnaryExpression unaryExpression)
//        {
//            return unaryExpression.Operand;
//        }
//        return expression;
//    }

//    public static Stack<MemberExpression> CreateMemberFullStack(this MemberExpression memberExpression)
//    {
//        Stack<MemberExpression> memberStack = new();
//        memberStack.Push(memberExpression);
//        Expression? expression = memberExpression.Expression;
//        while (expression is MemberExpression parentMemberExpression)
//        {
//            memberStack.Push(parentMemberExpression);
//            expression = parentMemberExpression.Expression;
//        }
//        return memberStack;
//    }

//    public static Stack<MemberExpression> CreateMemberParentsStack(this MemberExpression memberExpression)
//    {
//        Stack<MemberExpression> memberStack = new();
//        Expression? expression = memberExpression.Expression;
//        while (expression is MemberExpression parentMemberExpression)
//        {
//            memberStack.Push(parentMemberExpression);
//            expression = parentMemberExpression.Expression;
//        }
//        return memberStack;
//    }

//    public static string GetFieldName(this MemberExpression memberExpression)
//    {
//        string fieldName = memberExpression.Member.Name;
//        var jsonPropertyNameAttr = memberExpression.Member.GetCustomAttribute<JsonPropertyNameAttribute>();
//        if (jsonPropertyNameAttr != null)
//        {
//            fieldName = jsonPropertyNameAttr.Name;
//        }
//        return fieldName;
//    }

//    public static bool IsCol(this MemberInfo memberInfo)
//    {
//        bool isCol = memberInfo is PropertyInfo propertyInfo
//                     && memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>() != null
//                     && !memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name.Contains("@")
//                     && (propertyInfo.PropertyType.IsPrimitive
//                         || propertyInfo.PropertyType.IsValueType
//                         || propertyInfo.PropertyType == typeof(string)
//                         || propertyInfo.PropertyType.IsAssignableTo(typeof(Id)));
//        return isCol;
//    }
    
//    public static string GetColName(this MemberInfo memberInfo)
//    {
//        return memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? memberInfo.Name;
//    }
//}
