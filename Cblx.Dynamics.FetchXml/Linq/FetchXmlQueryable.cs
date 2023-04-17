using System.Linq.Expressions;

namespace Cblx.Dynamics.FetchXml.Linq;
public class FetchXmlQueryable<T> : QueryableBase<T>
{
    public FetchXmlQueryable(IQueryProvider provider) : base(provider)
    {

    }

    public FetchXmlQueryable(IQueryProvider provider, Expression expression) : base(provider, expression)
    {
    }

    public string ToFetchXml()
    {
        if (Expression != null)
        {
            var visitor = new FetchXmlExpressionVisitor((Provider as FetchXmlQueryProvider)!.MetadataProvider);
            visitor.Visit(Expression);
            return visitor.ToFetchXml();
        }
        return "";
    }

    public string ToRelativeUrl()
    {
        if (Expression != null)
        {
            var visitor = new FetchXmlExpressionVisitor((Provider as FetchXmlQueryProvider)!.MetadataProvider);
            visitor.Visit(Expression);
            return visitor.ToRelativeUrl();
        }
        return "";
    }
}
