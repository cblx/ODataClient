using Cblx.OData.Client.Abstractions;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlGroupMemberDictionaryVisitor : ExpressionVisitor
{
    public Dictionary<string, MemberExpression> MemberDictionary { get; } = new();

    protected override Expression VisitMember(MemberExpression node)
    {
        MemberDictionary[node.Member.Name] = node;
        return node;
    }
}

public class FetchXmlGroupByProjectionVisitor : ExpressionVisitor
{
    private readonly FetchXmlGroupMemberDictionaryVisitor _memberDictionaryVisitor = new();
    private readonly XElement _fetchXmlElement;
    private readonly IDynamicsMetadataProvider _metadataProvider;

    public FetchXmlGroupByProjectionVisitor(
        XElement fetchXmlElement,
        LambdaExpression groupExpression,
        IDynamicsMetadataProvider metadataProvider)
    {
        _memberDictionaryVisitor.Visit(groupExpression);
        _fetchXmlElement = fetchXmlElement;
        _metadataProvider = metadataProvider;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        
        XElement CreateAttributeElement(
            MethodCallExpression methodCallExpression, 
            string aggregation,
            bool distinct = false
        )
        {
            LambdaExpression lambda = (methodCallExpression.Arguments[1] as LambdaExpression)!;
            MemberExpression memberExpression = (lambda.Body as MemberExpression)!;
            memberExpression = _memberDictionaryVisitor.MemberDictionary[memberExpression.Member.Name];
            string entityAlias = memberExpression.GetEntityAlias(_metadataProvider);
            string attributeAlias = 
                distinct ?
                $"{entityAlias}.{memberExpression.Member.Name}.Distinct.{aggregation}"
                : $"{entityAlias}.{memberExpression.Member.Name}.{aggregation}";

            XElement entityElement = 
                _fetchXmlElement.FindEntityElementByAlias(entityAlias) ??
                _fetchXmlElement.Descendants().First();
            var attributeElement = new XElement(
                    "attribute",
                    new XAttribute("name", memberExpression.GetColName()),
                    new XAttribute("alias", attributeAlias),
                    new XAttribute("aggregate", aggregation.ToLower())
            );
            if (distinct)
            {
                attributeElement.SetAttributeValue("distinct", "true");
            }
            entityElement.Add(attributeElement);
            return attributeElement;
            //return _groupedAttributesPerProjectedMemberName[memberExpression.Member];
        }
        MethodCallExpression? selectCallExpression = null;
        switch (node.Method.Name)
        {
            case "Count":
                MethodCallExpression? methodCallExpression = node.Arguments[0] as MethodCallExpression;
                bool distinct = false;
                if (methodCallExpression?.Method.Name == "Distinct")
                {
                    //selectCallExpression = Visit(methodCallExpression) as MethodCallExpression;
                    selectCallExpression = (methodCallExpression.Arguments[0] as MethodCallExpression)!;
                    distinct = true;
                }
                else
                {
                    selectCallExpression = (node.Arguments[0] as MethodCallExpression)!;
                }
                if (selectCallExpression?.Method.Name != "Select")
                {
                    throw new Exception("Count is currently only supported on members, ex: g.Select(item => item.Member).Count() or g.Select(item => item.Member).Distinct().Count()");
                }
                CreateAttributeElement(selectCallExpression, "CountColumn", distinct);
                return node;
            case "Sum":
                CreateAttributeElement(node, "Sum");
                return node;
            //case "Distinct":
            //    if ((node.Arguments[0] as MethodCallExpression)?.Method.Name != "Select")
            //    {
            //        throw new Exception("Distinct must be used after selecting a member, ex: g.Select(item => item.Value).Distinct()");
            //    }
            //    selectCallExpression = (node.Arguments[0] as MethodCallExpression)!;
            //    CreateAttributeElement(selectCallExpression).SetAttributeValue("distinct", "true");
            //    return selectCallExpression;
        }
        return base.VisitMethodCall(node);
    }
}