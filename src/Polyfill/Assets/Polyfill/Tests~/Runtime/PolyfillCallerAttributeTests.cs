#if CSHARP_PREVIEW
using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for CallerMemberName, CallerFilePath, CallerLineNumber, CallerArgumentExpression polyfill.
    /// </summary>
    public class PolyfillCallerAttributeTests
    {
        private static (string memberName, string filePath, int lineNumber) GetCallerInfo(
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            return (memberName, filePath, lineNumber);
        }

        /// <summary>
        /// Throws ArgumentException when condition is false; message includes the expression.
        /// Example: Validate(value >= 0); on failure yields "Condition 'value >= 0' failed."
        /// </summary>
        private static void Validate(bool condition, [CallerArgumentExpression(nameof(condition))] string expression = null)
        {
            if (!condition)
                throw new ArgumentException($"Condition '{expression}' failed.");
        }

        /// <summary>
        /// Throws InvalidOperationException when condition is false; message includes the expression.
        /// Example: AssertCondition(obj != null); on failure yields "Assertion 'obj != null' failed."
        /// </summary>
        private static void AssertCondition(bool condition, [CallerArgumentExpression(nameof(condition))] string expression = null)
        {
            if (!condition)
                throw new InvalidOperationException($"Assertion '{expression}' failed.");
        }

        /// <summary>
        /// Value must be non-null. Throws ArgumentNullException with parameter name and expression when null.
        /// Example: RequireNotNull(obj); on failure includes the "obj" expression in the message.
        /// </summary>
        private static T RequireNotNull<T>(T value, [CallerArgumentExpression(nameof(value))] string expression = null) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(expression, $"Required non-null: '{expression}'.");
            return value;
        }

        [Test]
        public void CallerMemberName_WhenCalledFromTest_EqualsTestMethodName()
        {
            var (memberName, _, _) = GetCallerInfo();
            Assert.That(memberName, Is.EqualTo(nameof(CallerMemberName_WhenCalledFromTest_EqualsTestMethodName)));
        }

        [Test]
        public void CallerFilePath_WhenCalled_EndsWithCurrentFileName()
        {
            var (_, filePath, _) = GetCallerInfo();
            Assert.That(filePath, Does.EndWith("PolyfillCallerAttributeTests.cs"));
        }

        [Test]
        public void CallerLineNumber_WhenCalled_IsPositive()
        {
            var (_, _, lineNumber) = GetCallerInfo();
            Assert.That(lineNumber, Is.GreaterThan(0));
        }

        // --- CallerArgumentExpression usage: Validate / AssertCondition / RequireNotNull ---

        [Test]
        public void Validate_WhenConditionTrue_DoesNotThrow()
        {
            int value = 1;
            Validate(value > 0);
        }

        [Test]
        public void Validate_WhenConditionFalse_ThrowsWithExpressionInMessage()
        {
            int a = 1, b = 2;
            var ex = Assert.Throws<ArgumentException>(() => Validate(a + b == 0));
            Assert.That(ex.Message, Does.Contain("a + b == 0"));
        }

        [Test]
        public void AssertCondition_WhenConditionFalse_ThrowsWithExpressionInMessage()
        {
            string obj = null;
            var ex = Assert.Throws<InvalidOperationException>(() => AssertCondition(obj != null));
            Assert.That(ex.Message, Does.Contain("obj != null"));
        }

        [Test]
        public void RequireNotNull_WhenNotNull_ReturnsValue()
        {
            var s = "hello";
            var result = RequireNotNull(s);
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void RequireNotNull_WhenNull_ThrowsWithExpressionInParamName()
        {
            string nil = null;
            var ex = Assert.Throws<ArgumentNullException>(() => RequireNotNull(nil));
            Assert.That(ex.ParamName, Is.EqualTo("nil"));
            Assert.That(ex.Message, Does.Contain("nil"));
        }
    }
}
#endif
