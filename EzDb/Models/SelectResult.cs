using System.Collections.Generic;

namespace MPSHouse.EzDb.Models
{
    public class SelectResult<T>
    {
        public int Total { get; set; }
        public IEnumerable<T> Items { get; set; }

    }
}