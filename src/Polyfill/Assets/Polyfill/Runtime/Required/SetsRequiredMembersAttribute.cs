namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for using C# 11 'required' keyword in Unity
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class SetsRequiredMembersAttribute : Attribute { }
}
