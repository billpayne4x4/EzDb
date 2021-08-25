using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace MPSHouse.Extensions
{
    public static class DbSetExtensions
    {
        // public static IQueryable<TResult> Select<TEntity, TResult>(this IQueryable<TEntity> queryable)
        // {
        //     return queryable.Select<TEntity, TResult>(BuildSelectLambdaExpression<TEntity, TResult>().Compile()).AsQueryable();
        // }

    }

}