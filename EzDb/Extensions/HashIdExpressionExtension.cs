using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HashidsNet;
using MPSHouse;

namespace MPSHouse.EzDb.Extensions
{
    public static class HashIdExpressionExtension
    {
        internal static MethodCallExpression HashIdEncode(this Expression expression, Expression xValue)
        {
            return HashId(xValue, "HashIdEncode");
        }

        internal static MethodCallExpression HashIdDecode(this Expression expression, Expression xValue)
        {
            return HashId(xValue, "HashIdDecode");
        }

        private static MethodCallExpression HashId(
            Expression xValue,
            string methodName,
            string salt = null,
            int length = -1,
            string alphabet = null,
            string seps = null)
        {
            salt = salt ?? Config.HashId.Salt;
            length = length == -1 ? Config.HashId.Length : length;
            alphabet = alphabet ?? Config.HashId.Alphabet;
            seps = seps ?? Config.HashId.Seps;

            bool nullable = false;
            try
            {
                nullable = ((PropertyInfo)((MemberExpression)xValue).Member).PropertyType == typeof(long?);
            }
            catch { }

            MethodInfo mi = typeof(HashIdExtension).GetMethods().Where(w => w.Name == methodName).Skip(nullable ? 1 : 0).First();
            MethodCallExpression mce = Expression.Call(null, mi, xValue);
            return mce;
        }

        internal static Hashids GetHashidsInstance(
            string salt = null,
            int length = -1,
            string alphabet = null,
            string seps = null)
        {
            salt = salt ?? Config.HashId.Salt;
            length = length == -1 ? Config.HashId.Length : length;
            alphabet = alphabet ?? Config.HashId.Alphabet;
            seps = seps ?? Config.HashId.Seps;

            return new Hashids(salt, length, alphabet, seps);
        }
    }
}