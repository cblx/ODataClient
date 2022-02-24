using Cblx.OData.Client.Abstractions.Ids;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
namespace OData.Client;
class FilterVisitor : ExpressionVisitor
{
    public string Query { get; set; } = "";
    ParameterExpression parameter;
    readonly bool keepParamName;
    public FilterVisitor(bool keepParamName)
    {
        this.keepParamName = keepParamName;
    }
    //bool writingOrs = false;

    public override Expression Visit(Expression node)
    {
        if (node is LambdaExpression lambdaExpression)
        {
            parameter = lambdaExpression.Parameters[0];
        }
        return base.Visit(node);
    }


    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Convert:
                Visit(node.Operand);
                break;

        }
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        string str = node.ToString();
        string paramPrefix = parameter.Name + ".";
        if (str.StartsWith(paramPrefix))
        {
            var memberExpression = (node.Object ?? node.Arguments[0]) as MemberExpression;
            string field = string.Join(
                '/', 
                memberExpression.CreateMemberFullStack().Select(m => m.GetFieldName())
            );
            switch (node.Method.Name)
            {
                case "Contains":
                    Query += $"contains({field},";
                    Visit(node.Arguments[0]);
                    Query += ")";
                    break;
                case "Any":
                    var subVisitor = new FilterVisitor(keepParamName: true);
                    var subQuery = node.Arguments[1] as LambdaExpression;
                    subVisitor.Visit(subQuery);
                    Query += $"{field}/any({subQuery.Parameters[0].Name}:{subVisitor.Query})";
                    break;
                default:
                    throw new Exception($"Método {node.Method.Name} não suportado");
            }
            return node;
        }

        WriteValue(Expression.Lambda(node).Compile().DynamicInvoke());

        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        WriteValue(Expression.Lambda(node).Compile().DynamicInvoke());
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        WriteValue(node.Value);
        return base.VisitConstant(node);
    }

    bool WriteValue(object o)
    {
        if (o == null)
        {
            Query += "null";
            return true;
        }

        if (o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            o = o.GetType().GetProperty("Value").GetValue(o, null);
        }
        switch (o)
        {
            case object v when v.GetType().IsEnum:
                Query += Convert.ToInt32(v).ToString();
                return true;
            case object v when v.GetType() == typeof(string):
                string str = (string)v;
                str = str.Replace("'", "''")
                    .Replace("%", "%25")
                    .Replace("#", "%23")
                    .Replace("+", "%2B")
                    .Replace("/", "%2F")
                    .Replace("?", "%3F")
                    .Replace("&", "%26")
                    ;
                Query += $"'{str}'";
                return true;
            case object v when v.GetType() == typeof(bool):
                Query += v.ToString().ToLower();
                return true;
            case object v when v is DateTimeOffset dtoff:
                string strDateTimeOffset = $"{dtoff:O}";
                strDateTimeOffset = strDateTimeOffset
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                Query += strDateTimeOffset;
                return true;
            case object v when v is DateTime dtoff:
                string strDateTime = $"{dtoff:O}";
                strDateTime = strDateTime
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                Query += strDateTime;
                return true;
            case object v when v.GetType() == typeof(Guid):
                Query += $"{v}";
                return true;
            case object v when v.GetType() == typeof(int):
                Query += $"{v}";
                return true;
            case Id id:
                Query += $"{id}";
                return true;
        }
        return false;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        string str = node.ToString();
        string paramPrefix = $"{parameter.Name}.";
        if (str.StartsWith(paramPrefix))
        {
            IEnumerable<string> fieldPath = node.CreateMemberFullStack().Select(m => m.GetFieldName());
            var sb = new StringBuilder();
            if (keepParamName) {
                fieldPath = new string[] { parameter.Name }.Union(fieldPath);
            }
            Query += string.Join('/', fieldPath);
            return node;
        }

        var expression = Visit(node.Expression);
        if (expression == null || expression is ConstantExpression)
        {
            object container = null;
            if (expression is ConstantExpression constantExpression)
            {
                container = constantExpression.Value;
            }

            var member = node.Member;
            if (member is FieldInfo info1)
            {
                object value = info1.GetValue(container);
                //Visit(Expression.Constant(value));
                if (!WriteValue(value))
                {
                    return Expression.Constant(value);
                }
            }
            if (member is PropertyInfo info)
            {
                object value = info.GetValue(container, null);
                if (!WriteValue(value))
                {
                    return Expression.Constant(value);
                }
            }
        }
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.AndAlso:
                Visit(node.Left);
                Query += " and ";
                Visit(node.Right);
                break;
            case ExpressionType.Equal:
                Visit(node.Left);
                Query += " eq ";
                Visit(node.Right);
                break;
            case ExpressionType.GreaterThan:
                Visit(node.Left);
                Query += " gt ";
                Visit(node.Right);
                break;
            case ExpressionType.GreaterThanOrEqual:
                Visit(node.Left);
                Query += " ge ";
                Visit(node.Right);
                break;
            case ExpressionType.LessThan:
                Visit(node.Left);
                Query += " lt ";
                Visit(node.Right);
                break;
            case ExpressionType.LessThanOrEqual:
                Visit(node.Left);
                Query += " le ";
                Visit(node.Right);
                break;
            case ExpressionType.NotEqual:
                Visit(node.Left);
                Query += " ne ";
                Visit(node.Right);
                break;
            case ExpressionType.OrElse:
                //var shouldOpenParenthesis = !writingOrs;
                //if (shouldOpenParenthesis)
                //{
                //    writingOrs = true;
                //    Query += "(";
                //}
                Query += "(";
                Visit(node.Left);
                Query += " or ";
                Visit(node.Right);
                Query += ")";
                //if (shouldOpenParenthesis)
                //{
                //    Query += ")";
                //    writingOrs = false;
                //}
                break;
            default: break;
        }

        return node;
    }
}
