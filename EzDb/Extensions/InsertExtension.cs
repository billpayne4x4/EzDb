using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MPSHouse.EzDb.Extensions
{
    public static class InsertExtension
    {
        public static async Task<string> InsertAsync<TSource, TResult>(this DbSet<TResult> dbSet, TSource source)
            where TSource : class
            where TResult : class 
        {
            TResult result = source.Copy<TSource, TResult>(true, true);
            dbSet.Add(result);
            await dbSet.GetService<ICurrentDbContext>().Context.SaveChangesAsync();
            return ((long?)typeof(TResult).GetProperty("Id").GetValue(result)).HashIdEncode();
        }
    }
}