using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cblx.Dynamics;
using Cblx.Dynamics.OData.Linq;
using OData.Client.Abstractions;

namespace OData.Client;

public class ODataSet<TSource> : IODataSet<TSource>
    where TSource : class
{
    private readonly string _endpoint;
    private readonly ODataClient client;
    private readonly ODataOptions options = new();
    private Action<HttpRequestMessage>? requestMessageConfiguration;


    public ODataSet(ODataClient client, string endpoint)
    {
        this.client = client;
        _endpoint = endpoint;
    }

    private ODataSet(
        ODataSet<TSource> originalODataSet,
        ODataOptions options
    )
    {
        client = originalODataSet.client;
        _endpoint = originalODataSet._endpoint;
        requestMessageConfiguration = originalODataSet.requestMessageConfiguration;
        this.options = options;
    }

    public IODataSet<TSource> ConfigureRequestMessage(Action<HttpRequestMessage> requestMessageConfiguration)
    {
        return new ODataSet<TSource>(this, options) {requestMessageConfiguration = requestMessageConfiguration};
    }

    public IODataSet<TSource> AddOptionValue(string option, string value)
    {
        return new ODataSet<TSource>(this, options.Clone().Add(option, value));
    }


    public IODataSetSelected<TSource> PrepareSelect(Expression<Func<TSource, object>> selectExpression)
    {
        return new ODataSetSelected(this, selectExpression);
    }

    public async Task<List<TProjection>> SelectListAsync<TProjection>(
        Expression<Func<TSource, TProjection>> selectExpression)
    {
        var result = await SelectResultAsync(selectExpression);
        return result.Value.ToList();
    }

    public async Task<List<TSource>> ToListAsync()
    {
        var result = await ToResultAsync();
        return result.Value.ToList();
    }

    public async Task<List<TEntity>> ToListAsync<TEntity>() where TEntity : class
    {
        var result = await ToResultAsync<TEntity>();
        return result.Value.ToList();
    }

    public Task<ODataResult<TSource>> ToResultAsync()
    {
        var url = ToString();
        return Get(url);
    }

    public async Task<ODataResult<TEntity>> ToResultAsync<TEntity>() where TEntity : class
    {
        var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
        var url = AppendOptions(_endpoint, selectAndExpandParser.ToString());
        return (await Get<ODataResultInternal<TEntity>>(url))!;
    }

    async Task<ODataResult<TProjection>> RewriteExpressionAndGetResultAsync<TProjection>(
        Expression<Func<TSource, TProjection>> selectExpression
    )
    {
        var url = ToString(selectExpression);
        var rewriter = new ODataProjectionRewriter();
        requestMessageConfiguration = requestMessage =>
                    requestMessage
                    .Headers.Add(
                        "Prefer", 
                        "odata.include-annotations=OData.Community.Display.V1.FormattedValue"
                    );

        var projectionExpression = rewriter.Rewrite(selectExpression);
        var del = projectionExpression.Compile() as Func<JsonObject, TProjection>;
        var result = await Get<JsonObject>(url);

        int? count = result!["@odata.count"]?.GetValue<int?>();
        var value = result!["value"]!.AsArray();

        try
        {
            var projected = new ODataResult<TProjection>
            {
                Count = count,
                Value = value.Select(
                    e => del!(e!.AsObject())
                ).ToArray()
            };

            return projected;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could no project query. Check for null references in the projection expression.", ex);
        }
    }

    public async Task<ODataResult<TProjection>> SelectResultAsync<TProjection>(
        Expression<Func<TSource, TProjection>> selectExpression)
    {
        var url = ToString(selectExpression);
        var result = await Get(url);
        var project = selectExpression.Compile();
        try
        {
            var projected = new ODataResult<TProjection>
            {
                Count = result.Count,
                Value = result.Value.Select(
                    e => project.Invoke(e)
                ).ToArray()
            };

            return projected;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could no project query. Check for null references in the projection expression.", ex);
        }
    }


    public async Task<TProjection?> SelectFirstOrDefaultAsync<TProjection>(
        Expression<Func<TSource, TProjection>> selectExpression)
    {
        var set = Top(1);
        var result = await set.SelectResultAsync(selectExpression);
        return result.Value.FirstOrDefault();
    }

    public async Task<TSource?> FirstOrDefaultAsync()
    {
        var set = Top(1);
        var result = await set.ToResultAsync();
        return result.Value.FirstOrDefault();
    }

    public async Task<TEntity?> FirstOrDefaultAsync<TEntity>() where TEntity : class
    {
        var set = Top(1);
        var result = await set.ToResultAsync<TEntity>();
        return result.Value.FirstOrDefault();
    }

    public IODataSet<TSource> Filter(Expression<Func<TSource, bool>> filterExpression)
    {
        var visitor = new FilterVisitor(false);
        visitor.Visit(filterExpression);
        return AddOptionValue("$filter", visitor.Query);
    }

    public IODataSet<TSource> FilterOrs(params Expression<Func<TSource, bool>>[] filters)
    {
        if (!filters.Any()) return this;
        var clausules = filters.Select(f =>
        {
            var visitor = new FilterVisitor(false);
            visitor.Visit(f);
            return visitor.Query;
        });
        var joinedClausules = string.Join(" or ", clausules);
        joinedClausules = $"({joinedClausules})";
        return AddOptionValue("$filter", joinedClausules);
    }

    public IODataSet<TSource> IncludeCount()
    {
        return AddOptionValue("$count", "true");
    }

    public IODataSet<TSource> OrderBy(Expression<Func<TSource, object?>> orderByExpression)
    {
        var visitor = new FilterVisitor(false);
        visitor.Visit(orderByExpression);
        return AddOptionValue("$orderby", visitor.Query);
    }

    public IODataSet<TSource> OrderByDescending(Expression<Func<TSource, object?>> orderByExpression)
    {
        var visitor = new FilterVisitor(false);
        visitor.Visit(orderByExpression);
        return AddOptionValue("$orderby", visitor.Query + " desc");
    }

    public IODataSet<TSource> SkipToken(string value)
    {
        return AddOptionValue("$skiptoken", value);
    }

    public IODataSet<TSource> Top(int top)
    {
        return AddOptionValue("$top", top.ToString());
    }


    public Task<TSource?> FindAsync(Guid id)
    {
        return FindAsync<TSource>(id);
    }

    public Task<TEntity?> FindAsync<TEntity>(Guid id) where TEntity : class
    {
        return Get<TEntity>(CreateFindString<TEntity>(id));
    }
       
    private async Task<ODataResult<TSource>> Get(string url)
    {
        return (await Get<ODataResultInternal<TSource>>(url))!;
    }

    public async Task<PicklistOption[]> GetPicklistOptionsAsync(Expression<Func<TSource, object?>> propertyExpression)
    {
        string? entityLogicalName = typeof(TSource).GetCustomAttribute<DynamicsEntityAttribute>()?.Name;
        if(entityLogicalName is null)
        {
            throw new InvalidOperationException("You must annotate source class with [DynamicsEntity] to use this method");
        }
        Expression memberExpression = propertyExpression.Body;
        if(memberExpression is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand;
        }
        string attributeLogicalName = (memberExpression as MemberExpression)!
            .Member!
            .GetCustomAttribute<JsonPropertyNameAttribute>()!
            .Name;

        string uri = 
            attributeLogicalName == "statuscode" ?
            $"EntityDefinitions(LogicalName='{entityLogicalName}')/Attributes/Microsoft.Dynamics.CRM.StatusAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)" :
            $"EntityDefinitions(LogicalName='{entityLogicalName}')/Attributes(LogicalName='{attributeLogicalName}')/Microsoft.Dynamics.CRM.PicklistAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)";

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        HttpResponseMessage responseMessage = await client.Invoker.SendAsync(requestMessage, default);
        var jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(await responseMessage.Content.ReadAsStreamAsync());
        // when searching for statuscode options, we'll get it inside an array "value"
        if (jsonObject!.ContainsKey("value"))
        {
            jsonObject = jsonObject["value"]!.AsArray()!.First()!.AsObject();
        }
        var jsonArray = jsonObject!["OptionSet"]!["Options"] as JsonArray;
        var picklistOptions = new List<PicklistOption>();
        foreach(var item in jsonArray!)
        {
            picklistOptions.Add(new PicklistOption
            {
                Text = item!["Label"]!["LocalizedLabels"]![0]!["Label"]!.GetValue<string>(),
                Value = item["Value"]!.GetValue<int>()
            });
        }
        return picklistOptions.ToArray();
    }

    private Task<TResult?> Get<TResult>(string url)
    {
        return HttpHelpers.Get<TResult>(new RequestParameters(client, requestMessageConfiguration, url));
    }

    public string CreateFindString<TEntity>(Guid id) where TEntity : class
    {
        var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
        return $"{_endpoint}({id})?{selectAndExpandParser}";
    }

    public string ToString<TProjection>(Expression<Func<TSource, TProjection>>? selectExpression)
    {
        if (selectExpression == null) return AppendOptions(_endpoint, null);
        var visitor = new SelectAndExpandVisitor(true, null);
        visitor.Visit(selectExpression);
        return AppendOptions(_endpoint, visitor.ToString());
    }


    public override string ToString()
    {
        return AppendOptions(_endpoint, null);
    }

    private string AppendOptions(string endpoint, string? preOption)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(preOption)) parts.Add(preOption);

        foreach (var option in options.Items)
        {
            var part = $"{option.Key}=";
            switch (option.Key)
            {
                case "$filter":
                    part += string.Join(" and ", option.Value);
                    break;
                case "$orderby":
                    part += string.Join(", ", option.Value);
                    break;
                default:
                    part += option.Value.Last();
                    break;
            }

            parts.Add(part);
        }

        var joined = string.Join("&", parts);
        if (!string.IsNullOrWhiteSpace(joined)) return $"{endpoint}?{joined}";

        return endpoint;
    }

    public async Task<TProjection[]> SelectArrayAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
    {
        var result = await SelectResultAsync(selectExpression);
        return result.Value.ToArray();
    }

    public async Task<TSource[]> ToArrayAsync()
    {
        var result = await ToResultAsync();
        return result.Value.ToArray();
    }

    public async Task<TEntity[]> ToArrayAsync<TEntity>() where TEntity : class
    {
        var result = await ToResultAsync<TEntity>();
        return result.Value.ToArray();
    }

    public IODataSetSelection<TProjection> Select<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
    {
        return new ODataSelection<TProjection>(this, selectExpression);
    }

    private sealed class ODataSelection<TProjection> : IODataSetSelection<TProjection>
    {
        private readonly ODataSet<TSource> _dataSet;
        private readonly Expression<Func<TSource, TProjection>> _selectExpression;

        public ODataSelection(ODataSet<TSource> dataSet, Expression<Func<TSource, TProjection>> selectExpression)
        {
            _dataSet = dataSet;
            _selectExpression = selectExpression;
        }

        public async Task<TProjection?> FirstOrDefaultAsync()
        {
            var dataSet = _dataSet.Top(1) as ODataSet<TSource>;
            var result = await dataSet!.RewriteExpressionAndGetResultAsync(_selectExpression);
            return result.Value.FirstOrDefault();
        }

        public async Task<TProjection[]> ToArrayAsync()
        {
            var result = await _dataSet!.RewriteExpressionAndGetResultAsync(_selectExpression);
            return result.Value.ToArray();
        }

        public async Task<List<TProjection>> ToListAsync()
        {
            var result = await _dataSet!.RewriteExpressionAndGetResultAsync(_selectExpression);
            return result.Value.ToList();
        }

        public Task<ODataResult<TProjection>> ToResultAsync()
        {
            return _dataSet!.RewriteExpressionAndGetResultAsync(_selectExpression);
        }

        public override string ToString()
        {
            return _dataSet.ToString(_selectExpression);
        }
    }

    private sealed class ODataSetSelected : IODataSetSelected<TSource>
    {
        private readonly ODataSet<TSource> _dataSet;
        private readonly Expression<Func<TSource, object>>? _selectExpression;

        public ODataSetSelected(ODataSet<TSource> dataSet, Expression<Func<TSource, object>> selectExpression)
        {
            _dataSet = dataSet;
            _selectExpression = selectExpression;
        }

        public async Task<TProjection?> MapFirstOrDefaultAsync<TProjection>(Func<TSource, TProjection> transform)
        {
            var url = _dataSet.ToString(_selectExpression);
            var result = await _dataSet.Get(url);
            return result.Value.Select(transform).FirstOrDefault();
        }


        public async Task<List<TProjection>> MapToListAsync<TProjection>(Func<TSource, TProjection> transform)
        {
            var url = _dataSet.ToString(_selectExpression);
            var result = await _dataSet.Get(url);
            return result.Value.Select(transform).ToList();
        }

        public async Task<ODataResult<TProjection>> MapToResultAsync<TProjection>(Func<TSource, TProjection> transform)
        {
            var url = _dataSet.ToString(_selectExpression);
            var result = await _dataSet.Get(url);
            return new ODataResult<TProjection>
            {
                Count = result.Count,
                Value = result.Value.Select(transform).ToArray()
            };
        }
    }
}