using System;

using UnityEditor;

namespace xpTURN.Polyfill.Editor
{
    /// <summary>
    /// Sets LangVersion in generated .csproj files to the C# language version chosen in Project Settings &gt; xpTURN Polyfill.
    /// IDEs (Cursor, OmniSharp, VS, etc.) read this .csproj for C# 11 syntax support.
    /// Unity editor compilation does not use .csproj, so -langversion for editor builds must be set separately
    /// via csc.rsp or Player Settings -> Additional Compiler Arguments (this package's menu).
    /// (Unity 6+ may support C# 11 by default, so csc.rsp may not be required for editor builds.)
    /// </summary>
    public class CsprojLangVersionPostprocessor : AssetPostprocessor
    {
        public static string OnGeneratedCSProject(string path, string content)
        {
            return Process(path, content, PlayerSettingsAdditionalCompilerArguments.GetApplyArgumentsForAllTargetsEnabled(), PlayerSettingsAdditionalCompilerArguments.GetLangVersion());
        }

        internal static string Process(string path, string content, bool enabled, string langVersion)
        {
            if (!enabled || !LangVersionOptions.IsSupported(langVersion))
                return content;

            if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return content;

            return LangVersionOptions.ReplaceCsprojLangVersion(content, langVersion);
        }
    }
}