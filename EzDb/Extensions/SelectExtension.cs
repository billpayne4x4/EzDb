using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using MPSHouse.EzDb.Attributes;
using MPSHouse.EzDb.Models;
using MPSHouse.EzDb.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MPSHouse.EzDb.Extensions
{
    public static class SelectExtensions
    {
        public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
            this DbSet<TSource> dbSet,
            Expression<Func<TSource, bool>> where,
            Reason reason)
                where TSource : class
                where TResult : class
        {
            return (await SelectAsync<TSource, TResult>(dbSet, where, null, null, null, null, null, null, null, false, reason))?.Items;
        }

        public static async Task<TResult> SelectSingleAsync<TSource, TResult>(
            this DbSet<TSource> dbSet,
            Expression<Func<TSource, bool>> where = null,
            bool? includeActive = true,
            bool? includeDeleted = false)
                where TSource : class
                where TResult : class
        {
            return (await SelectAsync<TSource, TResult>(dbSet, where, includeActive, includeDeleted, null, null, null, null, null, false, Reason.None))?.Items.SingleOrDefault();
        }

        public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
            this DbSet<TSource> dbSet,
            Expression<Func<TSource, bool>> where = null,
            bool? includeActive = true,
            bool? includeDeleted = false)
                where TSource : class
                where TResult : class
        {
            return (await SelectAsync<TSource, TResult>(dbSet, where, includeActive, includeDeleted, null, null, null, null, null, false, Reason.None))?.Items;
        }

        public static async Task<SelectResult<TResult>> SelectAsync<TSource, TResult>(this DbSet<TSource> dbSet, SearchRequest searchRequest)
            where TSource : class
            where TResult : class
        {
            return await SelectAsync<TSource, TResult>(
                dbSet,
                null,
                searchRequest.IncludeActive,
                searchRequest.IncludeDeleted,
                searchRequest.SearchTerms,
                searchRequest.SearchFields,
                searchRequest.PageIndex,
                searchRequest.PageSize,
                searchRequest.OrderBy,
                true,
                Reason.None);
        }

        public static async Task<SelectResult<TResult>> SelectAsync<TSource, TResult>(this DbSet<TSource> dbSet, Expression<Func<TSource, bool>> where, SearchRequest searchRequest)
            where TSource : class
            where TResult : class
        {
            return await SelectAsync<TSource, TResult>(
                dbSet,
                where,
                searchRequest.IncludeActive,
                searchRequest.IncludeDeleted,
                searchRequest.SearchTerms,
                searchRequest.SearchFields,
                searchRequest.PageIndex,
                searchRequest.PageSize,
                searchRequest.OrderBy,
                true,
                Reason.None);
        }

        public static async Task<SelectResult<TResult>> SelectAsync<TSource, TResult>(
            DbSet<TSource> dbSet,
            Expression<Func<TSource, bool>> where,
            bool? includeActive = true,
            bool? includeDeleted = false,
            string[] searchTerms = null,
            string[] searchFields = null,
            int? pageIndex = null,
            int? pageSize = null,
            string orderBy = null,
            bool includeTotal = false,
            Reason reason = Reason.None)
                where TSource : class
                where TResult : class
        {
            SelectResult<TResult> result = new SelectResult<TResult>();
            IQueryable<TSource> queryable = dbSet.AsNoTracking();

            if (!(where is null) || includeActive.HasValue || includeDeleted.HasValue || ((!(searchTerms is null) && searchTerms.Length > 0) && (!(searchFields is null) && searchFields.Length > 0)))
                queryable = queryable.Where(queryable.WhereExpression(where, searchTerms, searchFields, includeActive, includeDeleted));

            if (includeTotal) result.Total = await queryable.CountAsync();

            if (pageIndex.HasValue && pageSize.HasValue)
                queryable = queryable.Skip((pageIndex.Value - 1) * pageSize.Value).Take(pageSize.Value);

            if (!string.IsNullOrEmpty(orderBy))
            {
                string[] directionAndOrderBy = orderBy.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (directionAndOrderBy.Length == 2 && (directionAndOrderBy[0] == "+" || directionAndOrderBy[0] == "-"))
                {
                    string direction = directionAndOrderBy[0];
                    orderBy = directionAndOrderBy[1];

                    ParameterExpression xParameter = Expression.Parameter(typeof(TSource), "o");
                    Expression<Func<TSource, object>> xLambda = Expression.Lambda<Func<TSource, object>>(Expression.Property(xParameter, orderBy), xParameter);

                    queryable = direction == "+" ? queryable.OrderBy<TSource, object>(xLambda) : queryable.OrderByDescending<TSource, object>(xLambda);
                }
            }

            result.Items = await queryable.Select(BuildSelectLambdaExpression<TSource, TResult>(0, null, false, reason)).ToListAsync();

            return result;
        }

        public static Expression<Func<TSource, TResult>> BuildSelectLambdaExpression<TSource, TResult>(int cnt = 0, ParameterExpression xParameter = null, bool notRecursive = false, Reason reason = Reason.None)
        {
            xParameter = xParameter ?? Expression.Parameter(typeof(TSource), $"s{cnt}");
            return Expression.Lambda<Func<TSource, TResult>>(BuildSelectInitExpression<TSource, TResult>(xParameter, cnt, null, notRecursive, reason), xParameter);
        }

        public static MemberInitExpression BuildSelectInitExpression<TSource, TResult>(ParameterExpression xParameter, int cnt, Expression xProperty = null, bool notRecursive = false, Reason reason = Reason.None)
        {
            Type tSource = typeof(TSource);
            Type tResult = typeof(TResult);

            NewExpression xNew = Expression.New(tResult);
            List<MemberAssignment> members = new List<MemberAssignment>();

            PropertyInfo[] sourceProperties = tSource.GetProperties();
            PropertyInfo[] resultProperties = tResult.GetProperties();

            foreach (PropertyInfo sourceProperty in sourceProperties)
            {
                foreach (PropertyInfo resultProperty in resultProperties)
                {
                    if(reason == Reason.Insert && (sourceProperty.GetCustomAttributes(typeof(EzDbInsertIgnoreAttribute)).Count() > 0 || sourceProperty.GetCustomAttributes(typeof(EzDbInsertIgnoreAttribute)).Count() > 0)) continue;
                    if(sourceProperty.GetCustomAttributes(typeof(EzDbIgnoreAttribute)).Count() > 0 || sourceProperty.GetCustomAttributes(typeof(EzDbIgnoreAttribute)).Count() > 0) continue;
                    if((sourceProperty.Name == resultProperty.Name) && (sourceProperty.Name.EndsWith("Id") && resultProperty.Name.EndsWith("Id")) &&
                        ((sourceProperty.PropertyType == typeof(long) || sourceProperty.PropertyType == typeof(long?)) && resultProperty.PropertyType == typeof(string)))
                    {
                        members.Add(Expression.Bind(resultProperty, xParameter.HashIdEncode(Expression.Property(xProperty ?? xParameter, sourceProperty.Name))));
                    }
                    else if (resultProperty.Name == $"{tSource.Name}Id" || sourceProperty.Name == $"{tSource.Name}Id")
                    {
                        if (sourceProperty.Name == "Id" || resultProperty.Name == "Id")
                        {
                            members.Add(Expression.Bind(resultProperty, xNew.HashIdEncode(Expression.Property(xProperty ?? xParameter, sourceProperty.Name))));
                        }
                    }
                    else if (resultProperty.Name != sourceProperty.Name) continue;
                    else if (resultProperty.PropertyType.IsClass &&
                        !resultProperty.PropertyType.Assembly.FullName.StartsWith("System") &&
                        !resultProperty.PropertyType.Assembly.FullName.StartsWith("Microsoft"))
                    {
                        if(notRecursive) continue;
                        MemberInitExpression xMemberInit = (MemberInitExpression)typeof(SelectExtensions).GetTypeInfo()
                            .GetMethod("BuildSelectInitExpression")
                            .MakeGenericMethod(sourceProperty.PropertyType, resultProperty.PropertyType)
                            .Invoke(null, new object[] { xParameter, cnt, Expression.Property(xProperty ?? xParameter, resultProperty.Name), sourceProperty.GetCustomAttributes(typeof(EzDbNotRecursiveAttribute)).Count() > 0, reason });

                        members.Add(Expression.Bind(resultProperty, xMemberInit));
                    }
                    else if (resultProperty.PropertyType.Name == "IEnumerable`1")
                    {
                        if(notRecursive) continue;
                        Type tSourceProperty = sourceProperty.PropertyType.GetGenericArguments()[0];
                        Type tResultProperty = resultProperty.PropertyType.GetGenericArguments()[0];

                        ParameterExpression xParameter2 = Expression.Parameter(tSourceProperty, $"s{++cnt}");
                        Expression xLambda = (Expression)typeof(SelectExtensions).GetTypeInfo()
                            .GetMethod("BuildSelectLambdaExpression")
                            .MakeGenericMethod(tSourceProperty, tResultProperty)
                            .Invoke(null, new object[] { cnt, xParameter2, sourceProperty.GetCustomAttributes(typeof(EzDbNotRecursiveAttribute)).Count() > 0, reason });

                        MethodInfo methodInfo = typeof(Enumerable).GetTypeInfo().DeclaredMethods.Where(w =>
                            {
                                ParameterInfo[] pis = w.GetParameters();
                                return w.Name == "Select" &&
                                        pis.Count() == 2 &&
                                        pis[0].ParameterType.Name == typeof(IEnumerable<>).Name &&
                                        pis[1].ParameterType.Name == typeof(Func<,>).Name;
                            })
                            .SingleOrDefault()?
                            .MakeGenericMethod(tSourceProperty, tResultProperty);

                        members.Add(Expression.Bind(resultProperty, Expression.Call(null, methodInfo, new Expression[] { Expression.Property(xProperty ?? xParameter, resultProperty.Name), xLambda })));
                    }
                    else
                        members.Add(Expression.Bind(resultProperty, Expression.Property(xProperty ?? xParameter, resultProperty.Name)));
                }
            }
            return Expression.MemberInit(xNew, members);
        }
    }
}