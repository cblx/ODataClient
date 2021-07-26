using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OData.Client.Abstractions
{
    public interface IODataSet<TSource>
        where TSource: class, new()
    {
        IODataSet<TSource> ConfigureRequestMessage(Action<HttpRequestMessage> requestMessageConfiguration);

        IODataSet<TSource> Filter(Expression<Func<TSource, bool>> filterExpression);

        IODataSet<TSource> FilterOrs(params Expression<Func<TSource, bool>>[] filters);

        IODataSet<TSource> Top(int top);

        IODataSet<TSource> OrderBy(Expression<Func<TSource, object>> orderByExpression);

        IODataSet<TSource> OrderByDescending(Expression<Func<TSource, object>> orderByExpression);

        IODataSet<TSource> SkipToken(string value);

        IODataSet<TSource> AddOptionValue(string option, string value);

        IODataSet<TSource> IncludeCount();

        /// <summary>
        /// Utilize esta forma + (ToListaAsync,ToResultAsync ou FirstOrDefaultAsync) para usar separadamente
        /// a lógica de execução de projeção vs lógica de seleção.
        /// As vezes isso é necessário pois podem existir Expands complexos
        /// que devem ser representados para a seleção, mas não devem ser executados na projeção.
        /// Pensar em talvez manter apenas desta forma e não oferecer mais as opções "Execute", apesar de serem convenientes
        /// na maioria das consultas.
        /// </summary>
        /// <param name="selectExpression"></param>
        /// <returns></returns>
        IODataSet<TSource> Select(Expression<Func<TSource, object>> selectExpression);

        Task<TSource> Find(Guid id);

        Task<TEntity> Find<TEntity>(Guid id) where TEntity: class;

        Task<List<TProjection>> ToListAsync<TProjection>(Func<TSource, TProjection> transform);

        Task<TProjection> FirstOrDefaultAsync<TProjection>(Func<TSource, TProjection> transform);

        Task<ODataResult<TProjection>> ToResultAsync<TProjection>(Func<TSource, TProjection> transform);

        Task<ODataResult<TSource>> Execute();

        Task<ODataResult<TProjection>> Execute<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);

        Task<TProjection> ExecuteFirstOrDefault<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);

        string ToString<TProjection>(Expression<Func<TSource, TProjection>> selectExpression);
        Task<ODataResult<TEntity>> Execute<TEntity>() where TEntity : class;
        Task<TEntity> ExecuteFirstOrDefault<TEntity>() where TEntity : class;
    }

}
