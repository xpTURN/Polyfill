#if CSHARP_PREVIEW
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for UnscopedRefAttribute polyfill.
    /// [UnscopedRef] applies only to instance methods/properties of structs (or virtual interfaces).
    /// Not supported on Unity runtime; tests are skipped in that case.
    /// </summary>
    public class PolyfillUnscopedRefTests
    {
#if NET7_0_OR_GREATER // UnscopedRef-supported runtime (.NET 7+)
        private struct UnscopedRefHolder
        {
            public int count;

            [UnscopedRef]
            public ref int MethodWithUnscopedRef()
            {
                return ref count;
            }

            [UnscopedRef]
            public ref int MethodWithUnscopedRef(ref int value)
            {
                return ref value;
            }

            public void MethodWithUnscopedRefParameter([UnscopedRef] ref int value)
            {
                value++;
            }
        }

        [Test]
        public void UnscopedRef_AttributeType_IsInCodeAnalysisNamespace()
        {
            Assert.That(typeof(UnscopedRefAttribute).Namespace, Is.EqualTo("System.Diagnostics.CodeAnalysis"));
        }

        [Test]
        public void UnscopedRef_OnMethod_IsDiscoverableViaReflection()
        {
            var method = typeof(UnscopedRefHolder).GetMethod(nameof(UnscopedRefHolder.MethodWithUnscopedRef),
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            var attr = method.GetCustomAttribute<UnscopedRefAttribute>();
            Assert.That(attr, Is.Not.Null);
        }

        [Test]
        public void UnscopedRef_OnParameter_IsDiscoverableViaReflection()
        {
            var method = typeof(UnscopedRefHolder).GetMethod(nameof(UnscopedRefHolder.MethodWithUnscopedRefParameter),
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            var param = method.GetParameters()[0];
            var attr = param.GetCustomAttribute<UnscopedRefAttribute>();
            Assert.That(attr, Is.Not.Null);
        }

        [Test]
        public void UnscopedRef_MethodWithRef_InvokesCorrectly()
        {
            var holder = new UnscopedRefHolder();
            int x = 10;
            ref int r = ref holder.MethodWithUnscopedRef(ref x);
            r = 20;
            Assert.That(x, Is.EqualTo(20));
        }

        [Test]
        public void UnscopedRef_MethodReturningRefToField_UpdatesStructField()
        {
            var holder = new UnscopedRefHolder { count = 7 };
            ref int r = ref holder.MethodWithUnscopedRef();
            r = 42;
            Assert.That(holder.count, Is.EqualTo(42));
        }

        [Test]
        public void UnscopedRef_Parameter_IncrementsValue()
        {
            var holder = new UnscopedRefHolder();
            int value = 5;
            holder.MethodWithUnscopedRefParameter(ref value);
            Assert.That(value, Is.EqualTo(6));
        }
#else // UnscopedRef unsupported runtime (e.g. Unity)
        [Test]
        public void UnscopedRef_UnsupportedOnUnityRuntime_Inconclusive()
        {
            Assert.Inconclusive("UnscopedRefAttribute is not supported on Unity runtime. .NET 7+ runtime is required.");
        }
#endif
    }
}
#endif
