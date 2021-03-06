#nullable enable
using Cblx.Dynamics;
using System.Linq.Expressions;
namespace OData.Client.Abstractions;
public interface IODataSet<TSource>
    where TSource: class
{
    IODataSet<TSource> ConfigureRequestMessage(Action<HttpRequestMessage> requestMessageConfiguration);

    IODataSet<TSource> Filter(Expression<Func<TSource, bool>> filterExpression);

    IODataSet<TSource> FilterOrs(params Expression<Func<TSource, bool>>[] filters);

    IODataSet<TSource> Top(int top);

    IODataSet<TSource> OrderBy(Expression<Func<TSource, object?>> orderByExpression);

    IODataSet<TSource> OrderByDescending(Expression<Func<TSource, object?>> orderByExpression);

    IODataSet<TSource> SkipToken(string value);

    IODataSet<TSource> AddOptionValue(string option, string value);

    IODataSet<TSource> IncludeCount();

    IODataSetSelected<TSource> PrepareSelect(Expression<Func<TSource, object>> selectExpression);

    Task<TSource?> FindAsync(Guid id);

    Task<TEntity?> FindAsync<TEntity>(Guid id) where TEntity: class;

    Task<List<TProjection>> SelectListAsync<TProjection>(Expression<Func<TSource, TProjection>> transform);

    Task<List<TSource>> ToListAsync();

    Task<List<TEntity>> ToListAsync<TEntity>() where TEntity : class;

    Task<ODataResult<TSource>> ToResultAsync();

    Task<ODataResult<TProjection>> SelectResultAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);

    Task<TProjection?> SelectFirstOrDefaultAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);

    Task<TSource?> FirstOrDefaultAsync();

    string ToString<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);
    Task<ODataResult<TEntity>> ToResultAsync<TEntity>() where TEntity : class;
    Task<TEntity?> FirstOrDefaultAsync<TEntity>() where TEntity : class;
    Task<IEnumerable<PicklistOption>> GetPicklistOptionsAsync(Expression<Func<TSource, object>> propertyExpression);
}

