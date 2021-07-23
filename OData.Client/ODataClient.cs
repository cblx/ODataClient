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

        public ODataClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public IODataSet<T> From<T>() where T : class, new()
        {
            return new ODataSet<T>(httpClient, ResolveEndpointName<T>());
        }

        public Task Post<T>(Body<T> body)  where T: class
            => HttpHelpers.Post(
                httpClient, 
                ResolveEndpointName<T>(), 
                body.ToDictionary());

        public Task Patch<T>(object id, Body<T> body) where T : class
            => HttpHelpers.Patch(
                httpClient, 
                $"{ResolveEndpointName<T>()}({id})", 
                body.ToDictionary());

        public Task Delete<T>(object id) => HttpHelpers.Delete(httpClient, $"{ResolveEndpointName<T>()}({id})");

        public Task Unbind<T>(object id, string nav) 
            => HttpHelpers.Delete(httpClient, $"{ResolveEndpointName<T>()}({id})/{nav}/$ref");

        public Task Unbind<T, TBind>(object id, Expression<Func<T, TBind>> navExpression) 
            where TBind: class
            => Unbind<T>(id, (navExpression.Body as MemberExpression).Member.Name);

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
