#if CSHARP_PREVIEW
using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for UTF-8 string literal (u8).
    /// </summary>
    public class PolyfillUtf8LiteralTests
    {
        [Test]
        public void Utf8Literal_Korean_ByteLengthIsThreePerChar()
        {
            ReadOnlySpan<byte> hello = "안녕하세요"u8;
            Assert.That(hello.Length, Is.EqualTo(5 * 3)); // UTF-8: 1 Korean character = 3 bytes
        }

        [Test]
        public void Utf8Literal_Ascii_LengthMatchesStringLength()
        {
            string str = "Hello";
            ReadOnlySpan<byte> u8str = "Hello"u8;
            Assert.That(str.Length, Is.EqualTo(u8str.Length));
        }

        [Test]
        public void Utf8Literal_ToArray_LengthMatchesString()
        {
            byte[] bytes = "Hello, World!"u8.ToArray();
            Assert.That(bytes.Length, Is.EqualTo("Hello, World!".Length));
        }

        [Test]
        public void Utf8Literal_Decode_EqualsOriginalString()
        {
            string decoded = Encoding.UTF8.GetString("안녕"u8);
            Assert.That(decoded, Is.EqualTo("안녕"));
        }

        [Test]
        public void Utf8Literal_EqualsGetBytes_SequenceEqual()
        {
            byte[] oldWay = Encoding.UTF8.GetBytes("Hello");
            ReadOnlySpan<byte> newWay = "Hello"u8;
            Assert.That(oldWay.AsSpan().SequenceEqual(newWay), Is.True);
        }

        [Test]
        public void Utf8Literal_StartsWith_ReturnsTrue()
        {
            ReadOnlySpan<byte> request = "GET /index.html HTTP/1.1"u8;
            Assert.That(request.StartsWith("GET "u8), Is.True);
        }

        [Test]
        public void Utf8Literal_WriteToMemoryStream_LengthMatches()
        {
            using var ms = new MemoryStream();
            ms.Write("Hello from u8!"u8);
            Assert.That(ms.Length, Is.EqualTo("Hello from u8!".Length));
        }
    }
}
#endif
