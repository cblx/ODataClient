using System.Linq.Expressions;

namespace Cblx.Dynamics.FetchXml.Linq;

public interface IAsyncQueryProvider : IQueryProvider
{
    Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
}
