namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for using C# 11 'required' keyword in Unity
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class RequiredMemberAttribute : Attribute { }
}
