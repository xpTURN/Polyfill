#if CSHARP_PREVIEW
using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for C# 11 pattern matching — list patterns with Span/ReadOnlySpan and or patterns.
    /// </summary>
    public class PolyfillPatternMatchSpanOrTests
    {
        [Test]
        public void Span_ListPattern_ExactMatch()
        {
            ReadOnlySpan<int> span = stackalloc int[] { 1, 2, 3 };
            Assert.That(span is [1, 2, 3], Is.True);
            Assert.That(span is [1, 2, 4], Is.False);
        }

        [Test]
        public void Span_ListPattern_Var_CapturesElements()
        {
            ReadOnlySpan<int> span = stackalloc int[] { 10, 20 };
            if (span is [var a, var b])
            {
                Assert.That(a, Is.EqualTo(10));
                Assert.That(b, Is.EqualTo(20));
            }
            Assert.That(span is [var x, var y], Is.True);
        }

        [Test]
        public void Span_ListPattern_Slice_FirstAndLast()
        {
            ReadOnlySpan<int> span = stackalloc int[] { 5, 10, 15, 20 };
            Assert.That(span is [var first, .., var last], Is.True);
            if (span is [var f, .., var l])
            {
                Assert.That(f, Is.EqualTo(5));
                Assert.That(l, Is.EqualTo(20));
            }
        }

        [Test]
        public void Span_ListPattern_Empty()
        {
            ReadOnlySpan<int> empty = ReadOnlySpan<int>.Empty;
            Assert.That(empty is [], Is.True);
            ReadOnlySpan<int> nonEmpty = stackalloc int[] { 1 };
            Assert.That(nonEmpty is [], Is.False);
        }

        [Test]
        public void OrPattern_WithListPattern_MatchesFirst()
        {
            int[] arr = { 1, 2 };
            Assert.That(arr is [1, 2] or [2, 3], Is.True);
            Assert.That(arr is [1, 2] or [3, 4], Is.True); // first branch matches
        }

        [Test]
        public void OrPattern_WithListPattern_MatchesSecond()
        {
            int[] arr = { 2, 3 };
            Assert.That(arr is [1, 2] or [2, 3], Is.True);
            Assert.That(arr is [2, 3] or [1, 2], Is.True);
        }

        [Test]
        public void OrPattern_WithListPattern_NoMatch()
        {
            int[] arr = { 5, 6 };
            Assert.That(arr is [1, 2] or [2, 3] or [3, 4], Is.False);
        }

        [Test]
        public void OrPattern_WithSpan_ListPattern()
        {
            ReadOnlySpan<int> span = stackalloc int[] { 1, 2 };
            Assert.That(span is [1, 2] or [2, 3], Is.True);
            ReadOnlySpan<int> span23 = stackalloc int[] { 2, 3 };
            Assert.That(span23 is [1, 2] or [2, 3], Is.True);
        }

        [Test]
        public void OrPattern_SwitchExpression_WithSpan()
        {
            static string Label(ReadOnlySpan<int> s) => s switch
            {
                [] => "empty",
                [var x] => "one",
                [1, 2] or [2, 1] => "pair-12",
                [var a, var b] => "pair",
                _ => "other"
            };
            Assert.That(Label(stackalloc int[] { }), Is.EqualTo("empty"));
            Assert.That(Label(stackalloc int[] { 7 }), Is.EqualTo("one"));
            Assert.That(Label(stackalloc int[] { 1, 2 }), Is.EqualTo("pair-12"));
            Assert.That(Label(stackalloc int[] { 2, 1 }), Is.EqualTo("pair-12"));
            Assert.That(Label(stackalloc int[] { 3, 4 }), Is.EqualTo("pair"));
        }
    }
}
#endif
