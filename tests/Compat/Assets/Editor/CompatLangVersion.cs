using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Reports what the language version setting reached: the Player Settings of every installed target, the Bee response
// files and the .csproj files, generated with the Visual Studio package's project generator so that the result does
// not depend on the machine's external code editor.
public static class CompatLangVersion
{
    static readonly string[] Assemblies = { "xpTURN.Polyfill.Runtime", "Case.C1_Polyfill", "xpTURN.Polyfill.Samples", "xpTURN.Polyfill.Runtime.Tests", "Assembly-CSharp-Editor" };

    public static void Report()
    {
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, "-compatLangVersionReport");
        var output = i >= 0 && i + 1 < args.Length ? args[i + 1] : "langversion-report.txt";
        var root = Directory.GetParent(Application.dataPath).FullName;
        var lines = new List<string> { "unity " + Application.unityVersion };

        var settings = Path.Combine(root, "ProjectSettings", "xpTURN.Polyfill.Settings.json");
        lines.Add("settings " + (File.Exists(settings) ? Regex.Replace(File.ReadAllText(settings), @"\s+", " ") : "missing"));

        foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android, BuildTargetGroup.iOS })
        {
            var target = NamedBuildTarget.FromBuildTargetGroup(group);
            lines.Add($"target {target.TargetName} args=[{string.Join(" | ", PlayerSettings.GetAdditionalCompilerArguments(target))}] symbols=[{PlayerSettings.GetScriptingDefineSymbols(target)}]");
        }

        foreach (var asm in Assemblies)
        {
            var rsp = Directory.GetFiles(Path.Combine(root, "Library", "Bee", "artifacts"), asm + ".rsp", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            var values = rsp == null ? new string[0] : Regex.Matches(File.ReadAllText(rsp), @"(?im)^-langversion:(\S+)").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            lines.Add($"rsp {asm} {(rsp == null ? "missing" : string.Join(" > ", values))}");
        }

        lines.Add("currentCodeEditor " + (CodeEditor.Editor.CurrentCodeEditor?.GetType().FullName ?? "null"));
        foreach (var f in Directory.GetFiles(root, "*.csproj")) File.Delete(f);
        var generator = Type.GetType("Microsoft.Unity.VisualStudio.Editor.LegacyStyleProjectGeneration, Unity.VisualStudio.Editor");
        if (generator == null)
        {
            lines.Add("generator missing");
        }
        else
        {
            var instance = Activator.CreateInstance(generator, true);
            generator.GetMethod("Sync", Type.EmptyTypes).Invoke(instance, null);
            foreach (var f in Directory.GetFiles(root, "*.csproj").OrderBy(f => f))
            {
                var m = Regex.Match(File.ReadAllText(f), "<LangVersion>([^<]*)</LangVersion>");
                lines.Add($"csproj {Path.GetFileName(f)} {(m.Success ? m.Groups[1].Value : "none")}");
            }
        }

        File.WriteAllLines(output, lines);
    }
}
