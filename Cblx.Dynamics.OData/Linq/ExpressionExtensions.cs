using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Cblx.OData.Client.Abstractions.Ids;

namespace Cblx.Dynamics.OData.Linq;

public static class ExpressionExtensions
{
    /// <summary>
    /// Get rid of Convert expressions (casting)
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static Expression UnBox(this Expression expression)
    {
        if(expression is UnaryExpression unaryExpression)
        {
            return unaryExpression.Operand;
        }
        return expression;
    }

    //public static string ToProjectionAttributeAlias(this MemberExpression memberExpression)
    //{
    //    string propAlias = memberExpression.ToString();
    //    propAlias = string
    //        .Join(
    //            '.',
    //            propAlias.Split(".").Where(str => !str.StartsWith("<>")
    //            ));
    //    return propAlias;
    //}
   
    //public static string GetColName(this MemberExpression memberExpression) => memberExpression.Member.GetColName();

    public static bool IsCol(this MemberInfo memberInfo)
    {
        bool isCol = memberInfo is PropertyInfo propertyInfo
                     && memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>() != null
                     && !memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name.Contains("@")
                     && (propertyInfo.PropertyType.IsPrimitive
                         || propertyInfo.PropertyType.IsValueType
                         || propertyInfo.PropertyType == typeof(string)
                         || propertyInfo.PropertyType.IsAssignableTo(typeof(Id)));
        return isCol;
    }
    
    public static string GetColName(this MemberInfo memberInfo)
    {
        return memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? memberInfo.Name;
    }
}
