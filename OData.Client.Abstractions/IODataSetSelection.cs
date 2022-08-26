namespace OData.Client.Abstractions;

public interface IODataSetSelection<TProjection>
{
    Task<TProjection?> FirstOrDefaultAsync();
    Task<TProjection[]> ToArrayAsync();
    Task<List<TProjection>> ToListAsync();
    Task<ODataResult<TProjection>> ToResultAsync();
}