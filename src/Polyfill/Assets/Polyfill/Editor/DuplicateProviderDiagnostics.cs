using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace xpTURN.Polyfill.Editor
{
    /// <summary>
    /// When a compile error names one of the types this package declares (CS0518 / CS0433), looks for another
    /// assembly that declares the same type publicly and says which one. Nothing runs on a clean compile.
    /// </summary>
    [InitializeOnLoad]
    internal static class DuplicateProviderDiagnostics
    {
        private const string OwnAssembly = "xpTURN.Polyfill.Runtime";

        private static readonly string[] s_TypeNames =
        {
            "System.Runtime.CompilerServices.IsExternalInit",
            "System.Runtime.CompilerServices.RequiredMemberAttribute",
            "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute",
            "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute",
            "System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute",
            "System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
            "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
            "System.Runtime.CompilerServices.SkipLocalsInitAttribute",
            "System.Runtime.CompilerServices.ModuleInitializerAttribute",
            "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute",
            "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes",
            "System.Diagnostics.CodeAnalysis.UnscopedRefAttribute",
            "System.Runtime.CompilerServices.CollectionBuilderAttribute",
            "System.Diagnostics.CodeAnalysis.ExperimentalAttribute",
            "System.Diagnostics.CodeAnalysis.MemberNotNullAttribute",
            "System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute",
            "System.Diagnostics.CodeAnalysis.StringSyntaxAttribute",
            "System.Text.AppendInterpolatedStringHandler",
            "System.Text.StringBuilderExtensions",
        };

        private static readonly HashSet<string> s_Reported = new HashSet<string>();

        static DuplicateProviderDiagnostics()
        {
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            List<string> hit = null;
            foreach (var m in messages)
            {
                if (m.type != CompilerMessageType.Error) continue;
                if (m.message.IndexOf("CS0518", StringComparison.Ordinal) < 0 && m.message.IndexOf("CS0433", StringComparison.Ordinal) < 0) continue;
                foreach (var name in s_TypeNames)
                {
                    if (m.message.IndexOf(SimpleName(name), StringComparison.Ordinal) < 0) continue;
                    if (hit == null) hit = new List<string>();
                    if (!hit.Contains(name)) hit.Add(name);
                }
            }
            if (hit == null) return;

            foreach (var path in CandidatePaths())
            {
                byte[] data;
                try { data = File.ReadAllBytes(path); } catch { continue; }
                foreach (var name in hit)
                {
                    if (IndexOf(data, Encoding.UTF8.GetBytes(SimpleName(name) + "\0")) < 0) continue;
                    if (!IsPublicIn(path, name)) continue;
                    if (!s_Reported.Add(path + "|" + name)) continue;
                    Debug.LogWarning("[Polyfill] '" + name + "' is also declared public in '" + Path.GetFileName(path) + "' (" + path + "). "
                        + "Two public declarations make the compiler report CS0518 or CS0433. Keep one provider: remove that assembly, or stop referencing xpTURN.Polyfill.Runtime where both are visible.");
                }
            }
        }

        private static string SimpleName(string fullName) => fullName.Substring(fullName.LastIndexOf('.') + 1);

        /// <summary>Precompiled DLLs and script assemblies, without this package's own runtime assembly.</summary>
        private static IEnumerable<string> CandidatePaths()
        {
            var seen = new HashSet<string>();
            foreach (var p in CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.UserAssembly))
                if (seen.Add(p)) yield return p;
            foreach (var a in CompilationPipeline.GetAssemblies())
            {
                if (a.name == OwnAssembly) continue;
                var p = a.outputPath;
                if (File.Exists(p) && seen.Add(p)) yield return p;
            }
        }

        /// <summary>
        /// An assembly of that name already in the domain answers directly; a file the editor has not loaded yet is read from bytes.
        /// (ReflectionOnlyLoadFrom fails on most of these files in the editor.)
        /// </summary>
        private static bool IsPublicIn(string path, string fullName)
        {
            try
            {
                var simpleName = Path.GetFileNameWithoutExtension(path);
                System.Reflection.Assembly asm = null;
#pragma warning disable UAC0005
                foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
#pragma warning restore UAC0005
                    if (loaded.GetName().Name == simpleName) { asm = loaded; break; }
#pragma warning disable UAC0020
                if (asm == null) asm = System.Reflection.Assembly.Load(File.ReadAllBytes(path));
#pragma warning restore UAC0020
                var t = asm.GetType(fullName, false);
                return t != null && t.IsPublic;
            }
            catch
            {
                return false;
            }
        }

        private static int IndexOf(byte[] hay, byte[] needle)
        {
            int last = hay.Length - needle.Length;
            byte first = needle[0];
            for (int i = 0; i <= last; i++)
            {
                if (hay[i] != first) continue;
                int j = 1;
                while (j < needle.Length && hay[i + j] == needle[j]) j++;
                if (j == needle.Length) return i;
            }
            return -1;
        }
    }
}
