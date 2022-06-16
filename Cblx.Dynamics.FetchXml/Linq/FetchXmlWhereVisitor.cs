using System.Linq.Expressions;
using System.Xml.Linq;
using Cblx.OData.Client.Abstractions.Ids;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlWhereVisitor : ExpressionVisitor
{
    string? _type = null;
    //string _rootEntityAlias;
    public XElement FilterElement { get; } = new XElement("filter");
    public bool IsEmpty => !FilterElement.HasElements;
    private readonly FetchXmlExpressionVisitor _mainVisitor;

    public FetchXmlWhereVisitor(FetchXmlExpressionVisitor mainVisitor)
    {
        _mainVisitor = mainVisitor;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Expression left = Visit(node.Left.UnBox());
        ConstantExpression? leftConstantExpression = left as ConstantExpression;
        object? leftValue = leftConstantExpression?.Value;

        switch (node.NodeType)
        {
            case ExpressionType.OrElse:
                if(leftValue is true) { return node; }
                SetType("or");
                Visit(node.Right);
                break;
            case ExpressionType.AndAlso:
                SetType("and");
                Visit(node.Right);
                break;
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
                if(leftConstantExpression != null)
                {
                    object? comparisonResult = Expression.Lambda(node).Compile().DynamicInvoke();
                    return Expression.Constant(comparisonResult);
                }
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        {
                            object? rightObj = Expression.Lambda(node.Right).Compile().DynamicInvoke();
                            if (rightObj == null)
                            {
                                AddCondition(node.Left, "null", rightObj);
                            }
                            else
                            {
                                AddCondition(node.Left, "eq", rightObj);
                            }
                        }
                        return node;
                    case ExpressionType.NotEqual:
                        {
                            object? rightObj = Expression.Lambda(node.Right).Compile().DynamicInvoke();
                            if (rightObj == null)
                            {
                                AddCondition(node.Left, "not-null", rightObj);
                            }
                            else
                            {
                                AddCondition(node.Left, "ne", rightObj);
                            }
                        }
                        return node;
                    case ExpressionType.GreaterThan:
                        AddCondition(node.Left, "gt", node.Right);
                        return node;
                    case ExpressionType.GreaterThanOrEqual:
                        AddCondition(node.Left, "ge", node.Right);
                        return node;
                    case ExpressionType.LessThan:
                        AddCondition(node.Left, "lt", node.Right);
                        return node;
                    case ExpressionType.LessThanOrEqual:
                        AddCondition(node.Left, "le", node.Right);
                        return node;
                }
                break;
        }
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ConstantExpression constantExpression) { return constantExpression; }
        if(node.Expression is MemberExpression memberExpression) { return base.Visit(memberExpression); }
        return base.VisitMember(node);
    }

    void AddCondition(Expression left, string op, Expression right)
    {
        object? rightObj = Expression.Lambda(right).Compile().DynamicInvoke();
        AddCondition(left, op, rightObj);
    }

    void AddCondition(Expression left, string op, object? rightObj)
    {
        left = left.UnBox();

        if (left is not MemberExpression memberExpression)
        {
            throw new Exception("Left side of where caluses must be a member accessor");
        }

        _mainVisitor.FindOrCreateElementForMemberExpression(memberExpression);


        var conditionElement = new XElement("condition");
        string entityAlias = memberExpression.GetEntityAlias();
        SetEntityNameForLinkedEntity(entityAlias, conditionElement);
        conditionElement.SetAttributeValue("attribute", memberExpression.GetColName());
        conditionElement.SetAttributeValue("operator", op);
        if (rightObj != null)
        {
            conditionElement.SetAttributeValue("value", GetStringRepresentation(rightObj));
        }
        FilterElement.Add(conditionElement);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if(node.Object is MemberExpression memberExpression)
        {
            switch (node.Method.Name)
            {
                case "Contains":
                    var conditionElement = new XElement("condition");
                    string entityAlias = memberExpression.GetEntityAlias();
                    SetEntityNameForLinkedEntity(entityAlias, conditionElement);
                    conditionElement.SetAttributeValue("attribute", memberExpression.GetColName());
                    conditionElement.SetAttributeValue("operator", "like");
                    object? val = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();
                    conditionElement.SetAttributeValue("value", $"%25{GetStringRepresentation(val)}%25");
                    FilterElement.Add(conditionElement);
                    return node;
            }
        }
        object? obj = Expression.Lambda(node).Compile().DynamicInvoke();
        return Expression.Constant(obj);
    }
    
    void SetEntityNameForLinkedEntity(string entityAlias, XElement conditionElement)
    {
        if (_mainVisitor.FetchElement.Descendants()
            .Any(el => el.Name == "link-entity" && el.Attribute("alias")?.Value == entityAlias))
        {
            conditionElement.SetAttributeValue("entityname", entityAlias);
        }
    }


    void SetType(string type)
    {
        if(_type == type) { return; }
        if(_type == null) {
            _type = type;
            FilterElement.SetAttributeValue("type", type);
            return;
        }
        if(_type != type)
        {
            throw new Exception("Can't mix 'OR' and 'AND' clauses in the same Where filter");
        }
    }

    static string GetStringRepresentation(object? o)
    {
        if (o == null)
        {
            return "null";
        }

        if (o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            o = o.GetType().GetProperty("Value")!.GetValue(o, null)!;
        }
        switch (o)
        {
            case object v when v.GetType().IsEnum:
                return Convert.ToInt32(v).ToString();
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
                return str;
            case object v when v.GetType() == typeof(bool):
                return v.ToString()!.ToLower();
            case object v when v is DateTimeOffset dtoff:
                string strDateTimeOffset = $"{dtoff:O}";
                strDateTimeOffset = strDateTimeOffset
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                return strDateTimeOffset;
            case object v when v is DateTime dtoff:
                string strDateTime = $"{dtoff:O}";
                strDateTime = strDateTime
                    .Replace(":", "%3A")
                    .Replace("+", "%2B");
                return strDateTime;
            case object v when v.GetType() == typeof(Guid):
                return $"{v}";
            case object v when v.GetType() == typeof(int):
                return $"{v}";
            case Id id:
                return $"{id.Guid}";
        }
        throw new Exception($"{o} is not a supported value for comparison in Where clause");
    }
}