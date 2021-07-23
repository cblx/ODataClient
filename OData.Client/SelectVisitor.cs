using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OData.Client
{
    class SelectAndExpandVisitor : ExpressionVisitor
    {

        readonly string filter;
        readonly SelectExpandPair selectExpand;
        ParameterExpression parameter;

        public SelectAndExpandVisitor(bool isRoot, string filter)
        {
            this.filter = filter;
            selectExpand = new SelectExpandPair(isRoot, filter);
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
                if(target is MethodCallExpression callExpression)
                {
                    if(callExpression.Method.Name == "Where")
                    {
                        var filterVisitor = new FilterVisitor(false);
                        filterVisitor.Visit(callExpression.Arguments[1]);
                        subExpandFilter = filterVisitor.Query;
                        target = callExpression.Arguments[0];
                    }
                }
                string field = target.ToString().Substring(paramPrefix.Length).Replace(".", "/");
                switch (node.Method.Name)
                {
                    case "Select":
                        {
                            var subVisitor = new SelectAndExpandVisitor(false, subExpandFilter);
                            subVisitor.Visit(node.Arguments[1]);
                            this.selectExpand.Expand.Add(field, subVisitor.selectExpand);
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
            if (!prop.DeclaringType.IsClass) { return base.VisitMember(node); } // Ex: Nullable
            if (
                (prop.PropertyType.IsValueType 
                || 
                prop.PropertyType == typeof(string))
            )
            {
                IEnumerable<string> splitted = node.ToString().Split('.');
                var pair = selectExpand;
                while(splitted.Count() > 2)
                {
                    string child = splitted.ElementAt(1);
                    if (!pair.Expand.ContainsKey(child))
                    {
                        pair.Expand[child] = new SelectExpandPair(false, null);
                    }
                    pair = pair.Expand[child];
                    splitted = splitted.Skip(1);
                }
                pair.Select.Add(splitted.Last());
            }
            //return node;
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

            public SelectExpandPair(bool isRoot, string filter)
            {
                this.isRoot = isRoot;
                this.filter = filter;
            }
            
            public override string ToString()
            {
                List<string> parts = new List<string>();
                if (Select.Any())
                {
                    parts.Add($"$select={string.Join(",", Select)}");
                }
                if (Expand.Any())
                {
                    parts.Add($"$expand={string.Join(",", Expand.Select(kvp => $"{kvp.Key}({kvp.Value})"))}");
                }
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    parts.Add($"$filter={filter}");
                }

                return string.Join(isRoot ? "&" : ";", parts);
            }
        }

    }
}
