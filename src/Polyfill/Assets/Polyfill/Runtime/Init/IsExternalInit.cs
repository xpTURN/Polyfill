#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for using C# 9.0 'init' accessor in Unity
    /// </summary>
    public static class IsExternalInit { }
}
#endif
