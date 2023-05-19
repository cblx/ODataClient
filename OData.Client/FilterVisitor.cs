using Cblx.Dynamics;
using Cblx.OData.Client;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace OData.Client;
internal class FilterVisitor : ExpressionVisitor
{
    public string Query { get; set; } = "";
    public HashSet<string> VisitedFields { get; } = new HashSet<string>();
    ParameterExpression parameter;
    readonly bool keepParamName;
    public FilterVisitor(bool keepParamName)
    {
        this.keepParamName = keepParamName;
    }

    public override Expression? Visit(Expression? node)
    {
        if (node is LambdaExpression lambdaExpression)
        {
            parameter = lambdaExpression.Parameters[0];
        }
        return base.Visit(node);
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
            switch (node.Method)
            {
              
                case { Name: "Contains" }:
                    Query += $"contains({field},";
                    Visit(node.Arguments[0]);
                    Query += ")";
                    break;
                case { Name: "Any" }:
                    if (node.Arguments.Count == 1)
                    {
                        Query += $"{field}/any()";
                    }
                    else {
                        var subVisitor = new FilterVisitor(keepParamName: true);
                        var subQuery = node.Arguments[1] as LambdaExpression;
                        subVisitor.Visit(subQuery);
                        Query += $"{field}/any({subQuery.Parameters[0].Name}%3A{subVisitor.Query})";
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported method {node.Method.Name}");
            }
            return node;
        }

        switch (node.Method)
        {
            case
            {
                Name: nameof(DynFunctions.LastMonth)
                      or nameof(DynFunctions.NextMonth) 
                      or nameof(DynFunctions.ThisMonth)
            } m when m.DeclaringType == typeof(DynFunctions):
                Query += $"Microsoft.Dynamics.CRM.{m.Name}(PropertyName='";
                Visit(node.Arguments[0]);
                Query += "')";
                break;
            case
            {
                Name: nameof(DynFunctions.In)
            } m when m.DeclaringType == typeof(DynFunctions):
                Query += $"Microsoft.Dynamics.CRM.In(PropertyName='";
                Visit(node.Arguments[0]);
                Query += "',PropertyValues=[";
                IEnumerable values = (Expression.Lambda(node.Arguments[1]).Compile().DynamicInvoke() as IEnumerable)!;
                Query += string.Join(",", values.Cast<object>().Select(ODataHelpers.ParseValueAsString));
                Query += "])";
                break;
            case 
            { 
                Name: nameof(DynFunctions.ContainValues) or nameof(DynFunctions.DoesNotContainValues) 
            } m when m.DeclaringType == typeof(DynFunctions):
                Query += $"Microsoft.Dynamics.CRM.{m.Name}(PropertyName='";
                Visit(node.Arguments[0]);
                Query += "',PropertyValues=";
                Visit(node.Arguments[1]);
                Query += ")";
                break;
            default: 
                WriteValue(Expression.Lambda(node).Compile().DynamicInvoke());
                break;
        }
        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        WriteValue(Expression.Lambda(node).Compile().DynamicInvoke());
        return node;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        WriteValue(Expression.Lambda(node).Compile().DynamicInvoke());
        return node;
    }


    protected override Expression VisitConstant(ConstantExpression node)
    {
        WriteValue(node.Value);
        return base.VisitConstant(node);
    }

    bool WriteValue(object? o)
    {
        if(ODataHelpers.TryParseValue(o, out string? stringValue))
        {
            Query += stringValue;
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
            string field = string.Join('/', fieldPath);
            VisitedFields.Add(field);
            Query += field;
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

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                Query += "not ";
                Visit(node.Operand);
                break;
            case ExpressionType.Convert:
                Visit(node.Operand);
                break;

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