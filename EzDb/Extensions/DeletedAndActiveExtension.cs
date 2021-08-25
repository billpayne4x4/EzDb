using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MPSHouse.EzDb.Extensions
{
    public static class DeletedAndActiveExtension
    {
        public static async Task SetIsDeleted<TSource>(this DbSet<TSource> dbSet, string id, bool val) where TSource : class
        {
            await UpdateFieldAsync<TSource>(dbSet, id, "IsDeleted", val);
        }

        public static async Task SetIsActive<TSource>(this DbSet<TSource> dbSet, string id, bool val) where TSource : class
        {
            await UpdateFieldAsync<TSource>(dbSet, id, "IsActive", val);
        }

        private static async Task UpdateFieldAsync<TSource>(DbSet<TSource> dbSet, string id, string property, bool val) where TSource : class
        {
            ParameterExpression xParameter = Expression.Parameter(typeof(TSource), "w");
            TSource source = await dbSet
                .AsNoTracking()
                .Where(Expression.Lambda<Func<TSource, bool>>(Expression.Equal(Expression.Property(xParameter, "Id"), xParameter.HashIdDecode(Expression.Constant(id))), xParameter))
                .SingleOrDefaultAsync();

            typeof(TSource).GetProperty(property).SetValue(source, val);

            dbSet.Update(source);
            await dbSet.GetService<ICurrentDbContext>().Context.SaveChangesAsync();
        }
    }
}