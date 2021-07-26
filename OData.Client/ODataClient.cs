using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;

namespace OData.Client
{
    public class ODataClient : IODataClient
    {
        readonly HttpClient httpClient;
        public static bool ShowLog = false;

        public ODataClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
      
        public IODataSet<T> From<T>() where T : class, new()
            => new ODataSet<T>(httpClient, ResolveEndpointName<T>());
     
        public Task Post<T>(Body<T> body, Action<HttpRequestMessage> requestMessageConfiguration = null)  where T: class
            => HttpHelpers.Post(
                new(
                    httpClient,
                    requestMessageConfiguration,
                    ResolveEndpointName<T>(), 
                    body.ToDictionary()
                )
            );

        public Task Patch<T>(object id, Body<T> body, Action<HttpRequestMessage> requestMessageConfiguration = null) where T : class
            => HttpHelpers.Patch(
                new(
                    httpClient, 
                    requestMessageConfiguration,
                    $"{ResolveEndpointName<T>()}({id})", 
                    body.ToDictionary()
                )
            );

        public Task Delete<T>(object id, Action<HttpRequestMessage> requestMessageConfiguration = null)
            => HttpHelpers.Delete(
                new(
                    httpClient, 
                    requestMessageConfiguration, 
                    $"{ResolveEndpointName<T>()}({id})"
                )
            );

        public Task Unbind<T>(object id, string nav, Action<HttpRequestMessage> requestMessageConfiguration = null) 
            => HttpHelpers.Delete(
                new(
                    httpClient, 
                    requestMessageConfiguration, 
                    $"{ResolveEndpointName<T>()}({id})/{nav}/$ref"
                )
            );

        public Task Unbind<T, TBind>(object id, Expression<Func<T, TBind>> navExpression, Action<HttpRequestMessage> requestMessageConfiguration = null) 
            where TBind: class
            => Unbind<T>(id, (navExpression.Body as MemberExpression).Member.Name, requestMessageConfiguration);

        static string ResolveEndpointName<T>()
        {
            string endpointName = typeof(T).Name;
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
