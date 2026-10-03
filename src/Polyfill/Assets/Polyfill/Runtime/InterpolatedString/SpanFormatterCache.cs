using System;
using System.Collections.Generic;

namespace System.Text
{
    /// <summary>
    /// Writes a value into <paramref name="destination"/>; returns false when it does not fit.
    /// </summary>
    internal delegate bool SpanFormatter<T>(T value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider);

    /// <summary>
    /// One formatter per type, created on first use. Null for types without a TryFormat in the list.
    /// </summary>
    internal static class SpanFormatterCache<T>
    {
        internal static readonly SpanFormatter<T> Instance = (SpanFormatter<T>)SpanFormatters.Create(typeof(T));
    }

    /// <summary>
    /// Names of the defined values of an enum, as the runtime's own ToString() writes them (aliases included).
    /// Arrays rather than a Dictionary: IL2CPP generates this class for every T that reaches AppendFormatted, enum or not.
    /// </summary>
    internal static class EnumNames<T>
    {
        private static readonly T[] s_Values = typeof(T).IsEnum ? (T[])Enum.GetValues(typeof(T)) : null;
        private static readonly string[] s_Names = BuildNames(s_Values);

        private static string[] BuildNames(T[] values)
        {
            if (values == null) return null;
            var names = new string[values.Length];
            for (int i = 0; i < values.Length; i++) names[i] = values[i].ToString();
            return names;
        }

        internal static string Find(T value)
        {
            var values = s_Values;
            if (values == null) return null;
            var comparer = EqualityComparer<T>.Default;
            int index = comparer.GetHashCode(value);
            if ((uint)index < (uint)values.Length && comparer.Equals(values[index], value)) return s_Names[index];
            for (int i = 0; i < values.Length; i++)
                if (comparer.Equals(values[i], value)) return s_Names[i];
            return null;
        }
    }

    /// <summary>
    /// The value types formatted into a stack buffer, and the buffer sizes.
    /// </summary>
    internal static class SpanFormatters
    {
        internal const int BufferLength = 128;
        internal const int LargeBufferLength = 1024;

        internal static Delegate Create(Type type)
        {
            if (type == typeof(int)) return new SpanFormatter<int>((int v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(uint)) return new SpanFormatter<uint>((uint v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(long)) return new SpanFormatter<long>((long v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(ulong)) return new SpanFormatter<ulong>((ulong v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(short)) return new SpanFormatter<short>((short v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(ushort)) return new SpanFormatter<ushort>((ushort v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(byte)) return new SpanFormatter<byte>((byte v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(sbyte)) return new SpanFormatter<sbyte>((sbyte v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(float)) return new SpanFormatter<float>((float v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(double)) return new SpanFormatter<double>((double v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(decimal)) return new SpanFormatter<decimal>((decimal v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(bool)) return new SpanFormatter<bool>((bool v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n));
            if (type == typeof(char)) return new SpanFormatter<char>((char v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => { if (d.Length == 0) { n = 0; return false; } d[0] = v; n = 1; return true; });
            if (type == typeof(DateTime)) return new SpanFormatter<DateTime>((DateTime v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(DateTimeOffset)) return new SpanFormatter<DateTimeOffset>((DateTimeOffset v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(TimeSpan)) return new SpanFormatter<TimeSpan>((TimeSpan v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f, p));
            if (type == typeof(Guid)) return new SpanFormatter<Guid>((Guid v, Span<char> d, out int n, ReadOnlySpan<char> f, IFormatProvider p) => v.TryFormat(d, out n, f));
            return null;
        }
    }
}
