using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace MPSHouse.EzDb.Extensions
{
    public static class DbContextResetStateExtension
    {
        public static void ResetState(this DbContext dbcontext)
        {
            ((IDbContextPoolable)dbcontext).ResetState();
            ((IDbContextPoolable)dbcontext).Resurrect(((IDbContextPoolable)dbcontext).SnapshotConfiguration());
        } 
    }
}