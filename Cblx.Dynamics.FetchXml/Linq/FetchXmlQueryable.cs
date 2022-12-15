﻿using System.Collections;
using System.Linq.Expressions;

namespace Cblx.Dynamics.FetchXml.Linq;
public class FetchXmlQueryable<T> : QueryableBase<T>
{
    public FetchXmlQueryable(IQueryProvider provider) : base(provider)
    {

    }

    public FetchXmlQueryable(IQueryProvider provider, Expression expression) : base(provider, expression)
    {
    }

    public string ToFetchXml()
    {
        if (Expression != null)
        {
            var visitor = new FetchXmlExpressionVisitor();
            visitor.Visit(Expression);
            return visitor.ToFetchXml();
        }
        return "";
    }

    public string ToRelativeUrl()
    {
        if (Expression != null)
        {
            var visitor = new FetchXmlExpressionVisitor();
            visitor.Visit(Expression);
            return visitor.ToRelativeUrl();
        }
        return "";
    }
}

public interface IAsyncQueryProvider : IQueryProvider
{
    Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
}

//
// Resumo:
//     Acts as a common base class for System.Linq.IQueryable`1 implementations based
//     on re-linq. In a specific LINQ provider, a custom queryable class should be derived
//     from Remotion.Linq.QueryableBase`1 which supplies an implementation of Remotion.Linq.IQueryExecutor
//     that is used to execute the query. This is then used as an entry point (the main
//     data source) of a LINQ query.
//
// Parâmetros de Tipo:
//   T:
//     The type of the result items yielded by this query.
public abstract class QueryableBase<T> : IOrderedQueryable<T>, IOrderedQueryable, IQueryable, IEnumerable, IQueryable<T>, IEnumerable<T>
{
    private readonly IQueryProvider _queryProvider;

    //
    // Resumo:
    //     Gets the expression tree that is associated with the instance of System.Linq.IQueryable.
    //     This expression describes the query represented by this Remotion.Linq.QueryableBase`1.
    //
    // Devoluções:
    //     The System.Linq.Expressions.Expression that is associated with this instance
    //     of System.Linq.IQueryable.
    public Expression Expression
    {
        get;
        private set;
    }

    //
    // Resumo:
    //     Gets the query provider that is associated with this data source. The provider
    //     is used to execute the query. By default, a Remotion.Linq.DefaultQueryProvider
    //     is used that parses the query and passes it on to an implementation of Remotion.Linq.IQueryExecutor.
    //
    // Devoluções:
    //     The System.Linq.IQueryProvider that is associated with this data source.
    public IQueryProvider Provider => _queryProvider;

    //
    // Resumo:
    //     Gets the type of the element(s) that are returned when the expression tree associated
    //     with this instance of System.Linq.IQueryable is executed.
    //
    // Devoluções:
    //     A System.Type that represents the type of the element(s) that are returned when
    //     the expression tree associated with this object is executed.
    public Type ElementType => typeof(T);

    //
    // Resumo:
    //     Initializes a new instance of the Remotion.Linq.QueryableBase`1 class with a
    //     Remotion.Linq.DefaultQueryProvider and the given executor. This constructor should
    //     be used by subclasses to begin a new query. The Remotion.Linq.QueryableBase`1.Expression
    //     generated by this constructor is a System.Linq.Expressions.ConstantExpression
    //     pointing back to this Remotion.Linq.QueryableBase`1.
    //
    // Parâmetros:
    //   queryParser:
    //     The Remotion.Linq.Parsing.Structure.IQueryParser used to parse queries. Specify
    //     an instance of Remotion.Linq.Parsing.Structure.QueryParser for default behavior.
    //     See also Remotion.Linq.Parsing.Structure.QueryParser.CreateDefault.
    //
    //   executor:
    //     The Remotion.Linq.IQueryExecutor used to execute the query represented by this
    //     Remotion.Linq.QueryableBase`1.
    //protected QueryableBase(IQueryParser queryParser, IQueryExecutor executor)
    //{
    //    _queryProvider = new DefaultQueryProvider(GetType().GetGenericTypeDefinition(), queryParser, executor);
    //    Expression = Expression.Constant(this);
    //}

    //
    // Resumo:
    //     Initializes a new instance of the Remotion.Linq.QueryableBase`1 class with a
    //     specific System.Linq.IQueryProvider. This constructor should only be used to
    //     begin a query when Remotion.Linq.DefaultQueryProvider does not fit the requirements.
    //
    // Parâmetros:
    //   provider:
    //     The provider used to execute the query represented by this Remotion.Linq.QueryableBase`1
    //     and to construct queries around this Remotion.Linq.QueryableBase`1.
    protected QueryableBase(IQueryProvider provider)
    {
        _queryProvider = provider;
        Expression = Expression.Constant(this);
    }

    //
    // Resumo:
    //     Initializes a new instance of the Remotion.Linq.QueryableBase`1 class with a
    //     given provider and expression. This is an infrastructure constructor that must
    //     be exposed on subclasses because it is used by Remotion.Linq.DefaultQueryProvider
    //     to construct queries around this Remotion.Linq.QueryableBase`1 when a query method
    //     (e.g. of the System.Linq.Queryable class) is called.
    //
    // Parâmetros:
    //   provider:
    //     The provider used to execute the query represented by this Remotion.Linq.QueryableBase`1
    //     and to construct queries around this Remotion.Linq.QueryableBase`1.
    //
    //   expression:
    //     The expression representing the query.
    protected QueryableBase(IQueryProvider provider, Expression expression)
    {
        _queryProvider = provider;
        Expression = expression;
    }

    //
    // Resumo:
    //     Executes the query via the Remotion.Linq.QueryableBase`1.Provider and returns
    //     an enumerator that iterates through the items returned by the query.
    //
    // Devoluções:
    //     A System.Collections.Generic.IEnumerator`1 that can be used to iterate through
    //     the query result.
    public IEnumerator<T> GetEnumerator()
    {
        return _queryProvider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _queryProvider.Execute<IEnumerable>(Expression).GetEnumerator();
    }
}