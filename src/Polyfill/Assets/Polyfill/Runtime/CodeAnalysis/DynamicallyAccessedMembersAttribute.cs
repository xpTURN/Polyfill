namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for .NET 5+ DynamicallyAccessedMembersAttribute. Indicates that certain members
    /// on a specified Type are accessed dynamically (e.g. through System.Reflection).
    /// Used for trimmer/linker analysis; in Unity this is a no-op but allows the same attribute syntax.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field
        | AttributeTargets.GenericParameter | AttributeTargets.Interface | AttributeTargets.Method
        | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue,
        Inherited = false)]
    public sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        /// <summary>Gets the member types that are dynamically accessed.</summary>
        public DynamicallyAccessedMemberTypes MemberTypes { get; }

        /// <summary>Initializes the attribute with the specified member types.</summary>
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        {
            MemberTypes = memberTypes;
        }
    }
}
