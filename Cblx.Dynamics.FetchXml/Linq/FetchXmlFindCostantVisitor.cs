using System.Linq.Expressions;

namespace Cblx.Dynamics.FetchXml.Linq;
public class FetchXmlFindCostantVisitor : ExpressionVisitor
{
    object? _value;
    public object? GetValue(Expression expression)
    {
        Visit(expression);
        return _value;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        _value = node.Value;
        return base.VisitConstant(node);
    }
}
