#if CSHARP_PREVIEW
using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for explicitly typed lambdas.
    /// </summary>
    public class PolyfillExplicitLambdaTests
    {
        [Test]
        public void ExplicitLambda_SingleParameter_InvokesCorrectly()
        {
            Func<int, int> addOne = (int x) => x + 1;
            Assert.That(addOne(5), Is.EqualTo(6));
        }

        [Test]
        public void ExplicitLambda_TwoParameters_InvokesCorrectly()
        {
            Func<int, int, int> add = (int x, int y) => x + y;
            Assert.That(add(2, 3), Is.EqualTo(5));
        }

        [Test]
        public void ExplicitLambda_Action_SideEffect()
        {
            int captured = 0;
            Action<int> set = (int value) => captured = value;
            set(42);
            Assert.That(captured, Is.EqualTo(42));
        }

        [Test]
        public void ExplicitLambda_StringParameter_ReturnsLength()
        {
            Func<string, int> length = (string s) => s?.Length ?? 0;
            Assert.That(length("hello"), Is.EqualTo(5));
            Assert.That(length(null), Is.EqualTo(0));
        }

        [Test]
        public void ExplicitLambda_NullableInt_HandlesNull()
        {
            Func<int?, int> coalesce = (int? x) => x ?? 0;
            Assert.That(coalesce(7), Is.EqualTo(7));
            Assert.That(coalesce(null), Is.EqualTo(0));
        }

        [Test]
        public void ExplicitLambda_BlockBody_ReturnsValue()
        {
            Func<int, int> doubleIt = (int x) =>
            {
                var result = x * 2;
                return result;
            };
            Assert.That(doubleIt(11), Is.EqualTo(22));
        }
    }
}
#endif
