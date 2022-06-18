using System.Linq.Expressions;

namespace Cblx.Dynamics.OData.Linq;

public class ProjectionVisitor : ExpressionVisitor
{
    public string Select { get => string.Join(",", _select); }
    private readonly SortedSet<string> _select = new SortedSet<string>();

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        // (e => e) projection
        if(node.Body is ParameterExpression parameterExpression)
        {
            Type entityType = parameterExpression.Type;
            entityType.ToSelectSet().ToList().ForEach(s => _select.Add(s));
            return node; // interrupt visitations
        }
        // skip to visit the lambda body
        return Visit(node.Body);
    }
   
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return base.VisitParameter(node);
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return base.VisitMemberAssignment(node);
    }
   
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression parameterExpression && parameterExpression.Type.IsDynamicsEntity())
        {
            _select.Add(node.Member.GetColName());
        }
        return base.VisitMember(node);
    }
}
