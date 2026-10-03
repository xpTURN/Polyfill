#if CSHARP_PREVIEW
using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for Polyfill init-only property.
    /// </summary>
    public class PolyfillInitTests
    {
        private class Data
        {
            public string Id { get; init; }
            public int Value { get; init; }

            public Data()
            {
                Id = "a";
                Value = 1;
            }
        }

        [Test]
        public void InitOnly_ObjectInitializer_SetsProperties()
        {
            var d = new Data { Id = "a", Value = 1 };
            Assert.That(d.Id, Is.EqualTo("a"));
            Assert.That(d.Value, Is.EqualTo(1));
        }

        [Test]
        public void InitOnly_DefaultConstructor_ReturnsDefaultValues()
        {
            var d = new Data();
            Assert.That(d.Id, Is.EqualTo("a"));
            Assert.That(d.Value, Is.EqualTo(1));
        }
    }
}
#endif
