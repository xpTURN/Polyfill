namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for .NET 5+ DynamicallyAccessedMemberTypes. Specifies the types of members
    /// that are dynamically accessed (e.g. via reflection). Used with <see cref="DynamicallyAccessedMembersAttribute"/>.
    /// </summary>
    [Flags]
    public enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        All = -1,

        PublicParameterlessConstructor = 1,
        PublicConstructors = 3,
        NonPublicConstructors = 4,

        PublicMethods = 8,
        NonPublicMethods = 16,

        PublicFields = 32,
        NonPublicFields = 64,

        PublicNestedTypes = 128,
        NonPublicNestedTypes = 256,

        PublicProperties = 512,
        NonPublicProperties = 1024,

        PublicEvents = 2048,
        NonPublicEvents = 4096,

        Interfaces = 8192,

        NonPublicConstructorsWithInherited = 16388,
        NonPublicMethodsWithInherited = 32784,
        AllMethods = 32792,

        NonPublicFieldsWithInherited = 65600,
        AllFields = 65632,

        NonPublicNestedTypesWithInherited = 131328,

        NonPublicPropertiesWithInherited = 263168,
        AllProperties = 263680,

        NonPublicEventsWithInherited = 528384,
        AllEvents = 530432,

        PublicConstructorsWithInherited = 1048579,
        AllConstructors = 1064967,

        PublicNestedTypesWithInherited = 2097280,
        AllNestedTypes = 2228608,
    }
}
