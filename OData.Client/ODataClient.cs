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
    // Currently letting this public for using in new Body<T>(client.MetadataProvider).
    // In the future, it can be set to private and provide something like client.Body<T>()
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
        => new ODataSet<T>(this, MetadataProvider);

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

    public async Task PatchAsync<T>(object id, Action<Body<T>> bodyBuilder, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
    {
        var body = new Body<T>(MetadataProvider);
        bodyBuilder(body);
        await Patch(id, body, requestMessageConfiguration);
    }

    public async Task PostAsync<T>(Action<Body<T>> bodyBuilder, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
    {
        var body = new Body<T>(MetadataProvider);
        bodyBuilder(body);
        await Post(body, requestMessageConfiguration);
    }

    // TODO: This method doesn't have the requestMessageConfiguration parameter. Maybe we should drop it from the other methods too?
    public async Task<T> PostAndReturnAsync<T>(Action<Body<T>> bodyBuilder) where T : class
    {
        var body = new Body<T>(MetadataProvider);
        bodyBuilder(body);
        var selectParserResult = new SelectAndExpandParserV2<T, T>().ToSelectAndExpand();

        void requestMessageConfiguration(HttpRequestMessage requestMessage)
        {
            if (selectParserResult.HasFormattedValues)
            {
                requestMessage.Headers.Add("Prefer", $"odata.include-annotations={DynAnnotations.FormattedValue}");
            }
            requestMessage.Headers.Add("Prefer", $"return=representation");
        }
        return await HttpHelpers.PostAndReturnAsync<T>(
           new(
               this,
               requestMessageConfiguration,
               $"{MetadataProvider.GetEndpoint<T>()}?{selectParserResult.Query}",
               body.ToDictionary()
           )
        );
    }

    public Task Delete<T>(object id, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
        => HttpHelpers.Delete(
            new(
                this,
                requestMessageConfiguration,
                $"{MetadataProvider.GetEndpoint<T>()}({id})"
            )
        );


    public Task Unbind<T>(object id, string nav, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class
        => HttpHelpers.Delete(
            new(
                this,
                requestMessageConfiguration,
                $"{MetadataProvider.GetEndpoint<T>()}({id})/{nav}/$ref"
            )
        );

    public Task Unbind<T, TBind>(object id, Expression<Func<T, TBind>> navExpression, Action<HttpRequestMessage>? requestMessageConfiguration = null)
        where T : class
        where TBind : class
    {
        var member = navExpression.Body as MemberExpression;
        return Unbind<T>(id, member!.GetFieldName(), requestMessageConfiguration);
    }
}
