using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OData.Client.Abstractions
{
    public interface IODataSetSelected<TSource> where TSource : class, new()
    {
        Task<List<TProjection>> ToListAsync<TProjection>(Func<TSource, TProjection> transform);

        Task<TProjection> FirstOrDefaultAsync<TProjection>(Func<TSource, TProjection> transform);

        Task<ODataResult<TProjection>> ToResultAsync<TProjection>(Func<TSource, TProjection> transform);
    }

}
