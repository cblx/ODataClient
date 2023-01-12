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
  
    Task<TSource> FindAsync(Guid id);

    Task<TEntity> FindAsync<TEntity>(Guid id) where TEntity: class;

    [Obsolete("Use .Select(...).ToListAsync()")]
    Task<List<TProjection>> SelectListAsync<TProjection>(Expression<Func<TSource, TProjection>> transform);

    [Obsolete("Use .Select(...).ToArrayAsync()")]
    Task<TProjection[]> SelectArrayAsync<TProjection>(Expression<Func<TSource, TProjection>> transform);

    Task<List<TSource>> ToListAsync();
    Task<List<TEntity>> ToListAsync<TEntity>() where TEntity : class;

    Task<TSource[]> ToArrayAsync();
    Task<TEntity[]> ToArrayAsync<TEntity>() where TEntity : class;

    IODataSetSelection<TProjection> Select<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);

    [Obsolete("Use .Select(...).ToResultAsync()")]
    Task<ODataResult<TProjection>> SelectResultAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);

    [Obsolete("Use .Select(...).FirstOrDefaultAsync()")]
    Task<TProjection?> SelectFirstOrDefaultAsync<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);


    string ToString<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);
    Task<ODataResult<TSource>> ToResultAsync();
    Task<ODataResult<TEntity>> ToResultAsync<TEntity>() where TEntity : class;
    Task<TSource?> FirstOrDefaultAsync();
    Task<TEntity?> FirstOrDefaultAsync<TEntity>() where TEntity : class;
    Task<PicklistOption<T>[]> GetPicklistOptionsAsync<T>(Expression<Func<TSource, T?>> propertyExpression) where T: struct;
    [Obsolete("Use the generic version")]
    Task<PicklistOption[]> GetNonGenericPicklistOptionsAsync(Expression<Func<TSource, object?>> propertyExpression);
}

