#if CSHARP_PREVIEW
using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for Polyfill required + SetsRequiredMembers.
    /// </summary>
    public class PolyfillRequiredTests
    {
        private class Config
        {
            public required string Name { get; set; }
            public required int Port { get; init; }

            [SetsRequiredMembers]
            public Config()
            {
                Name = "Jone";
                Port = 1000;
            }
        }

        [Test]
        public void Required_SetsRequiredMembersConstructor_SetsNameAndPort()
        {
            var c = new Config();
            Assert.That(c.Name, Is.EqualTo("Jone"));
            Assert.That(c.Port, Is.EqualTo(1000));
        }

        [Test]
        public void Required_ObjectInitializer_CanOverride()
        {
            var c = new Config { Name = "Custom", Port = 2000 };
            Assert.That(c.Name, Is.EqualTo("Custom"));
            Assert.That(c.Port, Is.EqualTo(2000));
        }
    }
}
#endif
