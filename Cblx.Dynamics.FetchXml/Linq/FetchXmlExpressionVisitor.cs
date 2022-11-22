using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using Cblx.Dynamics.Linq;
using OData.Client.Abstractions;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlExpressionVisitor : ExpressionVisitor
{
    public bool IsGroupBy => GroupExpression != null;
    public string? Endpoint { get; private set; }
    public LambdaExpression? GroupExpression { get; private set; }
    public LambdaExpression? GroupByExpression { get; private set; }
    public Dictionary<string, XElement> EntityParametersElements { get; } = new();
    public XElement FetchElement { get; }
    public bool HasFormattedValues { get; private set; }

    public FetchXmlExpressionVisitor()
    {
        FetchElement = new XElement(
            "fetch",
            new XAttribute("mapping", "logical") //,
                                                 //_entityElement
        );
    }

    public string ToFetchXml()
    {
        XElement fetchXmlElement = new XElement(FetchElement);
        ReadProjection(_rootExpression!, fetchXmlElement);
        return fetchXmlElement.ToString();
    }

    public string ToRelativeUrl()
    {
        string fetchXml = ToFetchXml();
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new InvalidOperationException("No Dynamics endpoint found for this expression");
        }
        return $"{Endpoint}?fetchXml={fetchXml}";
    }

    private Expression? _rootExpression = null;

    public override Expression? Visit(Expression? node)
    {
        _rootExpression ??= node;
        return base.Visit(node);
    }

    void ReadProjection(Expression expression, XElement fetchXml)
    {
        if (expression is MethodCallExpression methodCallExpression)
        {
            switch (methodCallExpression.Method)
            {
                case
                {
                    Name: nameof(Queryable.Distinct) 
                          or nameof(Queryable.Take)
                          or nameof(Queryable.Where)
                          or nameof(Queryable.FirstOrDefault)
                } m when m.DeclaringType == typeof(Queryable):
                    ReadProjection(methodCallExpression.Arguments[0], fetchXml);
                    break;
                case
                {
                    Name: nameof(Queryable.Select)
                          or nameof(Queryable.Join)
                          or nameof(Queryable.SelectMany)
                } m when m.DeclaringType == typeof(Queryable):
                    var projectionExpression = (methodCallExpression.Arguments.Last().UnBox() as LambdaExpression)!;
                    ReadAttributesFromProjection(projectionExpression, fetchXml);
                    break;
                case
                {
                    Name: nameof(DynamicsQueryableExpressionExtensions.ProjectTo)
                } m when m.DeclaringType == typeof(DynamicsQueryableExpressionExtensions):
                    // The entity that was used for the query
                    // Allowed cases:
                    // from ConstantExpression
                    // .OriginalEntity.ProjectoTo<OtherEntity>()   
                    // from MethodCallExpression
                    // ex: .OriginalEntity.Where(e => e.Value == 1).ProjectTo<Other>()
                    // or
                    // from o in db.Originals
                    // join etc...
                    // ...select o).ProjectTo<Other>()
                    Type originalEntityType = 
                        methodCallExpression.Arguments[0] is ConstantExpression constantExpression ?
                                                // The constant, directly from IQueryable<TblOriginal>
                                                (constantExpression.Value as IQueryable)!.GetType().GetGenericArguments().First()
                                                // The methodCall,ex: the return type IQueryable<TblOriginal> of a .Select or .Where
                                                : (methodCallExpression.Arguments[0] as MethodCallExpression)!.Method.ReturnType.GetGenericArguments().First();

                    var entityElement = GetOrCreateRootEntityElement(fetchXml, originalEntityType);

                    // The entity used in ProjectTo<Here>()
                    Type entityType = m.GetGenericArguments().First();
                    entityElement.AddEntityAttributesForType(entityType);
                    break;
            }
        }
        else if (expression is ConstantExpression constantExpression &&
                 constantExpression.Value is IQueryable queryable)
        {
            Type entityType = queryable.GetType().GetGenericArguments().First();
            var entityElement = GetOrCreateRootEntityElement(fetchXml, entityType);
            entityElement.AddEntityAttributesForType(entityType);
        }
    }

    XElement GetOrCreateRootEntityElement(XElement fetchXml, Type entityType)
    {
        XElement? entityElement = fetchXml.Descendants().FirstOrDefault(el => el.Name == "entity");
        if (entityElement is null)
        {
            entityElement = new XElement(
                "entity",
                new XAttribute(
                    "name",
                    entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
                    entityType.Name
                )
            );
            Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ??
                       throw new Exception($"No endpoint found for Entity {entityType.Name}");
            fetchXml.Add(entityElement);
        }
        return entityElement;
    }

    int _joinCount = 0;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method)
        {
            case var m when m.DeclaringType == typeof(Queryable):
                return m.Name switch
                {
                    nameof(Queryable.Distinct) => VisitDistinct(node),
                    nameof(Queryable.Take) => VisitTake(node),
                    nameof(Queryable.FirstOrDefault) => VisitFirstOrDefault(node),
                    nameof(Queryable.Select) => VisitSelect(node),
                    // Join/from form
                    nameof(Queryable.SelectMany) => VisitSelectMany(node),
                    nameof(Queryable.Where) => VisitWhere(node),
                    nameof(Queryable.Join) => VisitJoin(node),
                    nameof(Queryable.OrderBy) => VisitOrderBy(node),
                    nameof(Queryable.OrderByDescending) => VisitOrderBy(node, true),
                    nameof(Queryable.GroupBy) => VisitGroupBy(node),
                    _ => base.VisitMethodCall(node),
                };
            default: return base.VisitMethodCall(node);
        }
    }

    Expression VisitFirstOrDefault(MethodCallExpression node)
    {
        if (node.Arguments[0] is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }
        else
        {
            CreateRootEntityFromSource(node);
        }

        FetchElement.SetAttributeValue("top", 1);
        if (node.Arguments.Count > 1)
        {
            LambdaExpression filterExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
            var whereVisitor = new FetchXmlWhereVisitor(this);
            whereVisitor.Visit(filterExpression);
            if (!whereVisitor.IsEmpty)
            {
                FetchElement.Descendants().First().Add(whereVisitor.FilterElement);
            }
        }

        return node;
    }

    Expression VisitTake(MethodCallExpression node)
    {
        Expression fromExpression = node.Arguments[0];
        if (fromExpression is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }

        FetchElement.SetAttributeValue(
            "top",
            new FetchXmlFindCostantVisitor().GetValue(node.Arguments[1])
        );
        return node;
    }

    Expression VisitDistinct(MethodCallExpression node)
    {
        Expression fromExpression = node.Arguments[0];
        if (fromExpression is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }

        FetchElement.SetAttributeValue("distinct", "true");
        return node;
    }

    Expression VisitOrderBy(MethodCallExpression node, bool descending = false)
    {
        if (node.Arguments[0] is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }
        else
        {
            CreateRootEntityFromSource(node);
        }

        LambdaExpression orderByLambdaExpression = (node.Arguments[1].UnBox() as LambdaExpression)!;
        MemberExpression memberExpression = (orderByLambdaExpression.Body as MemberExpression)!;

        XElement entityElement = FetchElement.FindEntityElementByAlias(memberExpression.GetEntityAlias()) ??
                                 FetchElement.Descendants().First();
        var orderElement = new XElement("order");
        orderElement.SetAttributeValue("attribute", memberExpression.GetColName());
        if (descending)
        {
            orderElement.SetAttributeValue("descending", "true");
        }
        entityElement.Add(orderElement);

        return node;
    }

    Expression VisitSelect(MethodCallExpression node)
    {
        Expression fromExpression = node.Arguments[0];
        var projectionExpression = (node.Arguments[1].UnBox() as LambdaExpression)!;
        if (fromExpression is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }
        else
        {
            ParameterExpression parameterExpression = projectionExpression.Parameters[0];
            string entityAlias = parameterExpression.Name!;
            var entityElement = new XElement(
                "entity",
                new XAttribute(
                    "name",
                    parameterExpression.Type.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
                    parameterExpression.Type.Name
                ),
                new XAttribute(
                    "alias",
                    entityAlias
                )
            );
            FetchElement.Add(entityElement);
            if (EntityParametersElements.Count == 0)
            {
                Endpoint = parameterExpression.Type.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ??
                           "endpoint-missing";
            }

            EntityParametersElements[entityAlias] = entityElement;
        }

        return node;
    }

    Expression VisitSelectMany(MethodCallExpression selectManyExpression)
    {
        _joinCount++;

        //Ex:
        //  from p in db.PreviousTable
        //  from t in db.TargetTable.Where(t => t.Id == p.TargetTableId).DefaultIfEmpty()
        var previousTableExpression = selectManyExpression.Arguments[0];
        var whereOrDefaultIfEmptyLambdaExpression = selectManyExpression.Arguments[1].UnBox() as LambdaExpression;
        var whereOrDefaultIfEmptyMethodCallExpression =
            whereOrDefaultIfEmptyLambdaExpression!.Body as MethodCallExpression;
        bool isLeftJoin = whereOrDefaultIfEmptyMethodCallExpression!.Method.Name == "DefaultIfEmpty";
        var whereExpression = isLeftJoin
            ?
            // getting the Where call in Where(...).DefaultIfEmpty()
            whereOrDefaultIfEmptyMethodCallExpression!.Arguments[0] as MethodCallExpression
            // getting the Where call in Where(...)
            : whereOrDefaultIfEmptyMethodCallExpression;

        var targetTableExpression = whereExpression!.Arguments[0];
        var wherePredicateLambdaExpression = whereExpression!.Arguments[1].UnBox() as LambdaExpression;
        var binaryExpression = (whereExpression!.Arguments[1].UnBox() as LambdaExpression)!.Body as BinaryExpression;

        var resultSelectorExpression = selectManyExpression.Arguments[2].UnBox() as LambdaExpression;

        string targetTableAlias = resultSelectorExpression!.Parameters.Last().Name!;
        if (previousTableExpression is MethodCallExpression prevMethodCall)
        {
            Visit(previousTableExpression);
        }

        var memberAccessesByExpression = new[]
        {
            (binaryExpression!.Left as MemberExpression)!,
            (binaryExpression!.Right as MemberExpression)!
        }.ToDictionary(m => m.Expression!);

        // Target table member access is from the where parameter
        MemberExpression targetTableMemberAccess =
            memberAccessesByExpression[wherePredicateLambdaExpression!.Parameters[0]];
        // Previous table member access is the other one
        MemberExpression previousTableMemberAccess = memberAccessesByExpression
            .First(m => m.Value != targetTableMemberAccess)
            .Value;
        // We can trust the previous parameter alias from the binary expression because it comes from external expressions,
        // so it is impossible to change it's alias in the comparison
        string previousTableAlias =
            //It can come from direct parameter
            (previousTableMemberAccess.Expression is ParameterExpression parameterExpression)
                ?
                //Or member of a computed parameter
                parameterExpression.Name!
                : (previousTableMemberAccess.Expression as MemberExpression)!.Member.Name;

        XElement previousTableElement = new FindOrCreateXElementVisitor(
            this,
            previousTableAlias,
            FetchElement
        ).FindOrCreateXElement(previousTableExpression);

        XElement targetTableElement = new FindOrCreateXElementVisitor(
            this,
            targetTableAlias,
            previousTableElement
        ).FindOrCreateXElement(targetTableExpression);
        if (isLeftJoin)
        {
            targetTableElement.SetAttributeValue("link-type", "outer");
        }

        targetTableElement.SetAttributeValue("from", targetTableMemberAccess.GetColName());
        targetTableElement.SetAttributeValue("to", previousTableMemberAccess.GetColName());
        return selectManyExpression;
    }


    Expression VisitWhere(MethodCallExpression node)
    {
        if (node.Arguments[0] is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }
        else
        {
            CreateRootEntityFromSource(node);
        }

        LambdaExpression filterExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
        var whereVisitor = new FetchXmlWhereVisitor(this);
        whereVisitor.Visit(filterExpression);
        if (!whereVisitor.IsEmpty)
        {
            FetchElement.Descendants().First().Add(whereVisitor.FilterElement);
        }

        return node;
    }

    void CreateRootEntityFromSource(MethodCallExpression node)
    {
        ConstantExpression? constantExpression = (node.Arguments[0] as ConstantExpression)!;
        Type entityType = constantExpression.Value!.GetType().GetGenericArguments()[0];
        var entityElement = new XElement(
            "entity",
            new XAttribute(
                "name",
                entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
                entityType.Name
            )
        );
        FetchElement.Add(entityElement);
        Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ?? throw new Exception($@"ODataEndointAttribute not found for {entityType.Name}");
    }

    Expression VisitJoin(MethodCallExpression node)
    {
        // 5 arguments:
        // Index    Meaning
        // 0    -   The left queryable OR previous join clause
        // 1    -   The right queryable
        // 2    -   The left key accessor
        // 3    -   The right key accessor
        // 4    -   Select/Projection

        _joinCount++;
        Expression leftQueryableOrPreviousJoinCall = node.Arguments[0];
        Expression leftKeyAccessor = node.Arguments[2];
        Expression rightKeyAccessor = node.Arguments[3];

        if (leftQueryableOrPreviousJoinCall is MethodCallExpression previousJoinCallExpression)
        {
            Visit(previousJoinCallExpression);
        }

        GetOrCreateXElementsForJoin(rightKeyAccessor, leftKeyAccessor);
        return node;
    }

    Expression VisitGroupBy(MethodCallExpression node)
    {
        Expression fromExpression = node.Arguments[0];
        GroupByExpression = (node.Arguments[1].UnBox() as LambdaExpression)!;
        GroupExpression = (node.Arguments[2].UnBox() as LambdaExpression)!;

        if (fromExpression is MethodCallExpression methodCallExpression)
        {
            Visit(methodCallExpression);
        }
        else
        {
            ParameterExpression parameterExpression = GroupByExpression.Parameters[0];
            string entityAlias = parameterExpression.Name!;
            var entityElement = new XElement(
                "entity",
                new XAttribute(
                    "name",
                    parameterExpression.Type.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
                    parameterExpression.Type.Name
                ),
                new XAttribute(
                    "alias",
                    entityAlias
                )
            );
            FetchElement.Add(entityElement);
            if (EntityParametersElements.Count == 0)
            {
                Endpoint = parameterExpression.Type.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ??
                           "endpoint-missing";
            }

            EntityParametersElements[entityAlias] = entityElement;
        }

        FetchElement.SetAttributeValue("aggregate", "true");
        ReadAttributesFromProjection(GroupByExpression, FetchElement, groupBy: true);
        return node;
    }

    void ReadAttributesFromProjection(LambdaExpression projectionExpression, XElement fetchXmlElement,
        bool groupBy = false)
    {
        ParameterExpression parameterExpression = projectionExpression.Parameters[0];
        bool isGroupByQuery = parameterExpression.Type.IsGenericType
                              && parameterExpression.Type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        if (isGroupByQuery)
        {
            var visitor = new FetchXmlGroupByProjectionVisitor(fetchXmlElement, GroupExpression!);
            visitor.Visit(projectionExpression);
        }
        else
        {
            var visitor = new FetchXmlSelectProjectionVisitor(fetchXmlElement, groupBy);
            visitor.Visit(projectionExpression);
            if (visitor.HasFormattedValues)
            {
                HasFormattedValues = true;
            }
        }
    }

    XElement GetOrCreateXElementsForJoin(
        Expression exp,
        Expression? leftExpression = null
    )
    {
        XElement parent = leftExpression != null
            ? GetOrCreateXElementsForJoin(leftExpression)
            : FetchElement;

        LambdaExpression lambda = ((exp as UnaryExpression)!.Operand as LambdaExpression)!;
        MemberExpression memberExpression = (lambda.Body as MemberExpression)!;


        string? entityAlias = null;
        Type? entityType = null;

        // Not from previous join: e.Id
        if (memberExpression.Expression is ParameterExpression parameterExpression)
        {
            entityAlias = parameterExpression.Name!;
            entityType = parameterExpression.Type;
        }

        // When exists a previous join: <ResultOfPreviousJoin>.e.Id
        if (memberExpression.Expression is MemberExpression resultOfPreviousJoinParameterExpression)
        {
            entityAlias = resultOfPreviousJoinParameterExpression.Member.Name;
            entityType = resultOfPreviousJoinParameterExpression.Type;
        }

        if (entityType == null)
        {
            throw new ArgumentException("Expression must be parameter or member", nameof(exp));
        }

        if (entityAlias == null)
        {
            throw new ArgumentException("Expression must be parameter or member", nameof(exp));
        }

        if (EntityParametersElements.ContainsKey(entityAlias))
        {
            return EntityParametersElements[entityAlias];
        }

        bool isRootEntity = parent == FetchElement;
        if (isRootEntity)
        {
            Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint;
        }

        var entityElement = new XElement(
            isRootEntity ? "entity" : "link-entity",
            new XAttribute(
                "name",
                entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ?? entityType.Name
            )
        );
        if (isRootEntity is false)
        {
            entityElement.Add(
                new XAttribute(
                    "alias",
                    entityAlias
                )
            );
            LambdaExpression leftLambda = ((leftExpression as UnaryExpression)!.Operand as LambdaExpression)!;
            MemberExpression leftMemberExpression = (leftLambda.Body as MemberExpression)!;

            entityElement.Add(new XAttribute(
                "to",
                leftMemberExpression.GetColName()
            ));
            entityElement.Add(new XAttribute(
                "from",
                memberExpression.GetColName()
            ));
        }

        parent.Add(entityElement);

        EntityParametersElements[entityAlias] = entityElement;
        return entityElement;
    }

    class FindOrCreateXElementVisitor : ExpressionVisitor
    {
        private readonly FetchXmlExpressionVisitor _mainVisitor;
        private XElement? _createdElement = null;
        private readonly XElement _parentElement;
        private readonly string _alias;

        public FindOrCreateXElementVisitor(
            FetchXmlExpressionVisitor mainVisitor,
            string alias,
            XElement parentElement
        )
        {
            _alias = alias;
            _mainVisitor = mainVisitor;
            _parentElement = parentElement;
        }

        public XElement FindOrCreateXElement(Expression expression)
        {
            Visit(expression);
            if (_createdElement == null)
            {
                throw new ArgumentException("Could not find entity for queryable");
            }

            return _createdElement;
        }

        // IQueryable inside expressions
        protected override Expression VisitMember(MemberExpression node)
        {
            Type propertyType = (node.Member as PropertyInfo)!.PropertyType;
            if (propertyType.IsAssignableTo(typeof(IQueryable)))
            {
                Type entityType = propertyType.GetGenericArguments()[0];
                CreateForType(entityType);
                return node;
            }

            return base.VisitMember(node);
        }

        // From parameter
        protected override Expression VisitParameter(ParameterExpression node)
        {
            CreateForType(node.Type);
            return base.VisitParameter(node);
        }


        // Directly from an IQueryable<> instance
        protected override Expression VisitConstant(ConstantExpression node)
        {
            Type entityType = node.Value!.GetType().GetGenericArguments()[0];
            CreateForType(entityType);
            return node;
        }

        void CreateForType(Type entityType)
        {
            if (_mainVisitor.EntityParametersElements.ContainsKey(_alias))
            {
                _createdElement = _mainVisitor.EntityParametersElements[_alias];
                return;
            }

            bool isRoot = _parentElement.Name == "fetch";
            if (isRoot)
            {
                _mainVisitor.Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint;
            }

            var entityElement = new XElement(
                isRoot ? "entity" : "link-entity",
                new XAttribute(
                    "name",
                    entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ?? entityType.Name
                ),
                new XAttribute(
                    "alias",
                    _alias
                )
            );
            _parentElement.Add(entityElement);
            _mainVisitor.EntityParametersElements[_alias] = entityElement;
            _createdElement = entityElement;
        }
    }
}