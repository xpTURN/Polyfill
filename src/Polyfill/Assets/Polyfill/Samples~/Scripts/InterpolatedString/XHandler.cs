#if CSHARP_PREVIEW
using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace xpTURN.Polyfill.Samples.InterpolatedStringHandler;

/// <summary>Handler for static Log. Uses a singleton StringBuilder with no extra arguments.</summary>
[InterpolatedStringHandler]
public ref struct XHandler
{
    private static readonly StringBuilder _sb = new StringBuilder();
    public string GetString() => _sb.ToString();

    public XHandler(int literalLength, int formattedCount) => _sb.Clear();

    public void AppendLiteral(string value) => _sb.Append(value);
    public void AppendFormatted<T>(T value) => _sb.Append(value != null ? value.ToString() : "");
    public void AppendFormatted(string value) => _sb.Append(value);
    public void AppendFormatted(object value, int alignment = 0, string format = null) => _sb.Append(value != null ? value.ToString() : "");
    public void AppendFormatted<T>(T value, string format) => _sb.Append(value is IFormattable f ? f.ToString(format, null) : (value?.ToString() ?? ""));
}
#endif