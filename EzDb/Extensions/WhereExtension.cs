using System;
using System.Linq;
using System.Linq.Expressions;

namespace MPSHouse.EzDb.Extensions
{
    internal static class WhereExtension
    {
        internal static Expression<Func<TSource, bool>> WhereExpression<TSource>(this IQueryable<TSource> queryable,
        Expression<Func<TSource, bool>> where,
        string[] searchTerms,
        string[] searchFields,
        bool? includeActive,
        bool? includeDeleted)
        {
            Type tSource = typeof(TSource);
            ParameterExpression xParameter = where?.Parameters[0] ?? Expression.Parameter(typeof(TSource), "w");
            Expression xWhere = where?.Body;


            if (!(searchTerms is null) && !(searchFields is null))
            {
                foreach (string searchTerm in searchTerms)
                {
                    Expression xSearch = null;
                    foreach (string searchField in searchFields)
                    {
                        Expression xContains = Expression.Call(Expression.Property(xParameter, searchField),
                            typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                            Expression.Constant(searchTerm));

                        if (xSearch is null)
                            xSearch = xContains;
                        else
                            xSearch = Expression.Or(xSearch, xContains);
                    }
                    if (xWhere is null)
                        xWhere = xSearch;
                    else
                        xWhere = Expression.And(xWhere, xSearch);
                }

            }



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

            return Expression.Lambda<Func<TSource, bool>>(xWhere, xParameter);
        }
    }
}