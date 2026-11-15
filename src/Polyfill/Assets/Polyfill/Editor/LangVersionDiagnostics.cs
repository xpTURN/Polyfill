using System;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;

namespace xpTURN.Polyfill.Editor
{
    /// <summary>
    /// When a compile in an open editor fails with CS1617 on /langversion (a value this editor's compiler does not accept,
    /// e.g. 12.0 applied from Unity 6000.5 or later), says which argument it is and where to change it, once per compile.
    /// A project opened with that value compiles nothing, so this code does not run then.
    /// </summary>
    [InitializeOnLoad]
    internal static class LangVersionDiagnostics
    {
        private static bool s_Reported;

        static LangVersionDiagnostics()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            CompilationPipeline.compilationStarted += _ => s_Reported = false;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (s_Reported) return;
            foreach (var m in messages)
            {
                if (m.type != CompilerMessageType.Error || !IsLangVersionOptionError(m.message)) continue;
                s_Reported = true;
                var target = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
                Debug.LogError(BuildMessage(target.TargetName, PlayerSettings.GetAdditionalCompilerArguments(target)));
                return;
            }
        }

        internal static bool IsLangVersionOptionError(string message)
        {
            return message != null
                && message.IndexOf("CS1617", StringComparison.Ordinal) >= 0
                && message.IndexOf("/langversion", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static string BuildMessage(string targetName, string[] arguments)
        {
            return $"[Polyfill] This editor's C# compiler does not accept the -langversion in Player Settings > Additional Compiler Arguments ({targetName}: {string.Join(" ", arguments ?? Array.Empty<string>())}). "
                + $"Choose one of {string.Join(", ", LangVersionOptions.Values)} in Project Settings > xpTURN Polyfill and press Apply.";
        }
    }
}
