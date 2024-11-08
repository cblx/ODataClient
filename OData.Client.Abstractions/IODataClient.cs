using Cblx.OData.Client.Abstractions;

namespace OData.Client.Abstractions;
public interface IODataClient
{
    IDynamicsMetadataProvider MetadataProvider { get; }
    Task Delete<T>(object id, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    IODataSet<T> From<T>() where T : class;
    Task PatchAsync<T>(object id, Action<Write.Body<T>> bodyBuilder, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    [Obsolete("Use PatchAsync instead")]
    Task<RequestData> Patch<T>(object id, Write.Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    Task PostAsync<T>(Action<Write.Body<T>> bodyBuilder, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    [Obsolete("Use PostAsync instead")]
    Task<RequestData> Post<T>(Write.Body<T> body, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    Task<T> PostAndReturnAsync<T>(Action<Write.Body<T>> bodyBuilder) where T : class;
    Task<T> PostAndReturnAsync<T>(Write.Body<T> body) where T : class;
    Task Unbind<T>(object id, string nav, Action<HttpRequestMessage>? requestMessageConfiguration = null) where T : class;
    Task Unbind<T, TBind>(object id, System.Linq.Expressions.Expression<Func<T, TBind>> navExpression, Action<HttpRequestMessage>? requestMessageConfiguration = null)
        where T : class
        where TBind : class;
}


public class RequestData
{
    public string? Url { get; set; }
    public string? Body { get; set; }
}