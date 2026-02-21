#if CSHARP_PREVIEW
using System.Runtime.CompilerServices;

using UnityEngine;

using xpTURN.Polyfill.Samples.InterpolatedStringHandler;

namespace xpTURN.Polyfill.Samples;

/// <summary>
/// Logging utility. Pass interpolated strings directly as arguments.
/// </summary>
/// <example><code>xLogger.Log($"time={Time.time}");</code></example>
public sealed class XLogger
{
    /// <summary>
    /// Logs a message. Pass only the interpolated string; the compiler fills the handler parameter.
    /// </summary>
    /// <param name="handler">Generated and passed by the compiler when handling <c>Log($"...")</c>. Do not pass explicitly.</param>
    public static void Log([InterpolatedStringHandlerArgument] ref XHandler handler)
    {
        Debug.Log(handler.GetString());
    }

    /// <summary>
    /// Logs a warning. Pass only the interpolated string; the compiler fills the handler parameter.
    /// </summary>
    /// <param name="handler">Generated and passed by the compiler when handling <c>LogWarning($"...")</c>. Do not pass explicitly.</param>
    public static void LogWarning([InterpolatedStringHandlerArgument] ref XHandler handler)
    {
        Debug.LogWarning(handler.GetString());
    }

    /// <summary>
    /// Logs an error. Pass only the interpolated string; the compiler fills the handler parameter.
    /// </summary>
    /// <param name="handler">Generated and passed by the compiler when handling <c>LogError($"...")</c>. Do not pass explicitly.</param>
    public static void LogError([InterpolatedStringHandlerArgument] ref XHandler handler)
    {
        Debug.LogError(handler.GetString());
    }

    /// <summary>
    /// Logs an assertion. Pass only the interpolated string; the compiler fills the handler parameter.
    /// </summary>
    /// <param name="handler">Generated and passed by the compiler when handling <c>LogAssertion($"...")</c>. Do not pass explicitly.</param>
    public static void LogAssertion([InterpolatedStringHandlerArgument] ref XHandler handler)
    {
        Debug.LogAssertion(handler.GetString());
    }
}
#endif