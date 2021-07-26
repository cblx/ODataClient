using OData.Client.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OData.Client
{
    public class ODataSet<TSource> : IODataSet<TSource>
        where TSource : class, new()
    {
        readonly HttpMessageInvoker invoker;
        readonly string endpoint;
        readonly ODataOptions options = new ();
        Action<HttpRequestMessage> requestMessageConfiguration = null;


        public ODataSet(HttpMessageInvoker invoker, string endpoint)
        {
            this.invoker = invoker;
            this.endpoint = endpoint;
        }

        private ODataSet(
            ODataSet<TSource> o,
            ODataOptions options
        )
        {
            invoker = o.invoker;
            endpoint = o.endpoint;
            requestMessageConfiguration = o.requestMessageConfiguration;
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

        Task<TResult> Get<TResult>(string url) => HttpHelpers.Get<TResult>(new(invoker, requestMessageConfiguration, url));

        Expression<Func<TSource, object>> currentSelectExpression = null;
        public IODataSet<TSource> Select(Expression<Func<TSource, object>> selectExpression)
        {
            currentSelectExpression = selectExpression;
            return this;
        }

        public async Task<List<TProjection>> ToListAsync<TProjection>(Func<TSource, TProjection> transform) {
            string url = ToString(currentSelectExpression);
            ODataResult<TSource> result = await Get(url);
            return result.Value.Select(transform).ToList();
        }

        public async Task<TProjection> FirstOrDefaultAsync<TProjection>(Func<TSource, TProjection> transform) {
            string url = ToString(currentSelectExpression);
            ODataResult<TSource> result = await Get(url);
            return result.Value.Select(transform).FirstOrDefault();
        }

        public async Task<ODataResult<TProjection>> ToResultAsync<TProjection>(Func<TSource, TProjection> transform) {
            string url = ToString(currentSelectExpression);
            ODataResult<TSource> result = await Get(url);
            return new ODataResult<TProjection>
            {
                Count = result.Count,
                Value = result.Value.Select(transform).ToArray()
            };
        }

        public Task<ODataResult<TSource>> Execute()
        {
            string url = this.ToString();
            return Get(url);
        }

        public Task<ODataResult<TEntity>> Execute<TEntity>() where TEntity : class
        {
            var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
            string url = AppendOptions(endpoint, selectAndExpandParser.ToString());
            return Get<ODataResult<TEntity>>(url);
        }

        public async Task<ODataResult<TProjection>> Execute<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
        {
            try
            {
                string url = this.ToString(selectExpression);
                ODataResult<TSource> result = await Get(url);
                var project = selectExpression.Compile();
                var projected = new ODataResult<TProjection>
                {
                    Count = result.Count,
                    Value = result.Value.Select(
                        e => project.Invoke(e)
                    ).ToArray()
                };

                return projected;
            }catch(Exception ex)
            {
                throw new Exception("Could no project query. Check for null references in the projection expression.", ex);
            }
        }

        public async Task<TProjection> ExecuteFirstOrDefault<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
        {
            var set = Top(1);
            var result = await set.Execute(selectExpression);
            return result.Value.FirstOrDefault();
        }

        public async Task<TEntity> ExecuteFirstOrDefault<TEntity>() where TEntity: class
        {
            var set = Top(1);
            var result = await set.Execute<TEntity>();
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


        public Task<TSource> Find(Guid id) => Find<TSource>(id);

        public Task<TEntity> Find<TEntity>(Guid id) where TEntity: class
        {
            var selectAndExpandParser = new SelectAndExpandParser<TSource, TEntity>();
            string url = $"{endpoint}({id})?{selectAndExpandParser}";
            return Get<TEntity>(url);
        }

        public override string ToString()
        {
            return AppendOptions(endpoint, null);
        }

        public string ToString<TProjection>(Expression<Func<TSource, TProjection>> selectExpression)
        {
            if(selectExpression == null) { return AppendOptions(endpoint, null); }
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

    }
}
