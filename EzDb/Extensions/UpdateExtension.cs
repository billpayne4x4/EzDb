using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MPSHouse.EzDb.Types;

namespace MPSHouse.EzDb.Extensions
{
    public static class UpdateExtension
    {
        public static async Task UpdateAsync<TSource, TUpdate>(this DbSet<TSource> dbSet, string id, TUpdate update)
            where TSource : class
            where TUpdate : class 
        {
            ParameterExpression xParameter = Expression.Parameter(typeof(TSource), "w");
            Expression<Func<TSource, bool>> xLamda = Expression.Lambda<Func<TSource, bool>>(Expression.Equal(Expression.Property(xParameter, "Id"), Expression.Constant(id.HashIdDecode())), xParameter);
            
            TSource result = (await dbSet.SelectAsync<TSource, TSource>(xLamda, Reason.Insert)).FirstOrDefault();

            dbSet.Update(result);
            update.Copy<TUpdate, TSource>(true, false, result);
            
            await dbSet.GetService<ICurrentDbContext>().Context.SaveChangesAsync();
        }
    }
}