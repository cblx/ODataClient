using OData.Client.Abstractions;
using System.Linq.Expressions;
namespace OData.Client;
public class ODataSet<TSource> : IODataSet<TSource>
    where TSource : class
{
    readonly ODataClient client;
    readonly string endpoint;
    readonly ODataOptions options = new();
    Action<HttpRequestMessage> requestMessageConfiguration = null;


    public ODataSet(ODataClient client, string endpoint)
    {
        this.client = client;
        this.endpoint = endpoint;
    }

    private ODataSet(
        ODataSet<TSource> originalODataSet,
        ODataOptions options
    )
    {
        client = originalODataSet.client;
        endpoint = originalODataSet.endpoint;
        requestMessageConfiguration = originalODataSet.requestMessageConfiguration;
        this.options = options;
    }

    public IODataSet<TSource> ConfigureRequestMessage(Action<HttpRequestMessage> requestMessageConfiguration)
    {
        return new ODataSet<TSource>(this, options) { requestMessageConfiguration = requestMessageConfiguration };
    }

    public IODataSet<TSource> AddOptionValue(string option, string value)
    {
        return new ODataSet<TSource>(this, options.Clone().Add(option, value));
    }

    Task<ODataResult<TSource>> Get(string url) => Get<ODataResult<TSource>>(url);

    Task<TResult> Get<TResult>(string url) => HttpHelpers.Get<TResult>(new(client, requestMessageConfiguration, url));

    
    public IODataSetSelected<TSource> PrepareSelect(Expression<Func<TSource, object>> selectExpression)
    {
        return new ODataSetSelected(this, selectExpression);
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

    public Task<ODataResult<TSource>> ToResultAsync()
    {
        string url = this.ToString();
        return Get(url);
    }

    public Task<ODataResult<TEntity>> ToResultAsync<TEntity>() where TEntity : class
    {
        var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
        string url = AppendOptions(endpoint, selectAndExpandParser.ToString());
        return Get<ODataResult<TEntity>>(url);
    }

    public async Task<ODataResult<TProjection>> SelectResultAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
    {
        string url = this.ToString(selectExpression);
        ODataResult<TSource> result = await Get(url);
        if (!client.Options.DisableNullNavigationPropertiesProtectionInProjections)
        {
            InstantiateNullNavigationProperties(result.Value);
        }
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
            throw new Exception("Could no project query. Check for null references in the projection expression.", ex);
        }
    }

    void InstantiateNullNavigationProperties(IEnumerable<TSource> items)
    {
        foreach(TSource item in items)
        {
            Type itemType = item.GetType();

        }
    }

    public async Task<TProjection> SelectFirstOrDefaultAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
    {
        var set = Top(1);
        var result = await set.SelectResultAsync(selectExpression);
        return result.Value.FirstOrDefault();
    }

    public async Task<TSource> FirstOrDefaultAsync()
    {
        var set = Top(1);
        var result = await set.ToResultAsync();
        return result.Value.FirstOrDefault();
    }

    public async Task<TEntity> FirstOrDefaultAsync<TEntity>() where TEntity : class
    {
        var set = Top(1);
        var result = await set.ToResultAsync<TEntity>();
        return result.Value.FirstOrDefault();
    }

    public IODataSet<TSource> Filter(Expression<Func<TSource, bool>> filterExpression)
    {
        var visitor = new FilterVisitor(keepParamName: false);
        visitor.Visit(filterExpression);
        return AddOptionValue("$filter", visitor.Query);
    }

    public IODataSet<TSource> FilterOrs(params Expression<Func<TSource, bool>>[] filters)
    {
        if (!filters.Any()) { return this; }
        IEnumerable<string> clausules = filters.Select(f =>
        {
            var visitor = new FilterVisitor(keepParamName: false);
            visitor.Visit(f);
            return visitor.Query;
        });
        string joinedClausules = string.Join(" or ", clausules);
        joinedClausules = $"({joinedClausules})";
        return AddOptionValue("$filter", joinedClausules);
    }

    public IODataSet<TSource> IncludeCount()
    {
        return AddOptionValue("$count", "true");
    }

    public IODataSet<TSource> OrderBy(Expression<Func<TSource, object>> orderByExpression)
    {
        var visitor = new FilterVisitor(keepParamName: false);
        visitor.Visit(orderByExpression);
        return AddOptionValue("$orderby", visitor.Query);
    }

    public IODataSet<TSource> OrderByDescending(Expression<Func<TSource, object>> orderByExpression)
    {
        var visitor = new FilterVisitor(keepParamName: false);
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


    public Task<TSource> FindAsync(Guid id) => FindAsync<TSource>(id);

    public Task<TEntity> FindAsync<TEntity>(Guid id) where TEntity : class => Get<TEntity>(CreateFindString<TEntity>(id));

    public string CreateFindString<TEntity>(Guid id) where TEntity : class
    {
        var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
        return $"{endpoint}({id})?{selectAndExpandParser}";
    }

    public override string ToString()
    {
        return AppendOptions(endpoint, null);
    }

    public string ToString<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
    {
        if (selectExpression == null) { return AppendOptions(endpoint, null); }
        var visitor = new SelectAndExpandVisitor(true, null);
        visitor.Visit(selectExpression);
        return AppendOptions(endpoint, visitor.ToString());
    }

    string AppendOptions(string endpoint, string preOption)
    {
        List<string> parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(preOption)) { parts.Add(preOption); }

        foreach (var option in options.Items)
        {
            string part = $"{option.Key}=";
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
        string joined = string.Join("&", parts);
        if (!string.IsNullOrWhiteSpace(joined))
        {
            return $"{endpoint}?{joined}";
        }

        return endpoint;
    }

    class ODataSetSelected : IODataSetSelected<TSource>
    {
        readonly ODataSet<TSource> dataSet;
        Expression<Func<TSource, object>> selectExpression = null;

        public ODataSetSelected(ODataSet<TSource> dataSet, Expression<Func<TSource, object>> selectExpression)
        {
            this.dataSet = dataSet;
            this.selectExpression = selectExpression;
        }

        public async Task<TProjection> MapFirstOrDefaultAsync<TProjection>(Func<TSource, TProjection> transform)
        {
            string url = dataSet.ToString(selectExpression);
            ODataResult<TSource> result = await dataSet.Get(url);
            return result.Value.Select(transform).FirstOrDefault();
        }


        public async Task<List<TProjection>> MapToListAsync<TProjection>(Func<TSource, TProjection> transform)
        {
            string url = dataSet.ToString(selectExpression);
            ODataResult<TSource> result = await dataSet.Get(url);
            return result.Value.Select(transform).ToList();
        }

        public async Task<ODataResult<TProjection>> MapToResultAsync<TProjection>(Func<TSource, TProjection> transform)
        {
            string url = dataSet.ToString(selectExpression);
            ODataResult<TSource> result = await dataSet.Get(url);
            return new ODataResult<TProjection>
            {
                Count = result.Count,
                Value = result.Value.Select(transform).ToArray()
            };
        }
    }
}
