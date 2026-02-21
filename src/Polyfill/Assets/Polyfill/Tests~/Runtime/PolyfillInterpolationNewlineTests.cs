#if CSHARP_PREVIEW
using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for C# 11 newlines in string interpolation.
    /// Newlines can be placed inside interpolation expressions { }.
    /// Not supported on Unity runtime; tests are skipped in that case.
    /// </summary>
    public class PolyfillInterpolationNewlineTests
    {
#if NET7_0_OR_GREATER // Newlines in interpolation supported (C# 11 / .NET 7+)
        private static int GetValue() => 42;

        private static string GetText() => "hello";

        [Test]
        public void InterpolationNewline_ExpressionOnNewLine_Interpolates()
        {
            int x = 10;
            string s = $"Result: {
                x
            }";
            Assert.That(s, Is.EqualTo("Result: 10"));
        }

        [Test]
        public void InterpolationNewline_MethodCallOnNewLine_Interpolates()
        {
            string s = $"Value: {
                GetValue()
            }";
            Assert.That(s, Is.EqualTo("Value: 42"));
        }

        [Test]
        public void InterpolationNewline_MultiLineExpression_Interpolates()
        {
            int a = 2, b = 3;
            string s = $"Sum: {
                a + b
            }";
            Assert.That(s, Is.EqualTo("Sum: 5"));
        }

        [Test]
        public void InterpolationNewline_TernaryOnNewLines_Interpolates()
        {
            bool flag = true;
            string s = $"Flag: {
                flag
                    ? "yes"
                    : "no"
            }";
            Assert.That(s, Is.EqualTo("Flag: yes"));
        }

        [Test]
        public void InterpolationNewline_MultipleInterpolationsWithNewlines_Concatenates()
        {
            string s = $"{ GetText() }{
                " "
            }{
                GetValue()
            }";
            Assert.That(s, Is.EqualTo("hello 42"));
        }

        [Test]
        public void InterpolationNewline_FormatWithNewline_Interpolates()
        {
            double v = 3.14;
            string s = $"Pi: {
                v:F2
            }";
            Assert.That(s, Is.EqualTo("Pi: 3.14"));
        }
#else // Newlines in interpolation unsupported (e.g. Unity)
        [Test]
        public void InterpolationNewline_UnsupportedOnUnity_Inconclusive()
        {
            Assert.Inconclusive("Newlines in string interpolation are not supported on Unity runtime. C# 11 / .NET 7+ or later is required.");
        }
#endif
    }
}
#endif
