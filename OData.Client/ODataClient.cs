using Cblx.Dynamics;
using Cblx.OData.Client.Abstractions;
using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System.Linq.Expressions;
namespace OData.Client;
public class ODataClient : IODataClient
{
    public DynamicsOptions Options { get; private set; }
    public HttpMessageInvoker Invoker { get; private set; }
    public IDynamicsMetadataProvider MetadataProvider { get; private set; }

    public ODataClient(
        HttpMessageInvoker invoker, 
        IDynamicsMetadataProvider? metadataProvider = null,
        DynamicsOptions? options = null
    )
    {
        Options = options ?? new();
        Invoker = invoker;
        MetadataProvider = metadataProvider ?? new DynamicsCodeMetadataProvider();
    }

    public IODataSet<T> From<T>() where T : class
        => new ODataSet<T>(this, MetadataProvider.GetEndpoint<T>());

    public Task Post<T>(Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
        => HttpHelpers.Post(
            new(
                this,
                requestMessageConfiguration,
                MetadataProvider.GetEndpoint<T>(),
                body.ToDictionary()
            )
        );

    public Task Patch<T>(object id, Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
        => HttpHelpers.Patch(
            new(
                this,
                requestMessageConfiguration,
                $"{MetadataProvider.GetEndpoint<T>()}({id})",
                body.ToDictionary()
            )
        );

    public Task Delete<T>(object id, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T: class
        => HttpHelpers.Delete(
            new(
                this,
                requestMessageConfiguration,
                $"{MetadataProvider.GetEndpoint<T>()}({id})"
            )
        );

    public Task Unbind<T>(object id, string nav, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T: class
        => HttpHelpers.Delete(
            new(
                this,
                requestMessageConfiguration,
                $"{MetadataProvider.GetEndpoint<T>()}({id})/{nav}/$ref"
            )
        );

    public Task Unbind<T, TBind>(object id, Expression<Func<T, TBind>> navExpression, Action<HttpRequestMessage>? requestMessageConfiguration = null)
        where T: class
        where TBind : class
    {
        var member = navExpression.Body as MemberExpression;
        return Unbind<T>(id, member!.GetFieldName(), requestMessageConfiguration);
    }
}
