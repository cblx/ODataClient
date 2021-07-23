using System.Collections;
using System.Threading.Tasks;

namespace OData.Client.Abstractions
{
    public interface IODataClient
    {
        Task Delete<T>(object id);
        IODataSet<T> From<T>() where T : class, new();
        Task Patch<T>(object id, Write.Body<T> body) where T : class;
        Task Post<T>(Write.Body<T> body) where T : class;
        Task Unbind<T>(object id, string nav);
        Task Unbind<T, TBind>(object id, System.Linq.Expressions.Expression<System.Func<T, TBind>> navExpression) where TBind : class;
    }
}
