using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OData.Client.Abstractions
{
    public static class IODataSetExtensions
    {
        public static IODataSet<T> FilterOrs<T, TSource>(this IODataSet<T> set, IEnumerable<TSource> source, Func<TSource, Expression<Func<T, bool>>> exp)
            where T: class, new()
        {
            if(source == null) { return set; }
            if (!source.Any()) { return set; }
            return set.FilterOrs(source.Select(exp).ToArray());
        }
    }
}
