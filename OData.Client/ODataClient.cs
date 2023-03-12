using Cblx.OData.Client.Abstractions;
using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System.Linq.Expressions;
using System.Reflection;
namespace OData.Client;
public class ODataClient : IODataClient
{
    public DynamicsOptions Options { get; private set; }
    public HttpMessageInvoker Invoker { get; private set; }

    public ODataClient(HttpMessageInvoker invoker, DynamicsOptions? options = null)
    {
        this.Invoker = invoker;
        this.Options = options ?? new();
    }

    public IODataSet<T> From<T>() where T : class
        => new ODataSet<T>(this, ODataClientHelpers.ResolveEndpointName<T>());

    public Task Post<T>(Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
        => HttpHelpers.Post(
            new(
                this,
                requestMessageConfiguration,
                ODataClientHelpers.ResolveEndpointName<T>(),
                body.ToDictionary()
            )
        );

    public Task Patch<T>(object id, Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
        => HttpHelpers.Patch(
            new(
                this,
                requestMessageConfiguration,
                $"{ODataClientHelpers.ResolveEndpointName<T>()}({id})",
                body.ToDictionary()
            )
        );

    public Task Delete<T>(object id, Action<HttpRequestMessage>? requestMessageConfiguration = null)
        => HttpHelpers.Delete(
            new(
                this,
                requestMessageConfiguration,
                $"{ODataClientHelpers.ResolveEndpointName<T>()}({id})"
            )
        );

    public Task Unbind<T>(object id, string nav, Action<HttpRequestMessage>? requestMessageConfiguration = null)
        => HttpHelpers.Delete(
            new(
                this,
                requestMessageConfiguration,
                $"{ODataClientHelpers.ResolveEndpointName<T>()}({id})/{nav}/$ref"
            )
        );

    public Task Unbind<T, TBind>(object id, Expression<Func<T, TBind>> navExpression, Action<HttpRequestMessage>? requestMessageConfiguration = null)
        where TBind : class
    {
        var member = navExpression.Body as MemberExpression;
        return Unbind<T>(id, member!.GetFieldName(), requestMessageConfiguration);
    }
}
