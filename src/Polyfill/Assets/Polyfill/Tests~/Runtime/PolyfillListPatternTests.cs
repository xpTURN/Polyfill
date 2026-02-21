#if CSHARP_PREVIEW
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for C# 11 list patterns.
    /// </summary>
    public class PolyfillListPatternTests
    {
        [Test]
        public void ListPattern_ExactMatch_Matches()
        {
            int[] arr = { 1, 2, 3 };
            Assert.That(arr is [1, 2, 3], Is.True);
            Assert.That(arr is [1, 2, 4], Is.False);
        }

        [Test]
        public void ListPattern_ExactMatch_LengthMismatch_Fails()
        {
            int[] arr = { 1, 2 };
            Assert.That(arr is [1, 2, 3], Is.False);
            Assert.That(arr is [1], Is.False);
        }

        [Test]
        public void ListPattern_Var_CapturesElements()
        {
            int[] arr = { 10, 20, 30 };
            Assert.That(arr is [var a, var b, var c], Is.True);
            if (arr is [var x, var y, var z])
            {
                Assert.That(x, Is.EqualTo(10));
                Assert.That(y, Is.EqualTo(20));
                Assert.That(z, Is.EqualTo(30));
            }
        }

        [Test]
        public void ListPattern_Slice_CapturesFirstAndLast()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            Assert.That(arr is [var first, .., var last], Is.True);
            if (arr is [var f, .., var l])
            {
                Assert.That(f, Is.EqualTo(1));
                Assert.That(l, Is.EqualTo(5));
            }
        }

        [Test]
        public void ListPattern_Slice_MatchesMiddle()
        {
            int[] arr = { 1, 2, 3, 4 };
            if (arr is [.., 3, 4])
                Assert.Pass();
            Assert.That(arr is [.., 3, 4], Is.True);
            Assert.That(arr is [1, 2, ..], Is.True);
        }

        [Test]
        public void ListPattern_Empty_MatchesEmpty()
        {
            int[] empty = Array.Empty<int>();
            Assert.That(empty is [], Is.True);
            Assert.That(new[] { 1 } is [], Is.False);
        }

        [Test]
        public void ListPattern_SingleElement_Matches()
        {
            int[] one = { 42 };
            Assert.That(one is [42], Is.True);
            Assert.That(one is [var x] && x == 42, Is.True);
        }

        [Test]
        public void ListPattern_Relational_InList()
        {
            int[] arr = { 5, 10, 15 };
            Assert.That(arr is [> 0, ..], Is.True);
            Assert.That(arr is [< 0, ..], Is.False);
            if (arr is [>= 5, var second, <= 20])
            {
                Assert.That(second, Is.EqualTo(10));
            }
        }

        [Test]
        public void ListPattern_List_Matches()
        {
            var list = new List<int> { 1, 2, 3 };
            Assert.That(list is [1, 2, 3], Is.True);
            Assert.That(list is [var a, .., var b] && a == 1 && b == 3, Is.True);
        }

        [Test]
        public void ListPattern_SwitchExpression_ReturnsLabel()
        {
            string Label(int[] a) => a switch
            {
                [] => "empty",
                [var x] => "one",
                [var x, var y] => "two",
                [_, _, ..] => "many",
                _ => "other"
            };
            Assert.That(Label(Array.Empty<int>()), Is.EqualTo("empty"));
            Assert.That(Label(new[] { 1 }), Is.EqualTo("one"));
            Assert.That(Label(new[] { 1, 2 }), Is.EqualTo("two"));
            Assert.That(Label(new[] { 1, 2, 3 }), Is.EqualTo("many"));
        }
    }
}
#endif
