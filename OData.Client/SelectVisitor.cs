using Cblx.OData.Client.Abstractions.Ids;
using System.Linq.Expressions;
using System.Reflection;
namespace OData.Client;
class SelectAndExpandVisitor : ExpressionVisitor
{

    readonly SelectExpandPair selectExpand;
    ParameterExpression parameter;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isRoot"></param>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="top"></param>
    /// <param name="preSelectFields">Fields that for some reason are necessary in the $select clause even if not explictly required</param>
    public SelectAndExpandVisitor(
        bool isRoot, 
        string filter,
        string? orderBy = null,
        string? top = null,
        IEnumerable<string> preSelectFields = null
    )
    {
        selectExpand = new SelectExpandPair(
            isRoot, 
            filter, 
            orderBy, 
            top,
            preSelectFields
        );
    }

    public override Expression Visit(Expression node)
    {
        if(node is LambdaExpression lambdaExpression)
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
            Expression target = node.Object ?? node.Arguments[0];
            string subExpandFilter = null;
            string? subExpandOrderBy = null;
            string? subExpandTop = null;
            List<string> preSelect = new();
            while(target is MethodCallExpression callExpression 
                && 
                new string[]
                {
                    "Where",
                    "OrderBy",
                    "OrderByDescending",
                    "Take"
                }.Contains(callExpression.Method.Name)
            )
            {
                if(callExpression.Method.Name == "Where")
                {
                    var filterVisitor = new FilterVisitor(false);
                    filterVisitor.Visit(callExpression.Arguments[1]);
                    subExpandFilter = subExpandFilter == null ? filterVisitor.Query : $"{subExpandFilter} and {filterVisitor.Query}";
                    preSelect.AddRange(filterVisitor.VisitedFields);
                }
                if(callExpression.Method.Name == "OrderBy")
                {
                    var filterVisitor = new FilterVisitor(false);
                    filterVisitor.Visit(callExpression.Arguments[1]);
                    preSelect.Add(filterVisitor.Query); // Query is just a field acessor
                    subExpandOrderBy = filterVisitor.Query;
                }
                if (callExpression.Method.Name == "OrderByDescending")
                {
                    var filterVisitor = new FilterVisitor(false);
                    filterVisitor.Visit(callExpression.Arguments[1]);
                    preSelect.Add(filterVisitor.Query); // Query is just a field acessor
                    subExpandOrderBy = filterVisitor.Query + " desc";
                }
                if (callExpression.Method.Name == "Take")
                {
                    subExpandTop = callExpression.Arguments[1].ToString();
                }
                target = callExpression.Arguments[0];
            }
            //string field = target.ToString().Substring(paramPrefix.Length).Replace(".", "/");
            switch (node.Method.Name)
            {
                case "Select":
                    {
                        var subVisitor = new SelectAndExpandVisitor(false, subExpandFilter, subExpandOrderBy, subExpandTop, preSelect);
                        subVisitor.Visit(node.Arguments[1]);
                        this.selectExpand.Expand.Add(
                            (target as MemberExpression).GetFieldName(), 
                            subVisitor.selectExpand
                        );
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
                    // Ignora e retorna o argumento, Execução no client
                    Expression arg = 
                        node.Object // Metodo da instancia, ex: ToString()
                        ?? 
                        node.Arguments[0]; // Extension methods
                    Visit(arg);
                    return node;
                    //throw new Exception($"Método não suportado na projeção {node.Method.Name}");
            }
        }
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        string str = node.ToString();
        string paramPrefix = parameter.Name + ".";
        if (!str.StartsWith(paramPrefix)) { return base.VisitMember(node); }
        var prop = node.Member as PropertyInfo;
        if(prop == null) { return base.VisitMember(node); }
        if (!prop.DeclaringType.IsClass || prop.DeclaringType.IsAssignableTo(typeof(Id))) { return base.VisitMember(node); } // Ex: Nullable
        if (
            (prop.PropertyType.IsValueType 
            || 
            prop.PropertyType == typeof(string))
            ||
            prop.PropertyType.BaseType == typeof(Id)
        )
        {
            SelectExpandPair pair = selectExpand;
            Stack<MemberExpression> memberStack = node.CreateMemberParentsStack();
            while (memberStack.TryPop(out MemberExpression memberExpression))
            {
                string extendFieldName = memberExpression.GetFieldName();
                if (!pair.Expand.ContainsKey(extendFieldName))
                {
                    pair.Expand[extendFieldName] = new SelectExpandPair(false, null);
                }
                pair = pair.Expand[extendFieldName];
            }

            string field = node.GetFieldName();
            // Supporting OData annotations
            if (field.Contains("@"))
            {
                field = field.Split('@')[0];
                // This may happend for root annotations
                if (string.IsNullOrWhiteSpace(field)) { return base.VisitMember(node); }
            }
            pair.Select.Add(field);
        }
        return base.VisitMember(node);
    }

  

    public override string ToString()
    {
        return selectExpand.ToString();
    }

    class SelectExpandPair
    {
        public SortedSet<string> Select = new SortedSet<string>();

        public SortedDictionary<string, SelectExpandPair> Expand = new SortedDictionary<string, SelectExpandPair>();

        readonly bool isRoot;

        string filter;
        string? orderBy;
        string? top;

        public SelectExpandPair(
            bool isRoot,
            string filter,
            string? orderBy = null,
            string? top = null,
            IEnumerable<string> preSelectFields = null
        )
        {
            this.isRoot = isRoot;
            this.filter = filter;
            this.orderBy = orderBy;
            this.top = top;
            if(preSelectFields != null)
            {
                foreach(var field in preSelectFields)
                {
                    Select.Add(field);
                }
            }
        }
        
        public override string ToString()
        {
            List<string> parts = new List<string>();
            if (Select.Any())
            {
                parts.Add($"$select={string.Join(",", Select)}");
            }
            if (!string.IsNullOrWhiteSpace(filter))
            {
                parts.Add($"$filter={filter}");
            }
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                parts.Add($"$orderby={orderBy}");
            }
            if (!string.IsNullOrWhiteSpace(top))
            {
                parts.Add($"$top={top}");
            }
            if (Expand.Any())
            {
                parts.Add($"$expand={string.Join(",", Expand.Select(kvp => $"{kvp.Key}({kvp.Value})"))}");
            }

            return string.Join(isRoot ? "&" : ";", parts);
        }
    }

}
