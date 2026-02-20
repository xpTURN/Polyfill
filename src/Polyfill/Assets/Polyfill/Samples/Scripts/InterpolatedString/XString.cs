using System.Runtime.CompilerServices;

using xpTURN.Polyfill.Samples.InterpolatedStringHandler;

namespace xpTURN.Polyfill.Samples;

public static class XString
{
    /// <summary>
    /// Returns the interpolated string as the final string. Call as <c>XString.Format($"Hello, {name}")</c>.
    /// </summary>
    /// <param name="handler">Passed automatically by the compiler when the interpolated string is used. Do not pass explicitly.</param>
    public static string Format([InterpolatedStringHandlerArgument] ref XHandler handler)
    {
        return handler.GetString();
    }
}
