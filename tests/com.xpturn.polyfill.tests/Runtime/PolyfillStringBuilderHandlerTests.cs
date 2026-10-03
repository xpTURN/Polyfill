#if CSHARP_PREVIEW
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for AppendInterpolatedStringHandler and its StringBuilder extensions:
    /// output against string.Format (T1 · T5 · T7 · T8 · T9), the provider overloads (T2), no allocation on the fast path
    /// (T3 · T4 · T8 · T9), the slow path still allocating (T10) and the removed one-argument Append (T6).
    /// Allocation tests run the delegate once before measuring — the first call of each type allocates its cache.
    /// </summary>
    public class PolyfillStringBuilderHandlerTests
    {
        public enum Shape { Sphere, Box, Capsule }
        public enum E1 { Sphere, Box, Capsule, Cylinder }
        [Flags] public enum F1 { None = 0, A = 1, B = 2, C = 4, AB = A | B }
        public enum Al { One = 1, Uno = 1, Two = 2 }
        public enum Eb : byte { A, B, C, D }
        public enum Es : short { A = -2, B, C, D }
        public enum El : long { A = 1L << 40, B, C, D }

        public struct Formattable : IFormattable
        {
            public int Value;
            public string ToString(string format, IFormatProvider formatProvider) => "F" + Value.ToString(format, formatProvider);
            public override string ToString() => "P" + Value;
        }

        public sealed class Ref { public int Value; public override string ToString() => "R" + Value; }

        sealed class EchoFormatter : IFormatProvider, ICustomFormatter
        {
            public int Calls;
            public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;
            public string Format(string format, object arg, IFormatProvider formatProvider) { Calls++; return "<" + format + "|" + Convert.ToString(arg, CultureInfo.InvariantCulture) + ">"; }
        }

        sealed class NullFormatter : IFormatProvider, ICustomFormatter
        {
            public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;
            public string Format(string format, object arg, IFormatProvider formatProvider) => null;
        }

        static void AssertNoAllocation(TestDelegate append, string what)
        {
            append();
            Assert.That(append, Is.Not.AllocatingGCMemory(), what);
        }

        // ------------------------------------------------------------------ T1
        static readonly int[] Alignments = { 0, 12, -12, 2 };
        static readonly string[] IntFormats = { null, "", "D", "D8", "X", "x8", "N0", "N2", "C", "P", "E3", "G", "0000", "#,##0.00", "R" };
        static readonly string[] FloatFormats = { null, "", "F2", "F0", "E", "e3", "G", "G9", "G17", "R", "N3", "P1", "0.00#", "#,##0.0", "F40" };
        static readonly string[] DecimalFormats = { null, "", "F2", "N4", "C", "G", "0.###", "E2" };
        static readonly string[] DateFormats = { null, "", "O", "o", "s", "u", "yyyy-MM-dd HH:mm:ss.fff", "D", "t", "Q" };
        static readonly string[] SpanFormats = { null, "", "c", "g", "G", @"hh\:mm\:ss", "Q" };
        static readonly string[] GuidFormats = { null, "", "D", "N", "B", "P", "X", "Q" };
        static readonly string[] MiscFormats = { null, "", "X" };

        IFormatProvider[] _providers;
        int _checks;
        List<string> _mismatches;

        void Check<T>(T value, string[] formats)
        {
            foreach (var provider in _providers)
                foreach (var format in formats)
                    foreach (var alignment in Alignments)
                    {
                        string expected, actual;
                        try
                        {
                            string composite = "{0" + (alignment != 0 ? "," + alignment.ToString(CultureInfo.InvariantCulture) : "") + (format != null ? ":" + format : "") + "}";
                            expected = string.Format(provider, composite, value);
                        }
                        catch (Exception e) { expected = "EX:" + e.GetType().Name; }
                        try
                        {
                            var sb = new StringBuilder();
                            var h = provider == null ? new AppendInterpolatedStringHandler(0, 1, sb) : new AppendInterpolatedStringHandler(0, 1, sb, provider);
                            h.AppendFormatted(value, alignment, format);
                            actual = sb.ToString();
                        }
                        catch (Exception e) { actual = "EX:" + e.GetType().Name; }
                        _checks++;
                        if (expected != actual && _mismatches.Count < 20)
                            _mismatches.Add(typeof(T).Name + " " + value + " fmt=" + (format ?? "null") + " al=" + alignment + " prov=" + (provider == null ? "null" : provider is CultureInfo ci ? ci.Name : provider.GetType().Name) + " cur=" + CultureInfo.CurrentCulture.Name + " exp=[" + expected + "] act=[" + actual + "]");
                    }
        }

        void ParityPass()
        {
            Check(0, IntFormats); Check(-1, IntFormats); Check(12345, IntFormats); Check(int.MaxValue, IntFormats); Check(int.MinValue, IntFormats);
            Check(42u, IntFormats); Check(uint.MaxValue, IntFormats);
            Check(-9876543210L, IntFormats); Check(long.MaxValue, IntFormats); Check(long.MinValue, IntFormats);
            Check(ulong.MaxValue, IntFormats);
            Check((short)-123, IntFormats); Check((ushort)65535, IntFormats); Check((byte)200, IntFormats); Check((sbyte)-100, IntFormats);
            Check(0f, FloatFormats); Check(-0f, FloatFormats); Check(1.5f, FloatFormats); Check(-3.25f, FloatFormats); Check(0.1f, FloatFormats); Check(1e-7f, FloatFormats); Check(float.MaxValue, FloatFormats); Check(float.Epsilon, FloatFormats); Check(float.NaN, FloatFormats); Check(float.PositiveInfinity, FloatFormats);
            Check(0d, FloatFormats); Check(-0d, FloatFormats); Check(0.1, FloatFormats); Check(-3.25, FloatFormats); Check(1e-300, FloatFormats); Check(double.MaxValue, FloatFormats); Check(double.NaN, FloatFormats); Check(double.NegativeInfinity, FloatFormats); Check(123456789.125, FloatFormats);
            Check(0m, DecimalFormats); Check(1.5m, DecimalFormats); Check(-79228162514264337593543950335m, DecimalFormats); Check(0.0000000000000000000000000001m, DecimalFormats);
            Check(true, MiscFormats); Check(false, MiscFormats);
            Check('a', MiscFormats); Check('é', MiscFormats);
            Check(new DateTime(2026, 10, 3, 12, 34, 56, 789, DateTimeKind.Utc), DateFormats); Check(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local), DateFormats); Check(DateTime.MinValue, DateFormats);
            Check(new DateTimeOffset(2026, 10, 3, 12, 34, 56, TimeSpan.FromHours(9)), DateFormats);
            Check(TimeSpan.Zero, SpanFormats); Check(new TimeSpan(1, 2, 3, 4, 5), SpanFormats); Check(TimeSpan.MinValue, SpanFormats);
            Check(new Guid("01234567-89ab-cdef-0123-456789abcdef"), GuidFormats);
            Check(Shape.Box, MiscFormats); Check((int?)7, IntFormats); Check(new Formattable { Value = 5 }, IntFormats); Check(new Ref { Value = 3 }, MiscFormats); Check("str", MiscFormats); Check((object)17, IntFormats);
        }

        [Test]
        public void T1_Handler_MatchesStringFormat()
        {
            _providers = new IFormatProvider[] { null, CultureInfo.InvariantCulture, new CultureInfo("de-DE"), new CultureInfo("fr-FR"), new CultureInfo("ar-SA"), new EchoFormatter() };
            _checks = 0; _mismatches = new List<string>();
            var saved = CultureInfo.CurrentCulture;
            try
            {
                foreach (var name in new[] { "", "en-US", "de-DE", "ar-SA" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(name);
                    ParityPass();
                }
            }
            finally { CultureInfo.CurrentCulture = saved; }
            Assert.That(_mismatches, Is.Empty, string.Join("\n", _mismatches));
            Assert.That(_checks, Is.GreaterThan(60000));
        }

        // ------------------------------------------------------------------ T2
        [Test]
        public void T2_ProviderOverloads_UseTheHandler_AndReturnTheSameBuilder()
        {
            var sb = new StringBuilder();
            var p = new EchoFormatter();
            int i = 7;
            Assert.That(sb.Append(p, $"a{i}b"), Is.SameAs(sb));
            Assert.That(p.Calls, Is.EqualTo(1));
            Assert.That(sb.ToString(), Is.EqualTo("a<|7>b"));
            sb.Clear();
            Assert.That(sb.AppendInterpolated(p, $"c{i}d"), Is.SameAs(sb));
            Assert.That(p.Calls, Is.EqualTo(2));
            Assert.That(sb.ToString(), Is.EqualTo("c<|7>d"));
            sb.Clear();
            Assert.That(sb.AppendInterpolated($"e{i}f"), Is.SameAs(sb));
            Assert.That(sb.ToString(), Is.EqualTo("e7f"));
        }

        // ------------------------------------------------------------------ T3
        static void AssertNoAllocation<T>(T value, string format, bool formatAlways = false)
        {
            var sb = new StringBuilder(256);
            int[] alignments = { 0, 0, 8, -8 };
            string plain = formatAlways ? format : null;
            string[] formats = { plain, format, plain, format };
            for (int n = 0; n < 4; n++)
            {
                int alignment = alignments[n];
                string f = formats[n];
                TestDelegate append = () =>
                {
                    sb.Length = 0;
                    var h = new AppendInterpolatedStringHandler(0, 1, sb);
                    h.AppendFormatted(value, alignment, f);
                };
                append();
                Assert.That(append, Is.Not.AllocatingGCMemory(), typeof(T).Name + " alignment=" + alignment + " format=" + (f ?? "null"));
            }
        }

        static readonly string[] ListedTypes = { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal", "bool", "char", "DateTime", "DateTimeOffset", "TimeSpan", "Guid" };

        [Test]
        public void T3_ListedTypes_DoNotAllocate([ValueSource(nameof(ListedTypes))] string type)
        {
            int seed = Environment.TickCount & 0x7F;
            switch (type)
            {
                case "byte": AssertNoAllocation((byte)(seed + 1), "X2"); break;
                case "sbyte": AssertNoAllocation((sbyte)(seed - 64), "D3"); break;
                case "short": AssertNoAllocation((short)(seed * -97), "N0"); break;
                case "ushort": AssertNoAllocation((ushort)(seed * 997), "D6"); break;
                case "int": AssertNoAllocation(seed * 100003, "X8"); break;
                case "uint": AssertNoAllocation((uint)seed * 2654435761u, "N0"); break;
                case "long": AssertNoAllocation(seed * -1000000007L, "N2"); break;
                case "ulong": AssertNoAllocation((ulong)seed * 1000000000000UL, "N0"); break;
                case "float": AssertNoAllocation(seed * 0.37f, "F3"); break;
                case "double": AssertNoAllocation(seed * 1234.37, "E3"); break;
                case "decimal": AssertNoAllocation(seed * 1.25m, "N4"); break;
                case "bool": AssertNoAllocation((seed & 1) == 0, null); break;
                case "char": AssertNoAllocation((char)('a' + (seed & 7)), null); break;
                case "DateTime": AssertNoAllocation(new DateTime(2026, 10, 3, 12, 34, 56, DateTimeKind.Utc).AddSeconds(seed), "O", formatAlways: true); break;
                case "DateTimeOffset": AssertNoAllocation(new DateTimeOffset(2026, 10, 3, 12, 34, 56, TimeSpan.FromHours(9)).AddSeconds(seed), "O", formatAlways: true); break;
                case "TimeSpan": AssertNoAllocation(TimeSpan.FromMilliseconds(seed * 12345), "c"); break;
                case "Guid": AssertNoAllocation(new Guid(seed, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), "N"); break;
                default: Assert.Fail("unknown type " + type); break;
            }
        }

        // ------------------------------------------------------------------ T4
        static readonly string[] s_Names = { "abc", "defg", "hi", "jklmn" };

        [Test]
        public void T4_Strings_DoNotAllocate()
        {
            var sb = new StringBuilder(256);
            int i = Environment.TickCount & 3;
            AssertNoAllocation(() => { sb.Length = 0; string s = s_Names[i]; sb.AppendInterpolated($"name={s}"); }, "{s}");
            AssertNoAllocation(() => { sb.Length = 0; string s = s_Names[i]; sb.AppendInterpolated($"name={s,-8}|"); }, "{s,-8}");
        }

        [Test]
        public void T4_Spans_DoNotAllocate()
        {
            var sb = new StringBuilder(256);
            int i = 1 + (Environment.TickCount & 3);
            string text = "abcdef";
            AssertNoAllocation(() => { sb.Length = 0; ReadOnlySpan<char> s = text.AsSpan(0, i); sb.AppendInterpolated($"[{s}]"); }, "{span}");
            AssertNoAllocation(() => { sb.Length = 0; ReadOnlySpan<char> s = text.AsSpan(0, i); sb.AppendInterpolated($"[{s,8}]"); }, "{span,8}");
        }

        [Test]
        public void T4_MixedHoles_DoNotAllocate()
        {
            var sb = new StringBuilder(256);
            int i = Environment.TickCount & 0x3F;
            AssertNoAllocation(() => { sb.Length = 0; int j = i + 1, k = i + 2; float f = i * 0.5f; string s = s_Names[i & 3]; sb.AppendInterpolated($"[{i},{j},{k}] {f} {s}"); }, "mixed");
        }

        [Test]
        public void T4_ProviderOverloads_DoNotAllocate()
        {
            var sb = new StringBuilder(256);
            int i = Environment.TickCount & 0x3F;
            AssertNoAllocation(() => { sb.Length = 0; float f = i * 0.5f; sb.AppendInterpolated(CultureInfo.InvariantCulture, $"{f:F2}"); }, "AppendInterpolated(provider)");
            AssertNoAllocation(() => { sb.Length = 0; float f = i * 0.5f; sb.Append(CultureInfo.InvariantCulture, $"{i} {f}"); }, "Append(provider)");
        }

        // ------------------------------------------------------------------ T5
        [Test]
        public void T5_SlowPath_KeepsTheOutput()
        {
            var sb = new StringBuilder();
            // custom formatter returning null appends nothing (0.3.1 and the BCL handler) - string.Format would fall back
            var nf = new NullFormatter();
            var h = new AppendInterpolatedStringHandler(1, 1, sb, nf);
            h.AppendLiteral("[");
            h.AppendFormatted(42);
            h.AppendLiteral("]");
            Assert.That(sb.ToString(), Is.EqualTo("[]"));
            // a result longer than both stack buffers goes the slow way with the same output
            int k = 1 + (Environment.TickCount & 7);
            string format = "0'" + new string('x', 1100) + "'";
            sb.Clear();
            var h2 = new AppendInterpolatedStringHandler(0, 1, sb);
            h2.AppendFormatted(k, 0, format);
            Assert.That(sb.ToString(), Is.EqualTo(k.ToString(format)));
            // span alignment keeps string.Format's padding
            sb.Clear();
            ReadOnlySpan<char> span = "ab".AsSpan();
            sb.AppendInterpolated($"[{span,5}][{span,-5}]");
            Assert.That(sb.ToString(), Is.EqualTo(string.Format("[{0,5}][{0,-5}]", "ab")));
        }

        // ------------------------------------------------------------------ T6
        [Test]
        public void T6_OneArgumentAppend_IsGone()
        {
            var appends = new List<MethodInfo>();
            foreach (var m in typeof(StringBuilderExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (m.Name == "Append" && m.GetParameters().Length == 2)
                    appends.Add(m);
            Assert.That(appends, Is.Empty);
        }

        // ------------------------------------------------------------------ T7
        [Test]
        public void T7_CompilerShapedHoles_MatchStringFormat()
        {
            int i = 40 + (Environment.TickCount & 3); string s = "abc"; object o = 3.5; string ns = null; object no = null;
            var mismatches = new List<string>();
            void Same(string what, string actual, string expected) { if (actual != expected) mismatches.Add(what + " exp=[" + expected + "] act=[" + actual + "]"); }
            foreach (var p in new IFormatProvider[] { null, CultureInfo.InvariantCulture, new CultureInfo("de-DE"), new EchoFormatter() })
            {
                var sb = new StringBuilder();
                sb.Append(p, $"{i}|{i:X4}|{i,6}|{i,-6:X}|{s}|{s,6}|{s,-6}|{o}|{o,6}|{o:F2}|{ns}|{no}");
                Same("Append(" + (p == null ? "null" : p.GetType().Name) + ")", sb.ToString(), string.Format(p, "{0}|{0:X4}|{0,6}|{0,-6:X}|{1}|{1,6}|{1,-6}|{2}|{2,6}|{2:F2}|{3}|{4}", i, s, o, ns, no));
            }
            {
                var sb = new StringBuilder();
                sb.AppendInterpolated($"{i}|{i:X4}|{i,6}|{i,-6:X}|{s}|{s,6}|{s,-6}|{o}|{o,6}|{o:F2}|{ns}|{no}");
                Same("AppendInterpolated", sb.ToString(), string.Format("{0}|{0:X4}|{0,6}|{0,-6:X}|{1}|{1,6}|{1,-6}|{2}|{2,6}|{2:F2}|{3}|{4}", i, s, o, ns, no));
            }
            {
                var sb = new StringBuilder();
                ReadOnlySpan<char> span = "xy".AsSpan();
                sb.AppendInterpolated($"{span}|{span,5}|{span,-5}");
                Same("span", sb.ToString(), string.Format("{0}|{0,5}|{0,-5}", "xy"));
            }
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        // ------------------------------------------------------------------ T8
        [Test]
        public void T8_Enums_MatchStringFormat()
        {
            var mismatches = new List<string>(); int checks = 0;
            string[] formats = { null, "", "G", "g", "D", "X", "F" };
            int[] alignments = { 0, 10, -10, 2 };
            IFormatProvider[] providers = { null, CultureInfo.InvariantCulture, new CultureInfo("de-DE"), new EchoFormatter() };
            void CheckEnum<T>(T value)
            {
                foreach (var p in providers)
                    foreach (var a in alignments)
                        foreach (var f in formats)
                        {
                            checks++;
                            string composite = "{0" + (a != 0 ? "," + a.ToString(CultureInfo.InvariantCulture) : "") + (f != null ? ":" + f : "") + "}";
                            string expected, actual;
                            try { expected = string.Format(p, composite, value); } catch (Exception e) { expected = "EX:" + e.GetType().Name; }
                            try
                            {
                                var sb = new StringBuilder();
                                var h = p == null ? new AppendInterpolatedStringHandler(0, 1, sb) : new AppendInterpolatedStringHandler(0, 1, sb, p);
                                h.AppendFormatted(value, a, f);
                                actual = sb.ToString();
                            }
                            catch (Exception e) { actual = "EX:" + e.GetType().Name; }
                            if (expected != actual && mismatches.Count < 10) mismatches.Add(typeof(T).Name + " " + value + " " + composite + " exp=[" + expected + "] act=[" + actual + "]");
                        }
            }
            foreach (E1 e in new[] { E1.Sphere, E1.Cylinder, (E1)7, (E1)(-1) }) CheckEnum(e);
            foreach (F1 e in new[] { F1.None, F1.AB, F1.A | F1.C, (F1)8 }) CheckEnum(e);
            foreach (Al e in new[] { Al.One, Al.Uno, Al.Two }) CheckEnum(e);
            foreach (Eb e in new[] { Eb.A, Eb.D, (Eb)200 }) CheckEnum(e);
            foreach (Es e in new[] { Es.A, Es.D, (Es)(-100) }) CheckEnum(e);
            foreach (El e in new[] { El.A, El.D, (El)7 }) CheckEnum(e);
            CheckEnum<E1?>(E1.Box); CheckEnum<E1?>(null);
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
            Assert.That(checks, Is.EqualTo(22 * 4 * 4 * 7));
        }

        [Test]
        public void T8_Enums_DoNotAllocate()
        {
            var sb = new StringBuilder(256);
            int seed = Environment.TickCount & 3;
            AssertNoAllocation(() => { sb.Length = 0; E1 e = (E1)seed; sb.AppendInterpolated($"{e}"); }, "{e}");
            AssertNoAllocation(() => { sb.Length = 0; E1 e = (E1)seed; sb.AppendInterpolated($"{e,10}"); }, "{e,10}");
            AssertNoAllocation(() => { sb.Length = 0; Eb e = (Eb)seed; sb.AppendInterpolated($"{e:G}"); }, "{byte enum:G}");
            AssertNoAllocation(() => { sb.Length = 0; Es e = (Es)(seed - 2); sb.AppendInterpolated($"{e,-6}"); }, "{short enum,-6}");
            AssertNoAllocation(() => { sb.Length = 0; El e = (El)((1L << 40) + seed); sb.Append(CultureInfo.InvariantCulture, $"{e}"); }, "Append(provider, {long enum})");
        }

        // ------------------------------------------------------------------ T9
        [Test]
        public void T9_LongResults_UpTo1024_DoNotAllocate()
        {
            var sb = new StringBuilder(2048);
            int seed = 1 + (Environment.TickCount & 7);
            AssertNoAllocation(() => { sb.Length = 0; double d = seed * 1e100; sb.AppendInterpolated($"{d:F40}"); }, "{d:F40} (about 140)");
            AssertNoAllocation(() => { sb.Length = 0; double d = seed * 1e300; sb.AppendInterpolated($"{d:N2}"); }, "{d:N2} (about 400)");
            AssertNoAllocation(() => { sb.Length = 0; double d = double.MaxValue / seed; sb.AppendInterpolated($"{d:P99}"); }, "{d:P99} (about 516)");
        }

        [Test]
        public void T9_ResultsOver1024_KeepTheOutput()
        {
            int k = 1 + (Environment.TickCount & 7);
            string format = "0'" + new string('x', 1100) + "'";
            var a = new StringBuilder();
            var h = new AppendInterpolatedStringHandler(0, 1, a);
            h.AppendFormatted(k, 0, format);
            Assert.That(a.ToString(), Is.EqualTo(k.ToString(format)));
            double big = k * 1e300;
            var b = new StringBuilder();
            b.AppendInterpolated($"{big,-500:N2}|{big:F2}");
            Assert.That(b.ToString(), Is.EqualTo(string.Format("{0,-500:N2}|{0:F2}", big)));
        }

        // ------------------------------------------------------------------ T10
        [Test]
        public void T10_SlowPath_Allocates_PositiveControl()
        {
            var sb = new StringBuilder(256);
            int seed = Environment.TickCount & 3;
            TestDelegate nullable = () => { sb.Length = 0; int? n = seed; sb.AppendInterpolated($"{n}"); };
            nullable();
            Assert.That(nullable, Is.AllocatingGCMemory(), "{int?}");
            TestDelegate enumD = () => { sb.Length = 0; E1 e = (E1)seed; sb.AppendInterpolated($"{e:D}"); };
            enumD();
            Assert.That(enumD, Is.AllocatingGCMemory(), "{enum:D}");
        }
    }
}
#endif
