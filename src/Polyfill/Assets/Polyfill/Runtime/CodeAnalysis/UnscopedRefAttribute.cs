#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for C# 11 [UnscopedRef]. The compiler honors it from Roslyn 4.4 (Unity 6000.5 and later).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class UnscopedRefAttribute : Attribute
    {
        public UnscopedRefAttribute() { }
    }
}
#endif
