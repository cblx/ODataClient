using System.Linq.Expressions;
using System.Reflection;
using Cblx.OData.Client.Abstractions.Ids;

namespace OData.Client;

internal class SelectAndExpandVisitor : ExpressionVisitor
{
    private readonly SelectExpandPair _selectExpand;
    private ParameterExpression _parameter;

    /// <summary>
    /// </summary>
    /// <param name="isRoot"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="top"></param>
    /// <param name="preSelectFields">
    ///     Fields that for some reason are necessary in the $select clause even if not explictly
    ///     required
    /// </param>
    public SelectAndExpandVisitor(
        bool isRoot,
        string? filter,
        string? orderBy = null,
        string? top = null,
        IEnumerable<string>? preSelectFields = null
    )
    {
        _selectExpand = new SelectExpandPair(
            isRoot,
            filter,
            orderBy,
            top,
            preSelectFields
        );
    }

    public override Expression? Visit(Expression? node)
    {
        if (node is LambdaExpression lambdaExpression) _parameter = lambdaExpression.Parameters[0];
        return base.Visit(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var str = node.ToString();
        var paramPrefix = _parameter.Name + ".";
        if (str.StartsWith(paramPrefix))
        {
            var target = node.Object ?? node.Arguments[0];
            string? subExpandFilter = null;
            string? subExpandOrderBy = null;
            string? subExpandTop = null;
            List<string> preSelect = new();
            while (target is MethodCallExpression callExpression
                   &&
                   new[]
                   {
                       "Where",
                       "OrderBy",
                       "OrderByDescending",
                       "Take"
                   }.Contains(callExpression.Method.Name)
                  )
            {
                if (callExpression.Method.Name == "Where")
                {
                    var filterVisitor = new FilterVisitor(false);
                    filterVisitor.Visit(callExpression.Arguments[1]);
                    subExpandFilter = subExpandFilter == null
                        ? filterVisitor.Query
                        : $"{subExpandFilter} and {filterVisitor.Query}";
                    preSelect.AddRange(filterVisitor.VisitedFields);
                }

                if (callExpression.Method.Name == "OrderBy")
                {
                    var filterVisitor = new FilterVisitor(false);
                    filterVisitor.Visit(callExpression.Arguments[1]);
                    preSelect.Add(filterVisitor.Query); // Query is just a field accessor
                    subExpandOrderBy = filterVisitor.Query;
                }

                if (callExpression.Method.Name == "OrderByDescending")
                {
                    var filterVisitor = new FilterVisitor(false);
                    filterVisitor.Visit(callExpression.Arguments[1]);
                    preSelect.Add(filterVisitor.Query); // Query is just a field accessor
                    subExpandOrderBy = filterVisitor.Query + " desc";
                }

                if (callExpression.Method.Name == "Take") subExpandTop = callExpression.Arguments[1].ToString();
                target = callExpression.Arguments[0];
            }

            //string field = target.ToString().Substring(paramPrefix.Length).Replace(".", "/");
            switch (node.Method.Name)
            {
                case "Select":
                    {
                        var subVisitor = new SelectAndExpandVisitor(false, subExpandFilter, subExpandOrderBy, subExpandTop,
                            preSelect);
                        subVisitor.Visit(node.Arguments[1]);
                        string expandingFieldName = (target as MemberExpression)!.GetFieldName();
                        if (_selectExpand.Expand.ContainsKey(expandingFieldName))
                        {
                            //var existingExpand = _selectExpand.Expand[expandingFieldName];
                            //existingExpand.MergeFrom(_selectExpand);
                        }
                        else
                        {
                            _selectExpand.Expand.Add(expandingFieldName, subVisitor._selectExpand);
                        }
                        return node;
                    }
                //case "Where":
                //    {
                //        var filterVisitor = new FilterVisitor(false);
                //        filterVisitor.Visit(node.Arguments[1]);
                //        return node;
                //    }
                //case "FirstOrDefault":
                //    {
                //        var subVisitor = new SelectVisitor();
                //        subVisitor.Visit(node.Arguments[1]);
                //        this.selectExpand.Expand.Add(field, subVisitor.selectExpand);
                //        return node;
                //    }
                default:
                    // Ignores and returns the argument (client execution)
                    var arg =
                        node.Object // instance method, ex: ToString()
                        ??
                        node.Arguments[0]; // Extension methods
                    Visit(arg);
                    return node;
            }
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var str = node.ToString();
        var paramPrefix = _parameter.Name + ".";
        if (!str.StartsWith(paramPrefix)) return base.VisitMember(node);
        var prop = node.Member as PropertyInfo;
        if (prop == null) return base.VisitMember(node);
        if (!prop.DeclaringType!.IsClass || prop.DeclaringType.IsAssignableTo(typeof(Id))) return base.VisitMember(node);
        if (
            prop.PropertyType.IsValueType
            ||
            prop.PropertyType == typeof(string)
            ||
            prop.PropertyType.BaseType == typeof(Id)
        )
        {
            var pair = _selectExpand;
            var memberStack = node.CreateMemberParentsStack();
            while (memberStack.TryPop(out var memberExpression))
            {
                var extendFieldName = memberExpression.GetFieldName();
                if (!pair.Expand.ContainsKey(extendFieldName))
                    pair.Expand[extendFieldName] = new SelectExpandPair(false, null);
                pair = pair.Expand[extendFieldName];
            }

            var field = node.GetFieldName();
            // Supporting OData annotations
            if (field.Contains("@"))
            {
                field = field.Split('@')[0];
                // This may happened for root annotations
                if (string.IsNullOrWhiteSpace(field)) return base.VisitMember(node);
            }

            pair.Select.Add(field);
        }

        return base.VisitMember(node);
    }


    public override string ToString()
    {
        return _selectExpand.ToString();
    }

    private class SelectExpandPair
    {
        private readonly string? _filter;
        private readonly string? _orderBy;
        private readonly string? _top;

        private readonly bool _isRoot;

        public readonly SortedDictionary<string, SelectExpandPair> Expand = new();
        public readonly SortedSet<string> Select = new();

        public SelectExpandPair(
            bool isRoot,
            string? filter,
            string? orderBy = null,
            string? top = null,
            IEnumerable<string>? preSelectFields = null
        )
        {
            this._isRoot = isRoot;
            _filter = filter;
            _orderBy = orderBy;
            _top = top;
            if (preSelectFields != null)
                foreach (var field in preSelectFields)
                    Select.Add(field);
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (Select.Any()) parts.Add($"$select={string.Join(",", Select)}");
            if (!string.IsNullOrWhiteSpace(_filter)) parts.Add($"$filter={_filter}");
            if (!string.IsNullOrWhiteSpace(_orderBy)) parts.Add($"$orderby={_orderBy}");
            if (!string.IsNullOrWhiteSpace(_top)) parts.Add($"$top={_top}");
            if (Expand.Any()) parts.Add($"$expand={string.Join(",", Expand.Select(kvp => $"{kvp.Key}({kvp.Value})"))}");

            return string.Join(_isRoot ? "&" : ";", parts);
        }
    }
}