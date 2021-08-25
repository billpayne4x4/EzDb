using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MPSHouse.Extensions
{
    public static class WhereExtensions
    {
        public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> queryable,
        Expression<Func<TEntity, bool>> where,
        string seatchText,
        string[] searchFields,
        bool? includeActive,
        bool? includeDeleted)
        {
            ParameterExpression xParameter = where?.Parameters[0] ?? Expression.Parameter(typeof(TEntity), "w");
            Expression xWhere = where?.Body;


            if (includeActive.HasValue && includeDeleted.HasValue)
            {
                Expression xIncludeActive = Expression.Equal(Expression.Property(xParameter, "IsActive"), Expression.Constant(includeActive.Value, typeof(bool)));
                Expression xincludeDeleted = Expression.Equal(Expression.Property(xParameter, "IsDeleted"), Expression.Constant(includeDeleted.Value, typeof(bool)));

                xWhere = xWhere is null ? Expression.And(xIncludeActive, xincludeDeleted) : Expression.And(xWhere, Expression.And(xIncludeActive, xincludeDeleted));
            }
            else if (includeActive.HasValue)
            {
                Expression xIncludeActive = Expression.Equal(Expression.Property(xParameter, "IsActive"), Expression.Constant(includeActive.Value, typeof(bool)));
                xWhere = xWhere is null ? xIncludeActive : Expression.And(xWhere, xIncludeActive);
            }
            else if (includeDeleted.HasValue)
            {
                Expression xincludeDeleted = Expression.Equal(Expression.Property(xParameter, "IsDeleted"), Expression.Constant(includeDeleted.Value, typeof(bool)));
                xWhere = xWhere is null ? xincludeDeleted : Expression.And(xWhere, xincludeDeleted);
            }

            if (!(xWhere is null))
            {
                queryable = queryable.Where(Expression.Lambda<Func<TEntity, bool>>(xWhere, xParameter).Compile()).AsQueryable();
            }
            return queryable;
        }
    }
}