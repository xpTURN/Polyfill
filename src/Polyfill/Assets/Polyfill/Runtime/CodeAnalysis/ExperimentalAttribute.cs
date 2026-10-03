#if !NET8_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for C# 12 [Experimental]. The compiler reports the diagnostic from Roslyn 4.8 (Unity 6000.5 and later).
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum
        | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event
        | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId)
        {
            DiagnosticId = diagnosticId;
        }

        public string DiagnosticId { get; }
        public string UrlFormat { get; set; }
        public string Message { get; set; }
    }
}
#endif
