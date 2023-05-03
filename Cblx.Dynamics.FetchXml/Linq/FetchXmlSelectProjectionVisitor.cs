using Cblx.OData.Client.Abstractions;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlSelectProjectionVisitor : ExpressionVisitor
{
    private readonly bool _isGroupBy;
    private readonly XElement _fetchXmlElement;
    private readonly IDynamicsMetadataProvider _metadataProvider;

    public bool HasFormattedValues { get; private set; }
    public FetchXmlSelectProjectionVisitor(XElement fetchXml, IDynamicsMetadataProvider metadataProvider, bool isGroupBy = false)
    {
        _fetchXmlElement = fetchXml;
        _metadataProvider = metadataProvider;
        _isGroupBy = isGroupBy;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        return Visit(node.Body);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (_metadataProvider.IsEntity(node.Type))
        {
            XElement element = _fetchXmlElement
                .Descendants()
                .First(element =>
                    element.Name.LocalName is "entity" or "link-entity" &&
                    element.Attribute("alias")?.Value == node.Name);
            element.AddEntityAttributesForType(node.Type/*, node.Name*/);
        }
        return node;
    }

    protected override Expression VisitMember(MemberExpression? node)
    {
        XElement? linkedNavigationEntityElement = _fetchXmlElement.FindOrCreateElementForMemberExpression(node, _metadataProvider);
        if (linkedNavigationEntityElement == null) { return node; }
        string attributeAlias = node.ToProjectionAttributeAlias();
        var attributeElement = new XElement(
                "attribute",
                new XAttribute("name", node.GetColName()),
                new XAttribute("alias", attributeAlias)
        );
        if (_isGroupBy)
        {
            attributeElement.SetAttributeValue("groupby", "true");
        }
        if (linkedNavigationEntityElement.Elements().Any(el => el.ToString() == attributeElement.ToString()) is false)
        {
            linkedNavigationEntityElement.Add(attributeElement);
        }
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        HasFormattedValues = HasFormattedValues || node.Method is { Name: nameof(DynFunctions.FormattedValue) } && node.Method.DeclaringType == typeof(DynFunctions);
        return base.VisitMethodCall(node);
    }
}