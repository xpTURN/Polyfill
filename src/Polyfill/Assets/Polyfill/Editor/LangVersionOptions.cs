using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace xpTURN.Polyfill.Editor
{
    /// <summary>
    /// The C# language versions this editor's compiler accepts, and the pure transformations that write the chosen one
    /// into compiler arguments, define symbols and .csproj content.
    /// </summary>
    internal static class LangVersionOptions
    {
        internal const string Default = "preview";
        internal const string Preview = "preview";

#if UNITY_6000_5_OR_NEWER
        // Roslyn 4.10
        internal static readonly string[] Values = { "10.0", "11.0", "12.0", Preview };
#else
        // Roslyn 4.3 rejects -langversion:11.0 and above (CS1617)
        internal static readonly string[] Values = { "10.0", Preview };
#endif

        private static readonly Regex s_LangVersionArgument = new Regex(@"(?i)-langversion:\S*");
        private static readonly Regex s_CsprojLangVersion = new Regex(@"<LangVersion>[^<]*</LangVersion>");

        /// <summary>A missing or empty stored value means the default.</summary>
        internal static string Normalize(string stored)
        {
            return string.IsNullOrWhiteSpace(stored) ? Default : stored.Trim();
        }

        internal static bool IsSupported(string value)
        {
            return Array.IndexOf(Values, value) >= 0;
        }

        /// <summary>
        /// Removes every -langversion:* and puts -langversion:&lt;value&gt; where the first one was, or at the end.
        /// Other arguments, also those sharing an entry with a -langversion, keep their place.
        /// </summary>
        internal static string[] MergeArguments(string[] existing, string value)
        {
            var desired = PlayerSettingsAdditionalCompilerArguments.LangVersionPrefix + value;
            var result = new List<string>();
            var placed = false;
            if (existing != null)
            {
                foreach (var arg in existing)
                {
                    if (string.IsNullOrWhiteSpace(arg)) continue;
                    if (!s_LangVersionArgument.IsMatch(arg))
                    {
                        result.Add(arg);
                        continue;
                    }

                    var content = s_LangVersionArgument.Replace(arg, _ =>
                    {
                        if (placed) return string.Empty;
                        placed = true;
                        return desired;
                    }).Trim();
                    if (content.Length > 0)
                        result.Add(content);
                }
            }

            if (!placed)
                result.Add(desired);
            return result.ToArray();
        }

        /// <summary>
        /// CSHARP_PREVIEW is present only for preview. An existing one keeps its place; other symbols keep their order.
        /// </summary>
        internal static string[] MergeSymbols(string[] current, string value)
        {
            var symbol = PlayerSettingsAdditionalCompilerArguments.ScriptingDefineSymbolForLatestCSharp;
            var keep = string.Equals(value, Preview, StringComparison.Ordinal);
            var result = new List<string>();
            var present = false;
            if (current != null)
            {
                foreach (var s in current)
                {
                    var trimmed = s?.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    if (string.Equals(trimmed, symbol, StringComparison.Ordinal))
                    {
                        if (!keep || present) continue;
                        present = true;
                    }
                    result.Add(trimmed);
                }
            }

            if (keep && !present)
                result.Add(symbol);
            return result.ToArray();
        }

        internal static string ReplaceCsprojLangVersion(string content, string value)
        {
            return s_CsprojLangVersion.Replace(content, "<LangVersion>" + value + "</LangVersion>");
        }

        internal static bool SameEntries(string[] a, string[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++)
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            return true;
        }
    }
}
