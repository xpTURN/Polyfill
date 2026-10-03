#if CSHARP_PREVIEW
using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for C# 11 raw string literal (""" """).
    /// </summary>
    public class PolyfillRawStringLiteralTests
    {
        [Test]
        public void RawString_SingleLine_EqualsExpected()
        {
            string single = """Hello, World!""";

            Assert.That(single, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void RawString_MultilineJson_ContainsEscapedQuotes()
        {
            string json = """
                {
                    "name": "Polyfill",
                    "version": 1
                }
                """;

            string trimmed = json.Trim();
            Assert.That(trimmed, Does.Contain("\"name\""));
            string expected = "{\n    \"name\": \"Polyfill\",\n    \"version\": 1\n}";
            Assert.That(json.Replace("\r", ""), Is.EqualTo(expected), "Raw string result must match traditional string content");
        }

        [Test]
        public void RawString_DoubleQuoteInside_EqualsExpected()
        {
            string quoted = """She said "Hello".""";

            Assert.That(quoted, Is.EqualTo("She said \"Hello\"."));
            string expected = "She said \"Hello\".";
            Assert.That(quoted, Is.EqualTo(expected), "Raw string result must match traditional string content");

        }

        [Test]
        public void RawString_Interpolated_ContainsValues()
        {
            string key = "lang";
            string value = "C#";
            int version = 11;
            string interpolated = $$"""
                {
                    "{{key}}": "{{value}}",
                    "version": {{version}}
                }
                """;
            string expected = $"{{\n    \"{key}\": \"{value}\",\n    \"version\": {version}\n}}";
            Assert.That(interpolated.Replace("\r", ""), Is.EqualTo(expected), "Raw string result must match traditional string content");
        }

        [Test]
        public void RawString_Multiline_PreservesLinesAndBlankLine()
        {
            string multiline = """
                Line 1
                Line 2

                Line 4
                """;

            var lines = multiline.Split('\n');
            Assert.That(lines.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(multiline, Does.Contain("Line 1"));
            Assert.That(multiline, Does.Contain("Line 4"));
            string expected = "Line 1\nLine 2\n\nLine 4";
            Assert.That(multiline.Replace("\r",""), Is.EqualTo(expected), "Raw string result must match traditional string content");
        }
    }
}
#endif
