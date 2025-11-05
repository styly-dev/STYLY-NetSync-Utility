using System;

namespace Styly.NetSync.Utility
{
    /// <summary>
    /// Enumと文字列の変換
    /// </summary>
    public static class EnumExtensions
    {
        public static string ToStringValue<T>(this T rpc) where T : Enum
            => rpc.ToString();

        public static bool TryParse<T>(this string str, out T outputRpc) where T : Enum
        {
            var result = Enum.TryParse(typeof(T), str, out var output);
            outputRpc = (T) output;
            return result;
        }
    }
}
