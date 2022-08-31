using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
namespace OData.Client.Abstractions.Write;
public class Set<T, TValue> : BodyElement<T> where T: class
{
    private readonly object? value;
    private readonly string propName;
    public Set(Expression<Func<T, TValue>> propExpression, object? value)
    {
        var member = propExpression.GetMemberInfo();
        this.propName = member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name;
        this.value = value;
    }

    public Set(Expression<Func<T, int?>> propExpression, object? value)
    {
        var member = (propExpression.Body as MemberExpression)!.Member;
        this.propName = member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name;
        this.value = value;
    }

    public Set(string propName, object? value)
    {
        this.propName = propName;
        this.value = value;
    }

    public KeyValuePair<string, object?> ToKeyValuePair()
    {
        return new KeyValuePair<string, object?>(propName, value);
    }
}
