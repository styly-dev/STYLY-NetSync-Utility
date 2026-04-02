using System;

namespace Styly.NetSync.Utility
{
    /// <summary>
    /// Enumと文字列の変換
    /// </summary>
    public static class EnumExtensions
    {
        public static bool TryParse<T>(string str, out T outputRpc) where T : Enum
        {
            var result = Enum.TryParse(typeof(T), str, out var output);
            outputRpc = result ? (T) output : default;
            return result;
        }
    }
}
