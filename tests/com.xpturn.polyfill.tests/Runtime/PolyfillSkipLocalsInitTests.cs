#if CSHARP_PREVIEW
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR && POLYFILL_MONO_CECIL_INTEGRATION
using Mono.Cecil;
#endif
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for SkipLocalsInitAttribute polyfill.
    /// </summary>
    [SkipLocalsInit]
    public class PolyfillSkipLocalsInitTests
    {
        [SkipLocalsInit]
        private static int MethodWithAttribute(int x, int y)
        {
            return x + y;
        }

        [SkipLocalsInit]
        private static unsafe void MethodWithStackAlloc()
        {
            Span<byte> buffer = stackalloc byte[8];
            buffer[0] = 1;
            buffer[1] = 2;
            Assert.That(buffer[0] + buffer[1], Is.EqualTo(3));
        }

        [Test]
        public void SkipLocalsInit_OnMethod_IsDiscoverableViaReflection()
        {
            var method = typeof(PolyfillSkipLocalsInitTests).GetMethod(nameof(MethodWithAttribute),
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var attr = method.GetCustomAttribute<SkipLocalsInitAttribute>();
            Assert.That(attr, Is.Not.Null);
        }

        [Test]
        public void SkipLocalsInit_OnClass_IsDiscoverableViaReflection()
        {
            var attr = typeof(PolyfillSkipLocalsInitTests).GetCustomAttribute<SkipLocalsInitAttribute>();
            Assert.That(attr, Is.Not.Null);
        }

        [Test]
        public void SkipLocalsInit_MarkedMethod_InvokesWithoutError()
        {
            Assert.That(MethodWithAttribute(2, 3), Is.EqualTo(5));
        }

        [Test]
        public void SkipLocalsInit_MethodWithStackAlloc_ExecutesCorrectly()
        {
            MethodWithStackAlloc();
        }

        [Test]
        public void SkipLocalsInit_AttributeType_IsInCompilerServicesNamespace()
        {
            Assert.That(typeof(SkipLocalsInitAttribute).Namespace, Is.EqualTo("System.Runtime.CompilerServices"));
        }

#if UNITY_EDITOR && POLYFILL_MONO_CECIL_INTEGRATION
        /// <summary>
        /// Reads current assembly IL via Mono.Cecil and verifies that methods with [SkipLocalsInit] have no .locals init.
        /// </summary>
        [Test]
        public void SkipLocalsInit_MarkedMethods_HaveNoLocalsInitInIL()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                Assert.Inconclusive("Cannot get current assembly path (running in Editor/build environment).");

            using var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadSymbols = false });
            TypeDefinition type = asm.MainModule.GetType(typeof(PolyfillSkipLocalsInitTests).FullName);
            Assert.That(type, Is.Not.Null, "Test type must be findable via Cecil.");

            string skipLocalsInitName = typeof(SkipLocalsInitAttribute).FullName;

            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                bool hasAttr = method.CustomAttributes.Any(a => a.AttributeType.FullName == skipLocalsInitName);
                if (!hasAttr)
                    continue;

                // Methods with [SkipLocalsInit] must have InitLocals == false (no .locals init)
                Assert.That(method.Body.InitLocals, Is.False,
                    $"Method '{method.Name}' has [SkipLocalsInit], so IL must have no .locals init (InitLocals == false).");
            }
        }
#endif
    }
}
#endif
