using System;
using System.Runtime.CompilerServices;

namespace System.Text
{
    /// <summary>
    /// Interpolated string handler that appends to a <see cref="StringBuilder"/>.
    /// Used by <c>sb.AppendInterpolated($"…")</c> and <c>sb.Append(provider, $"…")</c>.
    /// Integral and floating-point types, <c>decimal</c>, <c>bool</c>, <c>char</c>, <c>DateTime</c>, <c>DateTimeOffset</c>, <c>TimeSpan</c> and <c>Guid</c>
    /// are formatted into a stack buffer, and enum values with no format or <c>G</c> are written from a cached name table.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct AppendInterpolatedStringHandler
    {
        private readonly StringBuilder _stringBuilder;
        private readonly IFormatProvider _provider;
        private readonly bool _hasCustomFormatter;

        /// <summary>
        /// Creates a handler used to append an interpolated string into the given StringBuilder.
        /// Called by the compiler; arguments are not validated.
        /// </summary>
        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, StringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
            _provider = null;
            _hasCustomFormatter = false;
        }

        /// <summary>
        /// Creates a handler used to append an interpolated string into the given StringBuilder with format provider.
        /// Called by the compiler; arguments are not validated.
        /// </summary>
        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, StringBuilder stringBuilder, IFormatProvider provider)
        {
            _stringBuilder = stringBuilder;
            _provider = provider;
            _hasCustomFormatter = provider != null && HasCustomFormatter(provider);
        }

        private static bool HasCustomFormatter(IFormatProvider provider)
        {
            var formatter = provider.GetFormat(typeof(ICustomFormatter));
            return formatter != null;
        }

        /// <summary>Writes the literal part of the interpolated string.</summary>
        public void AppendLiteral(string value)
        {
            _stringBuilder.Append(value);
        }

        #region AppendFormatted T

        public void AppendFormatted<T>(T value)
        {
            AppendFormatted(value, 0, format: null);
        }

        public void AppendFormatted<T>(T value, string format)
        {
            AppendFormatted(value, 0, format);
        }

        public void AppendFormatted<T>(T value, int alignment) =>
            AppendFormatted(value, alignment, format: null);

        public void AppendFormatted<T>(T value, int alignment, string format)
        {
            if (!_hasCustomFormatter)
            {
                var formatter = SpanFormatterCache<T>.Instance;
                if (formatter != null)
                {
                    Span<char> buffer = stackalloc char[SpanFormatters.BufferLength];
                    if (formatter(value, buffer, out int written, format, _provider))
                    {
                        AppendPadded(_stringBuilder, buffer.Slice(0, written), alignment);
                        return;
                    }
                    Span<char> large = stackalloc char[SpanFormatters.LargeBufferLength];
                    if (formatter(value, large, out written, format, _provider))
                    {
                        AppendPadded(_stringBuilder, large.Slice(0, written), alignment);
                        return;
                    }
                }
                else if (string.IsNullOrEmpty(format) || format == "G" || format == "g")
                {
                    string name = EnumNames<T>.Find(value);
                    if (name != null)
                    {
                        AppendPadded(_stringBuilder, name, alignment);
                        return;
                    }
                }
            }

            if (alignment == 0)
            {
                if (_hasCustomFormatter)
                {
                    AppendCustomFormatter(value, format);
                    return;
                }
                if (value is IFormattable formattable)
                {
                    _stringBuilder.Append(formattable.ToString(format, _provider));
                    return;
                }
                if (value != null)
                    _stringBuilder.Append(value.ToString());
                return;
            }

            string s = FormatValue(value, format);
            AppendPadded(_stringBuilder, s, alignment);
        }

        private static void AppendPadded(StringBuilder sb, ReadOnlySpan<char> s, int alignment)
        {
            int width = alignment < 0 ? -alignment : alignment;
            int paddingRequired = width - s.Length;
            if (paddingRequired <= 0)
            {
                sb.Append(s);
                return;
            }

            if (alignment < 0) // left-align: value then spaces
            {
                sb.Append(s);
                sb.Append(' ', paddingRequired);
            }
            else // right-align: spaces then value
            {
                sb.Append(' ', paddingRequired);
                sb.Append(s);
            }
        }

        private static void AppendPadded(StringBuilder sb, string s, int alignment)
        {
            int width = alignment < 0 ? -alignment : alignment;
            int paddingRequired = width - (s != null ? s.Length : 0);
            if (paddingRequired <= 0)
            {
                sb.Append(s);
                return;
            }

            if (alignment < 0) // left-align: value then spaces
            {
                sb.Append(s);
                sb.Append(' ', paddingRequired);
            }
            else // right-align: spaces then value
            {
                sb.Append(' ', paddingRequired);
                sb.Append(s);
            }
        }

        private string FormatValue<T>(T value, string format)
        {
            if (_hasCustomFormatter && _provider != null)
            {
                var formatter = (ICustomFormatter)_provider.GetFormat(typeof(ICustomFormatter));
                if (formatter != null)
                    return formatter.Format(format, value, _provider);
            }
            if (value is IFormattable formattable)
                return formattable.ToString(format, _provider);
            return value != null ? value.ToString() : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendCustomFormatter<T>(T value, string format)
        {
            if (_provider == null) return;
            var formatter = (ICustomFormatter)_provider.GetFormat(typeof(ICustomFormatter));
            if (formatter != null)
                _stringBuilder.Append(formatter.Format(format, value, _provider));
        }

        #endregion

        #region AppendFormatted ReadOnlySpan<char>

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
            _stringBuilder.Append(value);
        }

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string format = null)
        {
            AppendPadded(_stringBuilder, value, alignment);
        }

        #endregion

        #region AppendFormatted string

        public void AppendFormatted(string value)
        {
            if (!_hasCustomFormatter)
            {
                _stringBuilder.Append(value);
                return;
            }
            AppendFormatted<string>(value, format: null);
        }

        public void AppendFormatted(string value, int alignment = 0, string format = null) =>
            AppendFormatted<string>(value, alignment, format);

        #endregion

        #region AppendFormatted object

        public void AppendFormatted(object value, int alignment = 0, string format = null) =>
            AppendFormatted<object>(value, alignment, format);

        #endregion
    }
}
