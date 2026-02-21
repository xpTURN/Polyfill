#if CSHARP_PREVIEW
using System;
using System.Reflection;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for C# 11 file-scoped types.
    /// Types declared with the file modifier are accessible only from the same source file.
    /// Tests are skipped on unsupported environments (e.g. Unity).
    /// </summary>
    public class PolyfillFileScopedTypesTests
    {
#if NET7_0_OR_GREATER // file-scoped types supported (C# 11 / .NET 7+)
        [Test]
        public void FileScopedClass_CanInstantiateInSameFile()
        {
            var instance = new FileScopedHelper();
            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public void FileScopedClass_Method_ReturnsValue()
        {
            var instance = new FileScopedHelper();
            Assert.That(instance.GetValue(), Is.EqualTo(42));
        }

        [Test]
        public void FileScopedStruct_CanUseInSameFile()
        {
            var v = new FileScopedData { Id = 1, Name = "a" };
            Assert.That(v.Id, Is.EqualTo(1));
            Assert.That(v.Name, Is.EqualTo("a"));
        }

        [Test]
        public void FileScopedStruct_DefaultConstructed_HasDefaultValues()
        {
            var v = new FileScopedData();
            Assert.That(v.Id, Is.EqualTo(0));
            Assert.That(v.Name, Is.Null);
        }

        [Test]
        public void FileScopedType_ExistsInAssembly()
        {
            var type = typeof(FileScopedHelper);
            Assert.That(type, Is.Not.Null);
            Assert.That(type.Name, Does.Contain("FileScopedHelper"));
        }
#else // file-scoped types unsupported (e.g. Unity)
        [Test]
        public void FileScopedTypes_Unsupported_Inconclusive()
        {
            Assert.Inconclusive("file-scoped types are not available on unsupported environments (e.g. Unity). C# 11 / .NET 7+ or later is required.");
        }
#endif
    }

#if NET7_0_OR_GREATER
    file class FileScopedHelper
    {
        public int GetValue() => 42;
    }

    file struct FileScopedData
    {
        public int Id;
        public string Name;
    }
#endif
}
#endif
