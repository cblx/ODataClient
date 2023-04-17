using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cblx.Dynamics;
using Cblx.Dynamics.OData.Linq;
using Cblx.OData.Client.Abstractions;
using OData.Client.Abstractions;

namespace OData.Client;

public class ODataSet<TSource> : IODataSet<TSource>
    where TSource : class
{
    private readonly string _endpoint;
    private readonly ODataClient _client;
    private readonly IDynamicsMetadataProvider _metadataProvider;
    private readonly ODataOptions _options = new();
    private Action<HttpRequestMessage>? _requestMessageConfiguration;

    public ODataSet(ODataClient client, IDynamicsMetadataProvider metadataProvider)
    {
        this._client = client;
        _metadataProvider = metadataProvider;
        _endpoint = metadataProvider.GetEndpoint<TSource>();
    }

    private ODataSet(
        ODataSet<TSource> originalODataSet,
        ODataOptions options
    )
    {
        _client = originalODataSet._client;
        _endpoint = originalODataSet._endpoint;
        _metadataProvider = originalODataSet._metadataProvider;
        _requestMessageConfiguration = originalODataSet._requestMessageConfiguration;
        this._options = options;
    }

    public string? LastQuery { get; private set; }

    public IODataSet<TSource> ConfigureRequestMessage(Action<HttpRequestMessage> requestMessageConfiguration)
    {
        return new ODataSet<TSource>(this, _options) { _requestMessageConfiguration = requestMessageConfiguration };
    }

    public IODataSet<TSource> AddOptionValue(string option, string value)
    {
        return new ODataSet<TSource>(this, _options.Clone().Add(option, value));
    }

    public async Task<List<TProjection>> SelectListAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
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

    public Task<ODataResult<TSource>> ToResultAsync() => ToResultAsync<TSource>();

    public async Task<ODataResult<TEntity>> ToResultAsync<TEntity>() where TEntity : class
    {
        var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
        var url = AppendOptions(_endpoint, selectAndExpandParser.ToString());
        if (selectAndExpandParser.HasFormattedValues)
        {
            _requestMessageConfiguration = requestMessage =>
                        requestMessage
                        .Headers.Add(
                            "Prefer",
                            $"odata.include-annotations={DynAnnotations.FormattedValue}"
                        );
        }
        return (await Get<ODataResultInternal<TEntity>>(url))!;
    }

    async Task<ODataResult<TProjection>> RewriteExpressionAndGetResultAsync<TProjection>(
        Expression<Func<TSource, TProjection>> selectExpression
    )
    {
        var url = ToString(selectExpression);
        var rewriter = new ODataProjectionRewriter();
        var projectionExpression = rewriter.Rewrite(selectExpression);
        if (rewriter.HasFormattedValues)
        {
            _requestMessageConfiguration = requestMessage =>
                        requestMessage
                        .Headers.Add(
                            "Prefer",
                            $"odata.include-annotations={DynAnnotations.FormattedValue}"
                        );
        }

        var del = projectionExpression.Compile() as Func<JsonObject, TProjection>;
        var result = await Get<JsonObject>(url);

        int? count = result!["@odata.count"]?.GetValue<int?>();
        var value = result!["value"]!.AsArray();

        var projected = new ODataResult<TProjection>
        {
            Count = count,
            Value = value.Select(
                e => del!(e!.AsObject())
            ).ToArray()
        };
        return projected;
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


    public Task<TSource> FindAsync(Guid id)
    {
        return FindAsync<TSource>(id);
    }

    public Task<TEntity> FindAsync<TEntity>(Guid id) where TEntity : class
    {
        return Get<TEntity>(CreateFindString<TEntity>(id))!;
    }

    public async Task<PicklistOption[]> GetNonGenericPicklistOptionsAsync(Expression<Func<TSource, object?>> propertyExpression)
    {
        var jsonArray = await GetPicklistOptionsJsonArray(propertyExpression);
        var picklistOptions = new List<PicklistOption>();
        foreach (var item in jsonArray!)
        {
            picklistOptions.Add(new PicklistOption
            {
                Text = item!["Label"]!["LocalizedLabels"]![0]!["Label"]!.GetValue<string>(),
                Value = item["Value"]!.GetValue<int>()
            });
        }
        return picklistOptions.ToArray();
    }

    public async Task<PicklistOption[]> GetNonGenericMultiSelectPicklistOptionsAsync(Expression<Func<TSource, string?>> propertyExpression)
    {
        var jsonArray = await GetMultiSelectPicklistOptionsJsonArray(propertyExpression);
        var picklistOptions = new List<PicklistOption>();
        foreach (var item in jsonArray!)
        {
            picklistOptions.Add(new PicklistOption
            {
                Text = item!["Label"]!["LocalizedLabels"]![0]!["Label"]!.GetValue<string>(),
                Value = item["Value"]!.GetValue<int>()
            });
        }
        return picklistOptions.ToArray();
    }

    public async Task<PicklistOption<TOption>[]> GetMultiSelectPicklistOptionsAsync<TOption>(Expression<Func<TSource, string?>> propertyExpression) where TOption : struct, Enum
    {
        var jsonArray = await GetMultiSelectPicklistOptionsJsonArray(propertyExpression);
        var picklistOptions = new List<PicklistOption<TOption>>();
        Func<JsonNode, TOption> getValue = typeof(TOption).IsEnum ?
            (JsonNode node) => (TOption)((object)node["Value"]!.GetValue<int>()) :
            (JsonNode node) => node["Value"]!.GetValue<TOption>();
        foreach (var item in jsonArray!)
        {
            picklistOptions.Add(new PicklistOption<TOption>
            {
                Text = item!["Label"]!["LocalizedLabels"]![0]!["Label"]!.GetValue<string>(),
                Value = getValue(item)
            });
        }
        return picklistOptions.ToArray();
    }

    public async Task<PicklistOption<T>[]> GetPicklistOptionsAsync<T>(Expression<Func<TSource, T?>> propertyExpression) where T : struct
    {
        var jsonArray = await GetPicklistOptionsJsonArray(propertyExpression);
        var picklistOptions = new List<PicklistOption<T>>();
        Func<JsonNode, T> getValue = typeof(T).IsEnum ? 
            (JsonNode node) => (T)((object)node["Value"]!.GetValue<int>()) :
            (JsonNode node) => node["Value"]!.GetValue<T>();
        foreach (var item in jsonArray!)
        {
            picklistOptions.Add(new PicklistOption<T>
            {
                Text = item!["Label"]!["LocalizedLabels"]![0]!["Label"]!.GetValue<string>(),
                Value = getValue(item)
            });
        }
        return picklistOptions.ToArray();
    }

    private async Task<JsonArray> GetPicklistOptionsJsonArray<T>(Expression<Func<TSource, T?>> propertyExpression)
    {
        var entityLogicalName = _metadataProvider.GetTableName<TSource>();
        Expression memberExpression = propertyExpression.Body;
        if (memberExpression is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand;
        }
        string attributeLogicalName = (memberExpression as MemberExpression)!
            .Member!
            .GetCustomAttribute<JsonPropertyNameAttribute>()!
            .Name;

        string uri =
            attributeLogicalName switch
            {
                "statecode" =>  $"EntityDefinitions(LogicalName='{entityLogicalName}')/Attributes/Microsoft.Dynamics.CRM.StateAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)",
                "statuscode" => $"EntityDefinitions(LogicalName='{entityLogicalName}')/Attributes/Microsoft.Dynamics.CRM.StatusAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)",
                _ =>            $"EntityDefinitions(LogicalName='{entityLogicalName}')/Attributes(LogicalName='{attributeLogicalName}')/Microsoft.Dynamics.CRM.PicklistAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)"
            };
        LastQuery = uri;
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        HttpResponseMessage responseMessage = await _client.Invoker.SendAsync(requestMessage, default);
        var jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(await responseMessage.Content.ReadAsStreamAsync());
        // when searching for statuscode options, we'll get it inside an array "value"
        if (jsonObject!.ContainsKey("value"))
        {
            jsonObject = jsonObject["value"]!.AsArray()!.First()!.AsObject();
        }
        var jsonArray = jsonObject!["OptionSet"]!["Options"] as JsonArray;
        return jsonArray!;
    }

    private async Task<JsonArray> GetMultiSelectPicklistOptionsJsonArray(Expression<Func<TSource, string?>> propertyExpression)
    {
        var entityLogicalName = _metadataProvider.GetTableName<TSource>();
        Expression memberExpression = propertyExpression.Body;
        if (memberExpression is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand;
        }
        string attributeLogicalName = (memberExpression as MemberExpression)!
            .Member!
            .GetCustomAttribute<JsonPropertyNameAttribute>()!
            .Name;

        string uri = $"EntityDefinitions(LogicalName='{entityLogicalName}')/Attributes(LogicalName='{attributeLogicalName}')/Microsoft.Dynamics.CRM.MultiSelectPicklistAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options)";
        LastQuery = uri;
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        HttpResponseMessage responseMessage = await _client.Invoker.SendAsync(requestMessage, default);
        var jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(await responseMessage.Content.ReadAsStreamAsync());
        // when searching for statuscode options, we'll get it inside an array "value"
        if (jsonObject!.ContainsKey("value"))
        {
            jsonObject = jsonObject["value"]!.AsArray()!.First()!.AsObject();
        }
        var jsonArray = jsonObject!["OptionSet"]!["Options"] as JsonArray;
        return jsonArray!;
    }

    private async Task<ODataResult<TSource>> Get(string url)
    {
        return (await Get<ODataResultInternal<TSource>>(url))!;
    }

    private Task<TResult?> Get<TResult>(string url)
    {
        LastQuery = url;
        //if(typeof(TResult) is { IsClass: true } resultType 
        //    && resultType.GetProperties()
        //            .Any(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name.Contains(DynAnnotations.FormattedValue) is true))
        //{
        //    _requestMessageConfiguration = requestMessage =>
        //              requestMessage
        //              .Headers.Add(
        //                  "Prefer",
        //                  $"odata.include-annotations={DynAnnotations.FormattedValue}"
        //              );
        //}
        return HttpHelpers.Get<TResult>(new RequestParameters(_client, _requestMessageConfiguration, url));
    }

    public string CreateFindString<TEntity>(Guid id) where TEntity : class
    {
        var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
        if (selectAndExpandParser.HasFormattedValues)
        {
            _requestMessageConfiguration = requestMessage =>
                        requestMessage
                        .Headers.Add(
                            "Prefer",
                            $"odata.include-annotations={DynAnnotations.FormattedValue}"
                        );
        }
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

        foreach (var option in _options.Items)
        {
            var part = $"{option.Key}=";
            part += option.Key switch
            {
                "$filter" => string.Join(" and ", option.Value),
                "$orderby" => string.Join(", ", option.Value),
                _ => option.Value.Last(),
            };
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
}