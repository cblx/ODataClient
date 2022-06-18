using System.Linq.Expressions;

namespace Cblx.Dynamics.OData.Linq;

public class ODataExpressionVisitor : ExpressionVisitor
{

    private Expression? _rootExpression = null;
    public string? Endpoint { get; private set; }

    private SortedDictionary<string, string> _queryString = new();

    public override Expression? Visit(Expression? node)
    {
        _rootExpression ??= node;
        return base.Visit(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        Type? type = node.Value?.GetType();
        // Should exist just one access to a constant of ODataQueryable
        if(type != null && type.IsGenericType && type?.GetGenericTypeDefinition() == typeof(ODataQueryable<>))
        {
            Type entityType = type.GenericTypeArguments[0];
            Endpoint = entityType.GetEndpointName();
        }
        return base.VisitConstant(node);
    }

    //protected override Expression VisitMember(MemberExpression node)
    //{
    //    if(node.Expression is ParameterExpression parameterExpression && parameterExpression.Type.IsDynamicsEntity())
    //    {
    //        _select.Add(node.Member.GetColName());
    //    }
    //    return base.VisitMember(node);
    //}

    //public bool IsGroupBy => GroupExpression != null;
    //public LambdaExpression? GroupExpression { get; private set; }
    //public LambdaExpression? GroupByExpression { get; private set; }
    //public Dictionary<string, XElement> EntityParametersElements { get; } = new();
    //public XElement FetchElement { get; }

    //public ODataExpressionVisitor()
    //{
    //    FetchElement = new XElement(
    //        "fetch",
    //        new XAttribute("mapping", "logical") //,
    //        //_entityElement
    //    );
    //}

    //public string ToFetchXml()
    //{
    //    XElement fetchXmlElement = new XElement(FetchElement);
    //    ReadProjection(_rootExpression!, fetchXmlElement);
    //    return fetchXmlElement.ToString();
    //}

    public string ToRelativeUrl()
    {
        if(_rootExpression is null) { throw new Exception("The expression should be visited first"); }
        string select = CreateSelectFromProjection(_rootExpression);
        var queryString = new SortedDictionary<string, string>(_queryString);
        queryString.Add("$select", select);
        return $"{Endpoint}?{string.Join("&", queryString.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
        //string fetchXml = ToFetchXml();
        //if (string.IsNullOrWhiteSpace(Endpoint))
        //{
        //    throw new Exception("No Dynamics endpoint found for this expression");
        //}
        //return $"{Endpoint}?fetchXml={fetchXml}";
    }

    //private Expression? _rootExpression = null;

    //public override Expression? Visit(Expression? node)
    //{
    //    _rootExpression ??= node;
    //    return base.Visit(node);
    //}

    string CreateSelectFromProjection(Expression expression)
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
                    return CreateSelectFromProjection(methodCallExpression.Arguments[0]);
                case "Select":
                    {
                        // Interpret the lambda projection arg in db.Entities...etc..Extensions.Select(queryable, arg);
                        var projectionExpression = (methodCallExpression.Arguments.Last().UnBox() as LambdaExpression)!;
                        var projectionVisitor = new ProjectionVisitor();
                        projectionVisitor.Visit(projectionExpression);
                        return projectionVisitor.Select;
                    }
                default: throw new Exception($"{methodCallExpression?.Method.Name} is not supported");
            }
        }
        else if (expression is ConstantExpression constantExpression && constantExpression.Value is IQueryable queryable)
        {
            Type entityType = queryable.GetType().GetGenericArguments().First();
            Endpoint = entityType.GetEndpointName();
            return entityType.ToSelectString();
        }
        throw new Exception($"Expression {expression} is not supported");
    }

    //int joinCount = 0;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            //"Distinct" => VisitDistinct(node),
            "Take" => VisitTake(node),
            "FirstOrDefault" => VisitFirstOrDefault(node),
            "Select" => base.VisitMethodCall(node),
            // Join/from form
            //"SelectMany" => VisitSelectMany(node),
            //"Where" => VisitWhere(node),
            //"Join" => VisitJoin(node),
            //"OrderBy" => VisitOrderBy(node),
            //"OrderByDescending" => VisitOrderBy(node, true),
            //"GroupBy" => VisitGroupBy(node),
            _ => throw new Exception($"Unsupported method {node.Method.Name}") //base.VisitMethodCall(node),
        };
    }

    //Expression VisitFirstOrDefault(MethodCallExpression node)
    //{
    //    if (node.Arguments[0] is MethodCallExpression methodCallExpression)
    //    {
    //        Visit(methodCallExpression);
    //    }
    //    else
    //    {
    //       CreateRootEntityFromSource(node);
    //    }

    //    FetchElement.SetAttributeValue("top", 1);
    //    if (node.Arguments.Count > 1)
    //    {
    //        LambdaExpression filterExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
    //        var whereVisitor = new FetchXmlWhereVisitor(this);
    //        whereVisitor.Visit(filterExpression);
    //        if (!whereVisitor.IsEmpty)
    //        {
    //            FetchElement.Descendants().First().Add(whereVisitor.FilterElement);
    //        }
    //    }

    //    return node;
    //}

    Expression VisitFirstOrDefault(MethodCallExpression node)
    {
        _queryString["$top"] = "1";
        return base.VisitMethodCall(node);
    }

    Expression VisitTake(MethodCallExpression node)
    {
        object? top = Expression.Lambda(node.Arguments[1]).Compile().DynamicInvoke();
        _queryString["$top"] = top?.ToString() ?? "";
        return base.VisitMethodCall(node);
    }

    //Expression VisitDistinct(MethodCallExpression node)
    //{
    //    Expression fromExpression = node.Arguments[0];
    //    if (fromExpression is MethodCallExpression methodCallExpression)
    //    {
    //        Visit(methodCallExpression);
    //    }

    //    FetchElement.SetAttributeValue("distinct", "true");
    //    return node;
    //}

    //Expression VisitOrderBy(MethodCallExpression node, bool descending = false)
    //{
    //    if (node.Arguments[0] is MethodCallExpression methodCallExpression)
    //    {
    //        Visit(methodCallExpression);
    //    }
    //    else
    //    {
    //        CreateRootEntityFromSource(node);
    //    }


    //    //Expression fromExpression = node.Arguments[0];
    //    // if (fromExpression is MethodCallExpression methodCallExpression)
    //    // {
    //    //     Visit(methodCallExpression);
    //    // }

    //    //var findOrCreateXElementVisitor = new FindOrCreateXElementWithAliasVisitor(this, FetchElement);
    //    //XElement entityElement = findOrCreateXElementVisitor.FindOrCreate(node.Arguments[1]);
    //    //XElement entityElement = FetchElement.FindEntityElementByAlias()
    //    LambdaExpression orderByLambdaExpression = (node.Arguments[1].UnBox() as LambdaExpression)!;
    //    MemberExpression memberExpression = (orderByLambdaExpression.Body as MemberExpression)!;

    //    XElement entityElement = FetchElement.FindEntityElementByAlias(memberExpression.GetEntityAlias()) ??
    //                             FetchElement.Descendants().First();
    //    var orderElement = new XElement("order");
    //    orderElement.SetAttributeValue("attribute", memberExpression.GetColName());
    //    if (descending)
    //    {
    //        orderElement.SetAttributeValue("descending", "true");
    //    }
    //    entityElement.Add(orderElement);

    //    return node;
    //}

    //Expression VisitSelect(MethodCallExpression node)
    //{
    //    var projectionExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
    //    if (projectionExpression is NewExpression newExpression)
    //    {

    //    }

    //    //Expression fromExpression = node.Arguments[0];
    //    //var projectionExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
    //    //if (fromExpression is MethodCallExpression methodCallExpression)
    //    //{
    //    //    Visit(methodCallExpression);
    //    //}
    //    //else
    //    //{
    //    //    ParameterExpression parameterExpression = projectionExpression.Parameters[0];
    //    //    //string entityAlias = parameterExpression.Name!;
    //    //    //var entityElement = new XElement(
    //    //    //    "entity",
    //    //    //    new XAttribute(
    //    //    //        "name",
    //    //    //        parameterExpression.Type.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
    //    //    //        parameterExpression.Type.Name
    //    //    //    ),
    //    //    //    new XAttribute(
    //    //    //        "alias",
    //    //    //        entityAlias
    //    //    //    )
    //    //    //);
    //    //    //FetchElement.Add(entityElement);
    //    //    //if (EntityParametersElements.Count == 0)
    //    //    //{
    //    //    //    Endpoint = parameterExpression.Type.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ??
    //    //    //               "endpoint-missing";
    //    //    //}

    //    //    //EntityParametersElements[entityAlias] = entityElement;
    //    //}

    //    return node;
    //}

    //Expression VisitSelectMany(MethodCallExpression selectManyExpression)
    //{
    //    bool isFirstJoin = joinCount == 0;
    //    joinCount++;

    //    //Ex:
    //    //  from p in db.PreviousTable
    //    //  from t in db.TargetTable.Where(t => t.Id == p.TargetTableId).DefaultIfEmpty()
    //    var previousTableExpression = selectManyExpression.Arguments[0];
    //    var whereOrDefaultIfEmptyLambdaExpression = selectManyExpression.Arguments[1].UnBox() as LambdaExpression;
    //    var whereOrDefaultIfEmptyMethodCallExpression =
    //        whereOrDefaultIfEmptyLambdaExpression!.Body as MethodCallExpression;
    //    bool isLeftJoin = whereOrDefaultIfEmptyMethodCallExpression!.Method.Name == "DefaultIfEmpty";
    //    var whereExpression = isLeftJoin
    //        ?
    //        // getting the Where call in Where(...).DefaultIfEmpty()
    //        whereOrDefaultIfEmptyMethodCallExpression!.Arguments[0] as MethodCallExpression
    //        // getting the Where call in Where(...)
    //        : whereOrDefaultIfEmptyMethodCallExpression;

    //    var targetTableExpression = whereExpression!.Arguments[0];
    //    var wherePredicateLambdaExpression = whereExpression!.Arguments[1].UnBox() as LambdaExpression;
    //    var binaryExpression = (whereExpression!.Arguments[1].UnBox() as LambdaExpression)!.Body as BinaryExpression;

    //    var resultSelectorExpression = selectManyExpression.Arguments[2].UnBox() as LambdaExpression;

    //    string targetTableAlias = resultSelectorExpression!.Parameters.Last().Name!;
    //    if (previousTableExpression is MethodCallExpression prevMethodCall)
    //    {
    //        Visit(previousTableExpression);
    //    }

    //    var memberAccessesByExpression = new[]
    //    {
    //        (binaryExpression!.Left as MemberExpression)!,
    //        (binaryExpression!.Right as MemberExpression)!
    //    }.ToDictionary(m => m.Expression!);

    //    // Target table member access is from the where parameter
    //    MemberExpression targetTableMemberAccess =
    //        memberAccessesByExpression[wherePredicateLambdaExpression!.Parameters[0]];
    //    // Previous table member access is the other one
    //    MemberExpression previousTableMemberAccess = memberAccessesByExpression
    //        .First(m => m.Value != targetTableMemberAccess)
    //        .Value;
    //    // We can trust the previous parameter alias from the binary expression because it comes from external expressions,
    //    // so it is impossible to change it's alias in the comparison
    //    string previousTableAlias =
    //        //It can come from direct parameter
    //        (previousTableMemberAccess.Expression is ParameterExpression parameterExpression)
    //            ?
    //            //Or member of a computed parameter
    //            parameterExpression.Name!
    //            : (previousTableMemberAccess.Expression as MemberExpression)!.Member.Name;

    //    XElement previousTableElement = new FindOrCreateXElementVisitor(
    //        this,
    //        previousTableAlias,
    //        FetchElement
    //    ).FindOrCreateXElement(previousTableExpression);

    //    XElement targetTableElement = new FindOrCreateXElementVisitor(
    //        this,
    //        targetTableAlias,
    //        previousTableElement
    //    ).FindOrCreateXElement(targetTableExpression);
    //    if (isLeftJoin)
    //    {
    //        targetTableElement.SetAttributeValue("link-type", "outer");
    //    }

    //    targetTableElement.SetAttributeValue("from", targetTableMemberAccess.GetColName());
    //    targetTableElement.SetAttributeValue("to", previousTableMemberAccess.GetColName());
    //    return selectManyExpression;
    //}


    //Expression VisitWhere(MethodCallExpression node)
    //{
    //    if (node.Arguments[0] is MethodCallExpression methodCallExpression)
    //    {
    //        Visit(methodCallExpression);
    //    }
    //    else
    //    {
    //        CreateRootEntityFromSource(node);
    //    }

    //    LambdaExpression filterExpression = ((node.Arguments[1] as UnaryExpression)!.Operand as LambdaExpression)!;
    //    var whereVisitor = new FetchXmlWhereVisitor(this);
    //    whereVisitor.Visit(filterExpression);
    //    if (!whereVisitor.IsEmpty)
    //    {
    //        FetchElement.Descendants().First().Add(whereVisitor.FilterElement);
    //    }

    //    return node;
    //}

    //void CreateRootEntityFromSource(MethodCallExpression node)
    //{
    //    ConstantExpression? constantExpression = (node.Arguments[0] as ConstantExpression)!;
    //    Type entityType = constantExpression.Value!.GetType().GetGenericArguments()[0];
    //    var entityElement = new XElement(
    //        "entity",
    //        new XAttribute(
    //            "name",
    //            entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
    //            entityType.Name
    //        )
    //    );
    //    FetchElement.Add(entityElement);
    //    Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ?? throw new Exception($@"ODataEndointAttribute not found for {entityType.Name}");
    //    // var predicateExpression = (node.Arguments[1].UnBox() as LambdaExpression)!;
    //    // ParameterExpression parameterExpression = predicateExpression.Parameters[0];
    //    // string entityAlias = parameterExpression.Name!;
    //    // var entityElement = new XElement(
    //    //     "entity",
    //    //     new XAttribute(
    //    //         "name",
    //    //         parameterExpression.Type.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
    //    //         parameterExpression.Type.Name
    //    //     ),
    //    //     new XAttribute(
    //    //         "alias",
    //    //         entityAlias
    //    //     )
    //    // );
    //    // FetchElement.Add(entityElement);
    //    // if (EntityParametersElements.Count == 0)
    //    // {
    //    //     Endpoint = parameterExpression.Type.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ??
    //    //                "endpoint-missing";
    //    // }
    //    //
    //    // EntityParametersElements[entityAlias] = entityElement;
    //}

    //Expression VisitJoin(MethodCallExpression node)
    //{
    //    // 5 arguments:
    //    // Index    Meaning
    //    // 0    -   The left queryable OR previous join clause
    //    // 1    -   The right queryable
    //    // 2    -   The left key accessor
    //    // 3    -   The right key accessor
    //    // 4    -   Select/Projection

    //    joinCount++;
    //    Expression leftQueryableOrPreviousJoinCall = node.Arguments[0];
    //    Expression leftKeyAccessor = node.Arguments[2];
    //    Expression rightKeyAccessor = node.Arguments[3];

    //    if (leftQueryableOrPreviousJoinCall is MethodCallExpression previousJoinCallExpression)
    //    {
    //        Visit(previousJoinCallExpression);
    //    }

    //    GetOrCreateXElementsForJoin(rightKeyAccessor, leftKeyAccessor);
    //    return node;
    //}

    //Expression VisitGroupBy(MethodCallExpression node)
    //{
    //    Expression fromExpression = node.Arguments[0];
    //    GroupByExpression = (node.Arguments[1].UnBox() as LambdaExpression)!;
    //    GroupExpression = (node.Arguments[2].UnBox() as LambdaExpression)!;

    //    if (fromExpression is MethodCallExpression methodCallExpression)
    //    {
    //        Visit(methodCallExpression);
    //    }
    //    else
    //    {
    //        ParameterExpression parameterExpression = GroupByExpression.Parameters[0];
    //        string entityAlias = parameterExpression.Name!;
    //        var entityElement = new XElement(
    //            "entity",
    //            new XAttribute(
    //                "name",
    //                parameterExpression.Type.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ??
    //                parameterExpression.Type.Name
    //            ),
    //            new XAttribute(
    //                "alias",
    //                entityAlias
    //            )
    //        );
    //        FetchElement.Add(entityElement);
    //        if (EntityParametersElements.Count == 0)
    //        {
    //            Endpoint = parameterExpression.Type.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint ??
    //                       "endpoint-missing";
    //        }

    //        EntityParametersElements[entityAlias] = entityElement;
    //    }

    //    FetchElement.SetAttributeValue("aggregate", "true");
    //    ReadAttributesFromProjection(GroupByExpression, FetchElement, groupBy: true);
    //    return node;
    //}

    //void ReadAttributesFromProjection(LambdaExpression projectionExpression, XElement fetchXmlElement,
    //    bool groupBy = false)
    //{
    //    ParameterExpression parameterExpression = projectionExpression.Parameters[0];
    //    bool isGroupByQuery = parameterExpression.Type.IsGenericType
    //                          && parameterExpression.Type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
    //    if (isGroupByQuery)
    //    {
    //        var visitor = new FetchXmlGroupByProjectionVisitor(fetchXmlElement, GroupExpression!);
    //        visitor.Visit(projectionExpression);
    //    }
    //    else
    //    {
    //        var visitor = new FetchXmlSelectProjectionVisitor(fetchXmlElement, groupBy);
    //        visitor.Visit(projectionExpression);
    //    }
    //}

    //XElement GetOrCreateXElementsForJoin(
    //    Expression exp,
    //    Expression? leftExpression = null
    //)
    //{
    //    XElement parent = leftExpression != null
    //        ? GetOrCreateXElementsForJoin(leftExpression)
    //        : FetchElement;

    //    LambdaExpression lambda = ((exp as UnaryExpression)!.Operand as LambdaExpression)!;
    //    MemberExpression memberExpression = (lambda.Body as MemberExpression)!;


    //    string? entityAlias = null;
    //    Type? entityType = null;

    //    // Not from previous join: e.Id
    //    if (memberExpression.Expression is ParameterExpression parameterExpression)
    //    {
    //        entityAlias = parameterExpression.Name!;
    //        entityType = parameterExpression.Type;
    //    }

    //    // When exists a previous join: <ResultOfPreviousJoin>.e.Id
    //    if (memberExpression.Expression is MemberExpression resultOfPreviousJoinParameterExpression)
    //    {
    //        entityAlias = resultOfPreviousJoinParameterExpression.Member.Name;
    //        entityType = resultOfPreviousJoinParameterExpression.Type;
    //    }

    //    if (entityType == null)
    //    {
    //        throw new ArgumentException("Expression must be parameter or member", nameof(exp));
    //    }

    //    if (entityAlias == null)
    //    {
    //        throw new ArgumentException("Expression must be parameter or member", nameof(exp));
    //    }

    //    if (EntityParametersElements.ContainsKey(entityAlias))
    //    {
    //        return EntityParametersElements[entityAlias];
    //    }

    //    bool isRootEntity = parent == FetchElement;
    //    if (isRootEntity)
    //    {
    //        Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint;
    //    }

    //    var entityElement = new XElement(
    //        isRootEntity ? "entity" : "link-entity",
    //        new XAttribute(
    //            "name",
    //            entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ?? entityType.Name
    //        )
    //    );
    //    if (isRootEntity is false)
    //    {
    //        entityElement.Add(
    //            new XAttribute(
    //                "alias",
    //                entityAlias
    //            )
    //        );
    //        LambdaExpression leftLambda = ((leftExpression as UnaryExpression)!.Operand as LambdaExpression)!;
    //        MemberExpression leftMemberExpression = (leftLambda.Body as MemberExpression)!;

    //        entityElement.Add(new XAttribute(
    //            "to",
    //            leftMemberExpression.GetColName()
    //        ));
    //        entityElement.Add(new XAttribute(
    //            "from",
    //            memberExpression.GetColName()
    //        ));
    //    }

    //    parent.Add(entityElement);

    //    EntityParametersElements[entityAlias] = entityElement;
    //    return entityElement;
    //}

    ///// <summary>
    ///// Possibities:
    ///// Lambda: a => a.Member
    ///// Lambda: Anonymous => Anonymous.a.Member
    ///// </summary>
    //class FindOrCreateXElementWithAliasVisitor : ExpressionVisitor
    //{
    //    private readonly ODataExpressionVisitor _mainVisitor;
    //    private XElement? _createdElement = null;
    //    private XElement _parentElement;

    //    public FindOrCreateXElementWithAliasVisitor(
    //        ODataExpressionVisitor mainVisitor,
    //        XElement parentElement
    //    )
    //    {
    //        _mainVisitor = mainVisitor;
    //        _parentElement = parentElement;
    //    }

    //    public XElement FindOrCreate(Expression expression)
    //    {
    //        Visit(expression);
    //        if (_createdElement == null)
    //        {
    //            throw new ArgumentException("Could not find entity for queryable");
    //        }

    //        return _createdElement;
    //    }

    //    // IQueryable inside expressions
    //    protected override Expression VisitMember(MemberExpression node)
    //    {
    //        //Type propertyType = (node.Member as PropertyInfo)!.PropertyType;
    //        //if (propertyType.IsAssignableTo(typeof(IQueryable)))
    //        //{
    //        //    Type entityType = propertyType.GetGenericArguments()[0];
    //        //    CreateForType(
    //        //        "", entityType);
    //        //    return node;
    //        //}
    //        if (node.Type.IsDynamicsEntity())
    //        {
    //            CreateForType(node.Member.Name, node.Type);
    //        }

    //        return base.VisitMember(node);
    //    }

    //    // From parameter
    //    protected override Expression VisitParameter(ParameterExpression node)
    //    {
    //        if (node.Type.Name.Contains("AnonymousType"))
    //        {
    //            return base.VisitParameter(node);
    //        }

    //        CreateForType(node.Name!, node.Type);
    //        return base.VisitParameter(node);
    //    }


    //    //// Directly from an IQueryable<> instance
    //    //protected override Expression VisitConstant(ConstantExpression node)
    //    //{
    //    //    Type entityType = node.Value!.GetType().GetGenericArguments()[0];
    //    //    CreateForType("", entityType);
    //    //    return node;
    //    //}

    //    void CreateForType(string alias, Type entityType)
    //    {
    //        if (_mainVisitor.EntityParametersElements.ContainsKey(alias))
    //        {
    //            _createdElement = _mainVisitor.EntityParametersElements[alias];
    //            return;
    //        }

    //        bool isRoot = _parentElement.Name == "fetch";
    //        if (isRoot)
    //        {
    //            _mainVisitor.Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint;
    //        }

    //        var entityElement = new XElement(
    //            isRoot ? "entity" : "link-entity",
    //            new XAttribute(
    //                "name",
    //                entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ?? entityType.Name
    //            ),
    //            new XAttribute(
    //                "alias",
    //                alias
    //            )
    //        );
    //        _parentElement.Add(entityElement);
    //        _mainVisitor.EntityParametersElements[alias] = entityElement;
    //        _createdElement = entityElement;
    //    }
    //}


    //class FindOrCreateXElementVisitor : ExpressionVisitor
    //{
    //    private readonly ODataExpressionVisitor _mainVisitor;
    //    private XElement? _createdElement = null;
    //    private XElement _parentElement;
    //    private string _alias;

    //    public FindOrCreateXElementVisitor(
    //        ODataExpressionVisitor mainVisitor,
    //        string alias,
    //        XElement parentElement
    //    )
    //    {
    //        _alias = alias;
    //        _mainVisitor = mainVisitor;
    //        _parentElement = parentElement;
    //    }

    //    public XElement FindOrCreateXElement(Expression expression)
    //    {
    //        Visit(expression);
    //        if (_createdElement == null)
    //        {
    //            throw new ArgumentException("Could not find entity for queryable");
    //        }

    //        return _createdElement;
    //    }

    //    // IQueryable inside expressions
    //    protected override Expression VisitMember(MemberExpression node)
    //    {
    //        Type propertyType = (node.Member as PropertyInfo)!.PropertyType;
    //        if (propertyType.IsAssignableTo(typeof(IQueryable)))
    //        {
    //            Type entityType = propertyType.GetGenericArguments()[0];
    //            CreateForType(entityType);
    //            return node;
    //        }

    //        return base.VisitMember(node);
    //    }

    //    // From parameter
    //    protected override Expression VisitParameter(ParameterExpression node)
    //    {
    //        CreateForType(node.Type);
    //        return base.VisitParameter(node);
    //    }


    //    // Directly from an IQueryable<> instance
    //    protected override Expression VisitConstant(ConstantExpression node)
    //    {
    //        Type entityType = node.Value!.GetType().GetGenericArguments()[0];
    //        CreateForType(entityType);
    //        return node;
    //    }

    //    void CreateForType(Type entityType)
    //    {
    //        if (_mainVisitor.EntityParametersElements.ContainsKey(_alias))
    //        {
    //            _createdElement = _mainVisitor.EntityParametersElements[_alias];
    //            return;
    //        }

    //        bool isRoot = _parentElement.Name == "fetch";
    //        if (isRoot)
    //        {
    //            _mainVisitor.Endpoint = entityType.GetCustomAttribute<ODataEndpointAttribute>()?.Endpoint;
    //        }

    //        var entityElement = new XElement(
    //            isRoot ? "entity" : "link-entity",
    //            new XAttribute(
    //                "name",
    //                entityType.GetCustomAttribute<DynamicsEntityAttribute>()?.Name ?? entityType.Name
    //            ),
    //            new XAttribute(
    //                "alias",
    //                _alias
    //            )
    //        );
    //        _parentElement.Add(entityElement);
    //        _mainVisitor.EntityParametersElements[_alias] = entityElement;
    //        _createdElement = entityElement;
    //    }
    //}
}