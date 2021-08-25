using System.Linq;
using HashidsNet;

namespace MPSHouse.EzDb.Extensions
{
    public static class HashIdExtension
    {

        public static string HashIdEncode(this long source) =>
            ((long?)source).HashIdEncode();

        public static string HashIdEncode(this long? source)
        {
            return !source.HasValue ? null : new Hashids(Config.HashId.Salt,
                Config.HashId.Length,
                Config.HashId.Alphabet,
                Config.HashId.Seps).EncodeLong(new long[] { source.Value });
        }

        public static long HashIdDecode(this string source)
        {
            try{
            long? id = source is null ? null : new Hashids(Config.HashId.Salt,
                Config.HashId.Length,
                Config.HashId.Alphabet,
                Config.HashId.Seps).DecodeLong(source)?.First();

            return id is null ? 0 : id.Value;
            }catch(System.Exception ee)
            {

            }
            return 0;
        }

    }
}