#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for [ModuleInitializer] in Unity (netstandard2.0 targets)
    /// The built-in definition may be internal; this public version allows Source Generator output to use it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute { }
}
#endif
