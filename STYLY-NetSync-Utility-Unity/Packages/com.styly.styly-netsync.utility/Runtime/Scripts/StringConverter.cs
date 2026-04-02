using System;
using System.Globalization;

namespace Styly.NetSync.Utility
{
    internal static class StringConverter
    {
        public static T Parse<T>(string value)
        {
            if (typeof(T) == typeof(string)) return (T)(object)value;
            if (typeof(T) == typeof(bool))   return (T)(object)bool.Parse(value);
            if (typeof(T) == typeof(int))    return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(float))  return (T)(object)float.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(double)) return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(T).IsEnum) return (T)Enum.Parse(typeof(T), value);
            throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
        }

        public static string ToString<T>(T value)
        {
            if (value == null) return null;
            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return value.ToString();
        }
    }
}
