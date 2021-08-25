using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MPSHouse.Extensions
{
    public static class PageExtensions
    {
        public static IEnumerable<TEntity> Page<TEntity>(this IEnumerable<TEntity> queryable, int page, int pagesize, string orderby = null)
        {
            queryable = queryable.Skip((page - 1) * pagesize).Take(pagesize);
            if(!(orderby is null))
            {
                ParameterExpression xParameter = Expression.Parameter(typeof(TEntity), "o");
                queryable = ((IQueryable<TEntity>)queryable).OrderBy(Expression.Lambda<Func<TEntity, bool>>(Expression.Property(xParameter, orderby), xParameter));
            }
            return queryable;
        }
    }
}