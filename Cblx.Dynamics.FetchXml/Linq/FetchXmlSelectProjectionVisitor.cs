﻿using System.Linq.Expressions;
using System.Xml.Linq;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlSelectProjectionVisitor : ExpressionVisitor
{
    private readonly bool _isGroupBy;
    private readonly XElement _fetchXmlElement;
    public bool HasFormattedValues { get; private set; }
    public FetchXmlSelectProjectionVisitor(XElement fetchXml, bool isGroupBy = false)
    {
        _fetchXmlElement = fetchXml;
        _isGroupBy = isGroupBy;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        return Visit(node.Body);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node.Type.IsDynamicsEntity())
        {
            XElement element = _fetchXmlElement
                .Descendants()
                .First(element =>
                    element.Name.LocalName is "entity" or "link-entity" &&
                    element.Attribute("alias")?.Value == node.Name);
            element.AddEntityAttributesForType(node.Type, node.Name);
        }
        return node;
    }

    protected override Expression VisitMember(MemberExpression? memberExpression)
    {
        XElement? linkedNavigationEntityElement = _fetchXmlElement.FindOrCreateElementForMemberExpression(memberExpression);
        if (linkedNavigationEntityElement == null) { throw new Exception("Could not find entity element reference in projection. (Check if [DynamicsEntity] attribute is missing in the entity class)"); }
        string attributeAlias = memberExpression.ToProjectionAttributeAlias();
        var attributeElement = new XElement(
                "attribute",
                new XAttribute("name", memberExpression.GetColName()),
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
        return memberExpression;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        HasFormattedValues = node.Method is { Name: nameof(DynFunctions.FormattedValue) } && node.Method.DeclaringType == typeof(DynFunctions);
        return base.VisitMethodCall(node);
    }
}