using Cblx.OData.Client.Abstractions;
using OData.Client;
using System.Linq.Expressions;

namespace Cblx.Dynamics.OData.Linq;

public class ODataExpressionVisitor : ExpressionVisitor
{
    private Expression? _rootExpression = null;
    private readonly SortedDictionary<string, string> _queryString = new();
    private readonly IDynamicsMetadataProvider _metadataProvider;

    public ODataExpressionVisitor(IDynamicsMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    public override Expression? Visit(Expression? node)
    {
        _rootExpression ??= node;
        return base.Visit(node);
    }

    public string ToRelativeUrl()
    {
        if (_rootExpression is null) { throw new InvalidOperationException("The expression should be visited first"); }
        var selectAndExpand = CreateSelectAndExpandFromProjection(_rootExpression);
        IEnumerable<string> options = _queryString.Select(kvp => $"{kvp.Key}={kvp.Value}");
        options = new[] { selectAndExpand.SelectAndExpand }.Union(options);
        return $"{selectAndExpand.Endpoint}?{string.Join("&", options)}";
    }

    (string Endpoint, string SelectAndExpand) CreateSelectAndExpandFromProjection(Expression expression)
    {
        if (expression is MethodCallExpression methodCallExpression)
        {
            switch (methodCallExpression?.Method.Name)
            {
                // db.Entities...etc...Take();
                // db.Entities...etc...Where(...);
                // db.Entities...etc...FirstOrDefault(...);
                // Find previous sentence through recursion
                case "Take":
                case "Where":
                case "FirstOrDefault":
                    return CreateSelectAndExpandFromProjection(methodCallExpression.Arguments[0]);
                case "Select":
                    {
                        string endpoint = "";
                        // From constant Queryable
                        if(methodCallExpression.Arguments.First() is ConstantExpression constantExpression && constantExpression.Value is IQueryable queryable)
                        {
                            var entityType = queryable.GetType().GetGenericArguments().First();
                            endpoint = _metadataProvider.GetEndpoint(entityType);
                        // From resultando Queryable (like a .Where)
                        }else if(methodCallExpression.Arguments.First() is MethodCallExpression fromCallExpression && fromCallExpression.Method.IsGenericMethod)
                        {
                            var entityType = fromCallExpression.Method.GetGenericArguments()[0];
                            endpoint = _metadataProvider.GetEndpoint(entityType);
                        }

                        // Interpret the lambda projection arg in db.Entities ...etc.. Extensions.Select(queryable, arg);
                        var projectionExpression = (methodCallExpression.Arguments.Last().UnBox() as LambdaExpression)!;
                        var selectAndExpandVisitor = new SelectAndExpandVisitor(true, null);
                        selectAndExpandVisitor.Visit(projectionExpression);
                        var select = selectAndExpandVisitor.ToString();
                        return (endpoint, select);
                    }
                default: throw new InvalidOperationException($"{methodCallExpression?.Method.Name} is not supported");
            }
        }
        else if (expression is ConstantExpression constantExpression && constantExpression.Value is IQueryable queryable)
        {
            var entityType = queryable.GetType().GetGenericArguments().First();
            var endpoint = _metadataProvider.GetEndpoint(entityType);
            return (endpoint, entityType.ToSelectString());
        }
        throw new InvalidOperationException($"Expression {expression} is not supported");
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            //"Distinct" => VisitDistinct(node),
            "Take" => VisitTake(node),
            "FirstOrDefault" => VisitFirstOrDefault(node),
            "Select" => VisitSelect(node), // manage by SelectAndExpandVisitor
            "Where" => VisitWhere(node), // managed by FilterVisitor
            //"OrderBy" => VisitOrderBy(node),
            //"OrderByDescending" => VisitOrderBy(node, true),
            //"GroupBy" => VisitGroupBy(node),
            _ => throw new Exception($"Unsupported method {node.Method.Name}") //base.VisitMethodCall(node),
        };
    }
    
    Expression VisitFirstOrDefault(MethodCallExpression node)
    {
        _queryString["$top"] = "1";
        if (node.Arguments.Count > 1)
        {
            LambdaExpression filterExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
            var filterVisitor = new FilterVisitor(false);
            filterVisitor.Visit(filterExpression);
            _queryString["$filter"] = filterVisitor.Query;
        }
        return base.VisitMethodCall(node);
    }

    Expression VisitTake(MethodCallExpression node)
    {
        object? top = Expression.Lambda(node.Arguments[1]).Compile().DynamicInvoke();
        _queryString["$top"] = top?.ToString() ?? "";
        return base.VisitMethodCall(node);
    }
  
    Expression VisitSelect(MethodCallExpression node)
    {
        Expression fromExpression = node.Arguments[0];
        if (fromExpression is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }
        //LambdaExpression selectExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
        //var filterVisitor = new SelectAndExpandVisitor(true, null);
        //filterVisitor.Visit(selectExpression);
        return node;
    }

    readonly Stack<string> _filters = new();

    Expression VisitWhere(MethodCallExpression node)
    {
        LambdaExpression filterExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
        var filterVisitor = new FilterVisitor(false);
        filterVisitor.Visit(filterExpression);
        _filters.Push(filterVisitor.Query);
        _queryString["$filter"] = String.Join(" and ", _filters);
        // Other Where methods should be called in sequence
        Expression fromExpression = node.Arguments[0];
        if (fromExpression is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }
        return node;
    }
}