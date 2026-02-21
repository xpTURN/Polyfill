#if CSHARP_PREVIEW
using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for generic math (C# 11 static abstract members in interfaces).
    /// Verifies static abstract behavior with a custom interface, without depending on BCL INumber etc.
    /// Static abstract members in interfaces require .NET 7+ runtime support.
    /// </summary>
    public class PolyfillGenericMathTests
    {
#if NET7_0_OR_GREATER
        /// <summary>
        /// Interface requiring addition and Zero as static abstract members.
        /// </summary>
        private interface IAddable<TSelf> where TSelf : IAddable<TSelf>
        {
            static abstract TSelf Zero { get; }
            static abstract TSelf operator +(TSelf left, TSelf right);
        }

        private struct IntLike : IAddable<IntLike>
        {
            public int Value;

            public static IntLike Zero => new IntLike { Value = 0 };

            public static IntLike operator +(IntLike left, IntLike right) =>
                new IntLike { Value = left.Value + right.Value };
        }

        private static T Add<T>(T a, T b) where T : IAddable<T> => a + b;

        private static T Zero<T>() where T : IAddable<T> => T.Zero;

        [Test]
        public void StaticAbstract_GenericAdd_InvokesOperator()
        {
            var a = new IntLike { Value = 2 };
            var b = new IntLike { Value = 3 };
            var sum = Add(a, b);
            Assert.That(sum.Value, Is.EqualTo(5));
        }

        [Test]
        public void StaticAbstract_GenericZero_ReturnsStaticProperty()
        {
            var z = Zero<IntLike>();
            Assert.That(z.Value, Is.EqualTo(0));
        }

        [Test]
        public void StaticAbstract_ZeroPlusValue_EqualsValue()
        {
            var z = Zero<IntLike>();
            var v = new IntLike { Value = 7 };
            var sum = Add(z, v);
            Assert.That(sum.Value, Is.EqualTo(7));
        }

        [Test]
        public void StaticAbstract_InterfaceConstraint_ResolvesStaticAbstract()
        {
            T SumThree<T>(T a, T b, T c) where T : IAddable<T> => a + b + c;

            var a = new IntLike { Value = 1 };
            var b = new IntLike { Value = 2 };
            var c = new IntLike { Value = 3 };
            var total = SumThree(a, b, c);
            Assert.That(total.Value, Is.EqualTo(6));
        }
#else
        [Test]
        public void StaticAbstract_RequiresNet7Runtime_Inconclusive()
        {
            Assert.Inconclusive("Static abstract members in interfaces are supported only on .NET 7+ runtime. This test is skipped on the current target runtime.");
        }
#endif
    }
}
#endif
