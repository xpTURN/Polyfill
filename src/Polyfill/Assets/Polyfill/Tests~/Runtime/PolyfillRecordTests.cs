#if CSHARP_PREVIEW
using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for C# 9 record keyword (record types).
    /// </summary>
    public class PolyfillRecordTests
    {
        private record Person(string Name, int Age);

        private record struct Point(int X, int Y);

        [Test]
        public void Record_Constructor_SetsProperties()
        {
            var p = new Person("Alice", 30);
            Assert.That(p.Name, Is.EqualTo("Alice"));
            Assert.That(p.Age, Is.EqualTo(30));
        }

        [Test]
        public void Record_ValueEquality_ComparesByMembers()
        {
            var a = new Person("Bob", 25);
            var b = new Person("Bob", 25);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Record_ValueEquality_DifferentValues_NotEqual()
        {
            var a = new Person("Bob", 25);
            var b = new Person("Bob", 26);
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Record_ToString_PrintsMembers()
        {
            var p = new Person("Charlie", 40);
            var s = p.ToString();
            Assert.That(s, Does.Contain("Charlie"));
            Assert.That(s, Does.Contain("40"));
        }

        [Test]
        public void Record_WithExpression_CreatesCopyWithNewValues()
        {
            var p = new Person("Diana", 35);
            var q = p with { Age = 36 };
            Assert.That(q.Name, Is.EqualTo("Diana"));
            Assert.That(q.Age, Is.EqualTo(36));
            Assert.That(p.Age, Is.EqualTo(35));
        }

        [Test]
        public void RecordStruct_Constructor_SetsProperties()
        {
            var pt = new Point(1, 2);
            Assert.That(pt.X, Is.EqualTo(1));
            Assert.That(pt.Y, Is.EqualTo(2));
        }

        [Test]
        public void RecordStruct_ValueEquality_ComparesByMembers()
        {
            var a = new Point(3, 4);
            var b = new Point(3, 4);
            Assert.That(a, Is.EqualTo(b));
        }
    }
}
#endif
