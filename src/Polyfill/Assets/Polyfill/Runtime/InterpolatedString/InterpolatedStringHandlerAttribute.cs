namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for C# 10 interpolated string handler. Compiler uses this to recognize custom interpolated string handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerAttribute : Attribute { }
}