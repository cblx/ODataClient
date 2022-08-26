namespace OData.Client.Abstractions;
public interface IODataSetSelected<TSource> where TSource : class
{
    Task<List<TProjection>> MapToListAsync<TProjection>(Func<TSource, TProjection> transform);

    Task<TProjection> MapFirstOrDefaultAsync<TProjection>(Func<TSource, TProjection> transform);

    Task<ODataResult<TProjection>> MapToResultAsync<TProjection>(Func<TSource, TProjection> transform);
}
