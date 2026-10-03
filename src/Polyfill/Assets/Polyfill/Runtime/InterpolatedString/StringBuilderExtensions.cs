using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Text
{
    /// <summary>
    /// Extension methods that append an interpolated string to a StringBuilder through <see cref="AppendInterpolatedStringHandler"/>:
    /// <c>sb.AppendInterpolated($"…")</c> and <c>sb.Append(provider, $"…")</c>.
    /// A plain <c>sb.Append($"…")</c> binds to the instance <c>Append(string)</c> and does not use the handler.
    /// </summary>
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Appends the interpolated string to the builder through <see cref="AppendInterpolatedStringHandler"/>.
        /// </summary>
        public static StringBuilder AppendInterpolated(
            this StringBuilder sb,
            [InterpolatedStringHandlerArgument("sb")] ref AppendInterpolatedStringHandler handler) => sb;

        /// <summary>
        /// Appends the interpolated string to the builder with the given format provider, through <see cref="AppendInterpolatedStringHandler"/>.
        /// </summary>
        public static StringBuilder AppendInterpolated(
            this StringBuilder sb,
            IFormatProvider provider,
            [InterpolatedStringHandlerArgument("sb", "provider")] ref AppendInterpolatedStringHandler handler) => sb;

        /// <summary>
        /// Appends the interpolated string to the builder with the given format provider.
        /// </summary>
        public static StringBuilder Append(
            this StringBuilder sb,
            IFormatProvider provider,
            [InterpolatedStringHandlerArgument("sb", "provider")] ref AppendInterpolatedStringHandler handler) => sb;
    }
}
