using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Compat
{
    public record Pt(int X, int Y);
    public readonly record struct Rs(int A);
    public class Cfg { public required string Name { get; init; } public int Port { get; init; } }
    public class Cfg2 { public required int A { get; set; } [SetsRequiredMembers] public Cfg2() { A = 7; } }
    public enum Mode { Off, On, Auto }

    [InterpolatedStringHandler]
    public ref struct SmokeHandler
    {
        StringBuilder _sb;
        public SmokeHandler(int literalLength, int formattedCount, bool enabled, out bool handlerEnabled)
        { handlerEnabled = enabled; _sb = enabled ? new StringBuilder(literalLength) : null; }
        public void AppendLiteral(string s) { _sb.Append(s); Smoke.AppendCalls++; }
        public void AppendFormatted<T>(T v) { _sb.Append(v); Smoke.AppendCalls++; }
        public void AppendFormatted<T>(T v, string format)
        { _sb.Append(v is IFormattable f ? f.ToString(format, CultureInfo.InvariantCulture) : v?.ToString()); Smoke.AppendCalls++; }
        public string Text => _sb?.ToString();
    }

    internal static class ModuleFlag
    {
        public static bool Ran;
        [ModuleInitializer] internal static void Init() { Ran = true; }
    }

#nullable enable
    internal sealed class LateInit
    {
        string? _text;
        [MemberNotNull(nameof(_text))] void Init() { _text = "abc"; }
        public int Length() { Init(); return _text.Length; }
        [MemberNotNullWhen(true, nameof(_text))] bool Has => _text != null;
        public int LengthIfAny() => Has ? _text.Length : -1;
    }
#nullable restore

    public static class Smoke
    {
        public static int AppendCalls;

        public static void Run(Action<string, string> log)
        {
            Check(log, "init+record+with", () => { var p = new Pt(1, 2); var q = p with { Y = 3 }; return q.Y == 3 && p != q && p == new Pt(1, 2) && p.ToString() == "Pt { X = 1, Y = 2 }" && new Rs(4).A == 4; });
            Check(log, "required", () => { var c = new Cfg { Name = "a", Port = 2 }; return c.Name == "a" && c.Port == 2 && new Cfg2().A == 7; });
            Check(log, "handler", () => { AppendCalls = 0; var s = Fmt(true, $"x={1} y={2.5f:F1}"); var on = AppendCalls; var t = Fmt(false, $"x={1} y={2.5f:F1}"); return s == "x=1 y=2.5" && on == 4 && t == null && AppendCalls == on; });
            Check(log, "append-interpolated", () => { var sb = new StringBuilder(); sb.AppendInterpolated($"x={1}|{Mode.Auto}|{'c'}|{"a",3}|{new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc):O}|"); return sb.ToString() == "x=1|Auto|c|  a|2026-10-03T00:00:00.0000000Z|"; });
            Check(log, "append-interpolated-provider", () => { var sb = new StringBuilder(); sb.AppendInterpolated(CultureInfo.InvariantCulture, $"{1.5f}|{7,3}|"); sb.Append(CultureInfo.InvariantCulture, $"{2.5m:F2}"); return sb.ToString() == "1.5|  7|2.50"; });
            Check(log, "caller-argument-expression", () => { int a = 2, b = 1; return Expr(a > b) == "a > b"; });
            Check(log, "skip-locals-init", () => Sum() == 28);
            Check(log, "module-initializer", () => ModuleFlag.Ran);
            Check(log, "member-not-null", () => new LateInit().Length() == 3 && new LateInit().LengthIfAny() == -1);
            Check(log, "parameter-attributes", () => { Pattern("a+"); Reflect(typeof(Smoke)); return Parameter(nameof(Pattern)).GetCustomAttribute<StringSyntaxAttribute>().Syntax == "Regex" && Parameter(nameof(Reflect)).GetCustomAttribute<DynamicallyAccessedMembersAttribute>().MemberTypes == DynamicallyAccessedMemberTypes.PublicProperties; });
            Check(log, "attribute-types", () => new StringSyntaxAttribute(StringSyntaxAttribute.Regex).Syntax == "Regex" && new MemberNotNullAttribute("a").Members.Length == 1 && new MemberNotNullWhenAttribute(true, "a", "b").Members.Length == 2 && new ExperimentalAttribute("X1").DiagnosticId == "X1" && new CollectionBuilderAttribute(typeof(Smoke), "M").MethodName == "M" && new UnscopedRefAttribute() != null);
            Check(log, "lang-raw-list-u8-newline-shift", () => { var raw = """a "b" c"""; int[] arr = { 1, 2, 3 }; int x = 5; var nl = $"{x
                + 1}"; return raw == "a \"b\" c" && arr is [1, .., 3] && "ab"u8.Length == 2 && nl == "6" && (-8 >>> 28) == 15; });
#if UNITY_6000_5_OR_NEWER
            SmokeCs12.Run(log);
#endif
        }

        public static void Check(Action<string, string> log, string name, Func<bool> f)
        {
            try { log(name, f() ? "PASS" : "FAIL"); }
            catch (Exception e) { log(name, "EX " + e.GetType().Name + ": " + e.Message); }
        }

        static string Fmt(bool enabled, [InterpolatedStringHandlerArgument("enabled")] ref SmokeHandler h) => h.Text;
        static string Expr(bool c, [CallerArgumentExpression("c")] string e = null) => e;
        [SkipLocalsInit] static unsafe int Sum() { Span<int> b = stackalloc int[8]; for (int i = 0; i < 8; i++) b[i] = i; int s = 0; for (int i = 0; i < 8; i++) s += b[i]; return s; }
        static void Pattern([StringSyntax(StringSyntaxAttribute.Regex)] string pattern) { }
        static void Reflect([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) { }
        static ParameterInfo Parameter(string method) => typeof(Smoke).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).GetParameters()[0];
    }
}
