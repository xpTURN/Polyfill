namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Attribute for parameters that receive an interpolated string handler.
    /// When called as <c>Log($"value={x}")</c>, the compiler uses this parameter name to decide
    /// which arguments to pass to the handler constructor.
    /// </summary>
    /// <remarks>
    /// Usage (API provider perspective):
    /// <list type="bullet">
    /// <item><c>[InterpolatedStringHandlerArgument]</c> — No arguments. Handler constructor takes only (literalLength, formattedCount). Suitable for static log.</item>
    /// <item><c>[InterpolatedStringHandlerArgument("sb")]</c> — Passes this method's "sb" parameter as the third argument to the handler constructor. For StringBuilder etc.</item>
    /// <item><c>[InterpolatedStringHandlerArgument("a", "b")]</c> — Multiple arguments. Handler constructor signature must match in count and type.</item>
    /// </list>
    /// Callers pass only the interpolated string, not the handler: <c>xLogger.Log($"time={t}");</c>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
    {
        /// <summary>
        /// Names of method parameters to pass to the handler constructor; passed after literalLength and formattedCount.
        /// Empty array means no extra arguments; only the (int, int) constructor is used.
        /// </summary>
        public string[] Arguments { get; }

        /// <summary>Specifies a single parameter name.</summary>
        public InterpolatedStringHandlerArgumentAttribute(string argument)
        {
            Arguments = new[] { argument };
        }

        /// <summary>Specifies multiple parameter names. Order must match the handler constructor arguments.</summary>
        public InterpolatedStringHandlerArgumentAttribute(params string[] arguments)
        {
            Arguments = arguments;
        }
    }
}