using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace OData.Client
{
    public class ODataClient : IODataClient
    {
        public ODataClientOptions Options { get; private set; }
        public HttpMessageInvoker Invoker { get; private set; }

        public ODataClient(HttpMessageInvoker invoker, ODataClientOptions options = null)
        {
            this.Invoker = invoker;
            this.Options = options ?? new();
        }

        public IODataSet<T> From<T>() where T : class, new()
            => new ODataSet<T>(this, ResolveEndpointName<T>());

        public Task Post<T>(Body<T> body, Action<HttpRequestMessage> requestMessageConfiguration = null) where T : class
            => HttpHelpers.Post(
                new(
                    this,
                    requestMessageConfiguration,
                    ResolveEndpointName<T>(),
                    body.ToDictionary()
                )
            );

        public Task Patch<T>(object id, Body<T> body, Action<HttpRequestMessage> requestMessageConfiguration = null) where T : class
            => HttpHelpers.Patch(
                new(
                    this,
                    requestMessageConfiguration,
                    $"{ResolveEndpointName<T>()}({id})",
                    body.ToDictionary()
                )
            );

        public Task Delete<T>(object id, Action<HttpRequestMessage> requestMessageConfiguration = null)
            => HttpHelpers.Delete(
                new(
                    this,
                    requestMessageConfiguration,
                    $"{ResolveEndpointName<T>()}({id})"
                )
            );

        public Task Unbind<T>(object id, string nav, Action<HttpRequestMessage> requestMessageConfiguration = null)
            => HttpHelpers.Delete(
                new(
                    this,
                    requestMessageConfiguration,
                    $"{ResolveEndpointName<T>()}({id})/{nav}/$ref"
                )
            );

        public Task Unbind<T, TBind>(object id, Expression<Func<T, TBind>> navExpression, Action<HttpRequestMessage> requestMessageConfiguration = null)
            where TBind : class
            => Unbind<T>(id, (navExpression.Body as MemberExpression).Member.Name, requestMessageConfiguration);

        static string ResolveEndpointName<T>()
        {
            string endpointName = typeof(T).GetCustomAttribute<ODataTableAttribute>()?.Endpoint;
            if(endpointName != null) { return endpointName; }
            endpointName = typeof(T).Name;
            if (endpointName.EndsWith("s"))
            {
                endpointName += "es";
            }
            else
            {
                endpointName += "s";
            }
            return endpointName;
        }
    }
}
