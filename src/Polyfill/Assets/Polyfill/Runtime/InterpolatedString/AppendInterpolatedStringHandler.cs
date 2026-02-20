using System;
using System.Runtime.CompilerServices;

namespace System.Text
{
    /// <summary>
    /// Polyfill for StringBuilder interpolated string handler (C# 10 / .NET 6).
    /// Enables efficient <c>StringBuilder.Append($"{...}")</c> when the runtime does not provide it.
    /// Use via extension method: <c>sb.AppendInterpolated($"{x}")</c> or ensure extension is in scope for overload resolution.
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
            AppendFormatted(value, format: null);
        }

        public void AppendFormatted<T>(T value, string format)
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
        }

        public void AppendFormatted<T>(T value, int alignment) =>
            AppendFormatted(value, alignment, format: null);

        public void AppendFormatted<T>(T value, int alignment, string format)
        {
            if (alignment == 0)
            {
                AppendFormatted(value, format);
                return;
            }

            string s = FormatValue(value, format);
            int width = alignment < 0 ? -alignment : alignment;
            int paddingRequired = width - (s != null ? s.Length : 0);
            if (paddingRequired <= 0)
            {
                _stringBuilder.Append(s);
                return;
            }

            if (alignment < 0) // left-align: value then spaces
            {
                _stringBuilder.Append(s);
                _stringBuilder.Append(' ', paddingRequired);
            }
            else // right-align: spaces then value
            {
                _stringBuilder.Append(' ', paddingRequired);
                _stringBuilder.Append(s);
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
            if (alignment == 0)
            {
                _stringBuilder.Append(value);
                return;
            }

            bool leftAlign = alignment < 0;
            if (leftAlign) alignment = -alignment;
            int paddingRequired = alignment - value.Length;
            if (paddingRequired <= 0)
            {
                _stringBuilder.Append(value);
                return;
            }

            if (leftAlign)
            {
                _stringBuilder.Append(value);
                _stringBuilder.Append(' ', paddingRequired);
            }
            else
            {
                _stringBuilder.Append(' ', paddingRequired);
                _stringBuilder.Append(value);
            }
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
