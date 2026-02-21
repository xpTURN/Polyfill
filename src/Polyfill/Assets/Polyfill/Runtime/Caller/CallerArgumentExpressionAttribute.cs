namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for C# 10 / .NET 5 CallerArgumentExpression. Applied to an optional parameter;
    /// the compiler passes the source text of the argument expression for the specified parameter.
    /// </summary>
    /// <remarks>
    /// Example: <c>void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)</c>
    /// When called as <c>Assert(x &gt; 0)</c>, <c>expression</c> receives <c>"x &gt; 0"</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the name of the parameter whose argument expression is to be captured.</summary>
        /// <param name="parameterName">The name of the parameter (e.g. from nameof(parameter)).</param>
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        /// <summary>Gets the name of the parameter whose argument expression is captured by the compiler.</summary>
        public string ParameterName { get; }
    }
}
