#if !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for C# 12 [CollectionBuilder]. Lets a custom collection be created from a collection expression (Unity 6000.5 and later).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
    public sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, string methodName)
        {
            BuilderType = builderType;
            MethodName = methodName;
        }

        public Type BuilderType { get; }
        public string MethodName { get; }
    }
}
#endif
