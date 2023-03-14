using Cblx.OData.Client.Abstractions;

namespace OData.Client.Abstractions;
public interface IODataClient
{
    IDynamicsMetadataProvider MetadataProvider { get; }
    Task Delete<T>(object id, Action<HttpRequestMessage>? requestMessageConfiguration = null);
    IODataSet<T> From<T>() where T : class;
    Task Patch<T>(object id, Write.Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    Task Post<T>(Write.Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    Task Unbind<T>(object id, string nav, Action<HttpRequestMessage>? requestMessageConfiguration = null);
    Task Unbind<T, TBind>(object id, System.Linq.Expressions.Expression<Func<T, TBind>> navExpression, Action<HttpRequestMessage>? requestMessageConfiguration = null) where TBind : class;
}
