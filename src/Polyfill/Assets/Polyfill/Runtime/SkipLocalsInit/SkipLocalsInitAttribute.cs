namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for C# 9 / .NET 5 SkipLocalsInit. Applied to a method, type, or module so that the compiler
    /// does not emit the .locals init flag; locals are not zero-initialized (can improve performance in hot paths).
    /// </summary>
    /// <remarks>
    /// Use with care: locals may contain garbage. Prefer on performance-critical code where every local is
    /// definitely assigned before use.
    /// Example: <c>[SkipLocalsInit] static void HotPath() { ... }</c>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method
        | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = false)]
    public sealed class SkipLocalsInitAttribute : Attribute
    {
        /// <summary>Initializes the attribute.</summary>
        public SkipLocalsInitAttribute()
        {
        }
    }
}
