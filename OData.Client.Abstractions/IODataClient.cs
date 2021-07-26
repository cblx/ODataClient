using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;

namespace OData.Client.Abstractions
{
    public interface IODataClient
    {
        Task Delete<T>(object id, Action<HttpRequestMessage> requestMessageConfiguration = null);
        IODataSet<T> From<T>() where T : class, new();
        Task Patch<T>(object id, Write.Body<T> body, Action<HttpRequestMessage> requestMessageConfiguration = null) where T : class;
        Task Post<T>(Write.Body<T> body, Action<HttpRequestMessage> requestMessageConfiguration = null) where T : class;
        Task Unbind<T>(object id, string nav, Action<HttpRequestMessage> requestMessageConfiguration = null);
        Task Unbind<T, TBind>(object id, System.Linq.Expressions.Expression<System.Func<T, TBind>> navExpression, Action<HttpRequestMessage> requestMessageConfiguration = null) where TBind : class;
    }
}
