#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for [MemberNotNull] (nullable flow analysis, C# 9).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class MemberNotNullAttribute : Attribute
    {
        public MemberNotNullAttribute(string member)
        {
            Members = new[] { member };
        }

        public MemberNotNullAttribute(params string[] members)
        {
            Members = members;
        }

        public string[] Members { get; }
    }
}
#endif
