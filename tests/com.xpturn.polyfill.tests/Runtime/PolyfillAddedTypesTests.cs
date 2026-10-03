#if CSHARP_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    [InterpolatedStringHandler]
    public ref struct CountingHandler
    {
        StringBuilder _sb;
        public CountingHandler(int literalLength, int formattedCount, bool enabled, out bool handlerEnabled)
        { handlerEnabled = enabled; _sb = enabled ? new StringBuilder(literalLength) : null; }
        public void AppendLiteral(string s) { _sb.Append(s); PolyfillAddedTypesTests.Appends++; }
        public void AppendFormatted<T>(T v) { _sb.Append(v); PolyfillAddedTypesTests.Appends++; }
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

    /// <summary>
    /// Tests for the types without a dedicated test file: InterpolatedStringHandler, ModuleInitializer,
    /// MemberNotNull, StringSyntax, DynamicallyAccessedMembers, the StringBuilder.AppendInterpolated extension, and —
    /// on Roslyn 4.10 (Unity 6000.5 and later) — CollectionBuilder and Experimental.
    /// </summary>
    public class PolyfillAddedTypesTests
    {
        public static int Appends;

        static string Format(bool enabled, [InterpolatedStringHandlerArgument("enabled")] ref CountingHandler h) => h.Text;
        static void Pattern([StringSyntax(StringSyntaxAttribute.Regex)] string pattern) { }
        static void Reflect([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) { }

        [Test]
        public void InterpolatedStringHandler_Enabled_AppendsEachPart()
        {
            Appends = 0;
            Assert.That(Format(true, $"a={1} b={2}"), Is.EqualTo("a=1 b=2"));
            Assert.That(Appends, Is.EqualTo(4));
        }

        [Test]
        public void InterpolatedStringHandler_Disabled_SkipsEveryAppend()
        {
            Appends = 0;
            Assert.That(Format(false, $"a={1} b={2}"), Is.Null);
            Assert.That(Appends, Is.EqualTo(0));
        }

        [Test]
        public void ModuleInitializer_HasRun_BeforeTheAssemblyIsUsed() => Assert.That(ModuleFlag.Ran, Is.True);

        [Test]
        public void MemberNotNull_CompilesWithoutNullWarning_AndRuns()
        {
            Assert.That(new LateInit().Length(), Is.EqualTo(3));
            Assert.That(new LateInit().LengthIfAny(), Is.EqualTo(-1));
        }

        [Test]
        public void StringSyntax_IsReadableOnTheParameter()
        {
            var p = typeof(PolyfillAddedTypesTests).GetMethod(nameof(Pattern), BindingFlags.NonPublic | BindingFlags.Static).GetParameters()[0];
            Assert.That(p.GetCustomAttribute<StringSyntaxAttribute>().Syntax, Is.EqualTo("Regex"));
        }

        [Test]
        public void DynamicallyAccessedMembers_IsReadableOnTheParameter()
        {
            var p = typeof(PolyfillAddedTypesTests).GetMethod(nameof(Reflect), BindingFlags.NonPublic | BindingFlags.Static).GetParameters()[0];
            Assert.That(p.GetCustomAttribute<DynamicallyAccessedMembersAttribute>().MemberTypes, Is.EqualTo(DynamicallyAccessedMemberTypes.PublicProperties));
        }

        [Test]
        public void StringBuilder_AppendInterpolated_WritesIntoTheBuilder()
        {
            var sb = new StringBuilder("[");
            sb.AppendInterpolated($"x={1}|{2.5f:F1}|{"a",3}|");
            sb.AppendInterpolated(CultureInfo.InvariantCulture, $"{1.5f}]");
            Assert.That(sb.ToString(), Is.EqualTo("[x=1|2.5|  a|1.5]"));
        }

#if UNITY_6000_5_OR_NEWER
        [CollectionBuilder(typeof(BagBuilder), nameof(BagBuilder.Create))]
        sealed class Bag : IEnumerable<int>
        {
            public int[] Items;
            public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
        }
        static class BagBuilder { public static Bag Create(ReadOnlySpan<int> items) => new Bag { Items = items.ToArray() }; }

        [Experimental("POLY001")] static int Unstable() => 7;

        [Test]
        public void CollectionBuilder_BuildsFromACollectionExpression()
        {
            Bag bag = [1, 2, 3];
            Assert.That(bag.Items, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Experimental_CallCompilesWhenTheDiagnosticIsSuppressed()
        {
#pragma warning disable POLY001
            Assert.That(Unstable(), Is.EqualTo(7));
#pragma warning restore POLY001
        }
#endif
    }
}
#endif
