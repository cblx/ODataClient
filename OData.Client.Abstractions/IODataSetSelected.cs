using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OData.Client.Abstractions
{
    public interface IODataSetSelected<TSource> where TSource : class, new()
    {
        Task<List<TProjection>> MapToListAsync<TProjection>(Func<TSource, TProjection> transform);

        Task<TProjection> MapFirstOrDefaultAsync<TProjection>(Func<TSource, TProjection> transform);

        Task<ODataResult<TProjection>> MapToResultAsync<TProjection>(Func<TSource, TProjection> transform);
    }

}
