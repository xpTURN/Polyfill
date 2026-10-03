#if CSHARP_PREVIEW
using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace xpTURN.Polyfill.Samples.InterpolatedStringHandler;

/// <summary>
/// Handler for the sample XLogger and XString. Formats each hole through the package's
/// <see cref="AppendInterpolatedStringHandler"/> into a StringBuilder reused on the same thread;
/// a call nested inside a hole gets its own StringBuilder.
/// </summary>
[InterpolatedStringHandler]
public ref struct XHandler
{
    [ThreadStatic] private static StringBuilder t_cached;

    private readonly StringBuilder _sb;
    private AppendInterpolatedStringHandler _inner;

    public XHandler(int literalLength, int formattedCount)
    {
        _sb = t_cached ?? new StringBuilder(256);
        t_cached = null;
        _inner = new AppendInterpolatedStringHandler(literalLength, formattedCount, _sb);
    }

    /// <summary>Returns the text and keeps the StringBuilder for the next call on this thread.</summary>
    public string GetString()
    {
        string text = _sb.ToString();
        _sb.Clear();
        t_cached = _sb;
        return text;
    }

    public void AppendLiteral(string value) => _inner.AppendLiteral(value);
    public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);
    public void AppendFormatted<T>(T value, string format) => _inner.AppendFormatted(value, format);
    public void AppendFormatted<T>(T value, int alignment) => _inner.AppendFormatted(value, alignment);
    public void AppendFormatted<T>(T value, int alignment, string format) => _inner.AppendFormatted(value, alignment, format);
}
#endif
