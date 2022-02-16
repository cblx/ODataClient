using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
namespace OData.Client;
static class MemberExtensions
{
    public static string GetFieldName(this MemberExpression memberExpression)
    {
        string fieldName = memberExpression.Member.Name;
        var jsonPropertyNameAttr = memberExpression.Member.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonPropertyNameAttr != null)
        {
            fieldName = jsonPropertyNameAttr.Name;
        }
        return fieldName;
    }

    public static Stack<MemberExpression> CreateMemberParentsStack(this MemberExpression memberExpression)
    {
        Stack<MemberExpression> memberStack = new();
        Expression expression = memberExpression.Expression;
        while (expression is MemberExpression parentMemberExpression)
        {
            memberStack.Push(parentMemberExpression);
            expression = parentMemberExpression.Expression;
        }
        return memberStack;
    }

    public static Stack<MemberExpression> CreateMemberFullStack(this MemberExpression memberExpression)
    {
        Stack<MemberExpression> memberStack = new();
        memberStack.Push(memberExpression);
        Expression expression = memberExpression.Expression;
        while (expression is MemberExpression parentMemberExpression)
        {
            memberStack.Push(parentMemberExpression);
            expression = parentMemberExpression.Expression;
        }
        return memberStack;
    }
}
