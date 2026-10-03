using System;
using NUnit.Framework;

namespace xpTURN.Polyfill.Tests
{
    /// <summary>
    /// Tests for C# 7/8 pattern matching switch.
    /// </summary>
    public class PolyfillSwitchTests
    {
        private static string TypeSwitchResult(object obj)
        {
            return obj switch
            {
                int n => $"int {n}",
                float n when n >= 0 => $"float {n} >= 0",
                float n when n < 0 => $"float {n} < 0",
                _ => "default"
            };
        }

        private static string MultiSwitchResult(int x, int y)
        {
            return (x >= 0, y >= 0) switch
            {
                (true, true) => "true, true",
                (true, false) => "true, false",
                (false, true) => "false, true",
                (false, false) => "false, false",
            };
        }

        [Test]
        public void TypeSwitch_Int_ReturnsIntMessage()
        {
            Assert.That(TypeSwitchResult(1), Is.EqualTo("int 1"));
        }

        [Test]
        public void TypeSwitch_FloatNonNegative_ReturnsFloatMessage()
        {
            Assert.That(TypeSwitchResult(10.0f), Is.EqualTo("float 10 >= 0"));
        }

        [Test]
        public void TypeSwitch_FloatNegative_ReturnsFloatMessage()
        {
            Assert.That(TypeSwitchResult(-5.0f), Is.EqualTo("float -5 < 0"));
        }

        [Test]
        public void MultiSwitch_BothNonNegative_ReturnsTrueTrue()
        {
            Assert.That(MultiSwitchResult(10, 10), Is.EqualTo("true, true"));
        }

        [Test]
        public void MultiSwitch_FirstNonNegativeSecondNegative_ReturnsTrueFalse()
        {
            Assert.That(MultiSwitchResult(10, -10), Is.EqualTo("true, false"));
        }

        [Test]
        public void MultiSwitch_BothNegative_ReturnsFalseFalse()
        {
            Assert.That(MultiSwitchResult(-10, -10), Is.EqualTo("false, false"));
        }
    }
}
