using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Text
{
    /// <summary>
    /// Extension methods for StringBuilder to support interpolated string Append when the runtime does not provide it.
    /// Enables <c>sb.Append($"{value}")</c> via polyfilled <see cref="AppendInterpolatedStringHandler"/>.
    /// </summary>
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Appends the interpolated string to the builder. Compiler uses <see cref="AppendInterpolatedStringHandler"/>.
        /// </summary>
        public static StringBuilder Append(
            this StringBuilder sb,
            [InterpolatedStringHandlerArgument("sb")] ref AppendInterpolatedStringHandler handler) => sb;

        /// <summary>
        /// Appends the interpolated string to the builder with the given format provider.
        /// </summary>
        public static StringBuilder Append(
            this StringBuilder sb,
            IFormatProvider provider,
            [InterpolatedStringHandlerArgument("sb", "provider")] ref AppendInterpolatedStringHandler handler) => sb;
    }
}
