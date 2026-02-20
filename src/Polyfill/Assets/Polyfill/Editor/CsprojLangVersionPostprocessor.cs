using System;
using System.Text.RegularExpressions;

using UnityEditor;

namespace xpTURN.Polyfill.Editor
{
    /// <summary>
    /// Sets LangVersion to 'preview' in generated .csproj files.
    /// IDEs (Cursor, OmniSharp, VS, etc.) read this .csproj for C# 11 syntax support.
    /// Unity editor compilation does not use .csproj, so -langversion for editor builds must be set separately
    /// via csc.rsp or Player Settings -> Additional Compiler Arguments (this package's menu).
    /// (Unity 6+ may support C# 11 by default, so csc.rsp may not be required for editor builds.)
    /// </summary>
    public class CsprojLangVersionPostprocessor : AssetPostprocessor
    {
        public static string OnGeneratedCSProject(string path, string content)
        {
            if (!PlayerSettingsAdditionalCompilerArguments.GetApplyArgumentsForAllTargetsEnabled())
                return content;

            if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return content;

            var pattern = @"<LangVersion>[^<]*</LangVersion>";
            var replace = @"<LangVersion>preview</LangVersion>";
            content = Regex.Replace(content, pattern, replace);
            return content;
        }
    }
}