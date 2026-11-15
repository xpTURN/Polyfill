using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace xpTURN.Polyfill.Editor
{
    /// <summary>
    /// Configures Project Settings -> Player -> Additional Compiler Arguments via script.
    /// -langversion: replaces existing value if present, otherwise adds it.
    /// </summary>
    public static class PlayerSettingsAdditionalCompilerArguments
    {
        public const string SettingsFileName = "xpTURN.Polyfill.Settings.json";
        public const string LangVersionPrefix = "-langversion:";
        /// <summary>The argument for the default value (preview). The applied value is the one chosen in Project Settings &gt; xpTURN Polyfill.</summary>
        public const string DesiredLangVersion = "-langversion:preview";
        public const string ScriptingDefineSymbolForLatestCSharp = "CSHARP_PREVIEW";

        private const string UnsupportedWarnedKey = "xpTURN.Polyfill.UnsupportedLangVersionWarned";

        [Serializable]
        internal class Settings
        {
            public bool applyArgumentsForAllTargets;
            public string langVersion = LangVersionOptions.Default;
        }

        private static Settings _cachedSettings;

        private static string GetSettingsPath()
        {
            var projectSettingsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectSettings"));
            return Path.Combine(projectSettingsDir, SettingsFileName);
        }

        /// <summary>Reads from file once on assembly load and caches the result.</summary>
        internal static Settings LoadSettings(string path)
        {
            if (!File.Exists(path))
                return new Settings { applyArgumentsForAllTargets = false };

            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonUtility.FromJson<Settings>(json);
                return settings ?? new Settings { applyArgumentsForAllTargets = false };
            }
            catch
            {
                return new Settings { applyArgumentsForAllTargets = false };
            }
        }

        private static void SaveSettings(Settings settings)
        {
            var path = GetSettingsPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(settings, prettyPrint: true));
        }

        private static void EnsureSettingsLoaded()
        {
            if (_cachedSettings != null) return;
            _cachedSettings = LoadSettings(GetSettingsPath());
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EnsureSettingsLoaded();
            if (AssetDatabase.IsAssetImportWorkerProcess()) return;
            if (!_cachedSettings.applyArgumentsForAllTargets) return;
            var value = GetLangVersion();
            if (LangVersionOptions.IsSupported(value) || SessionState.GetBool(UnsupportedWarnedKey, false)) return;
            SessionState.SetBool(UnsupportedWarnedKey, true);
            Debug.LogWarning($"[Polyfill] {SettingsFileName} sets C# language version '{value}', which this editor's compiler does not accept. Choose one of {string.Join(", ", LangVersionOptions.Values)} in Project Settings > xpTURN Polyfill.");
        }

        /// <summary>The chosen C# language version (preview when the file does not set one).</summary>
        internal static string GetLangVersion()
        {
            EnsureSettingsLoaded();
            return LangVersionOptions.Normalize(_cachedSettings.langVersion);
        }

        /// <summary>Saves both settings to ProjectSettings JSON.</summary>
        internal static void SaveSettings(bool applyArgumentsForAllTargets, string langVersion)
        {
            EnsureSettingsLoaded();
            _cachedSettings.applyArgumentsForAllTargets = applyArgumentsForAllTargets;
            _cachedSettings.langVersion = langVersion;
            SaveSettings(_cachedSettings);
        }

        /// <summary>
        /// Returns whether 'apply to all targets' is enabled (stored in ProjectSettings JSON). Default false.
        /// </summary>
        public static bool GetApplyArgumentsForAllTargetsEnabled()
        {
            EnsureSettingsLoaded();
            return _cachedSettings.applyArgumentsForAllTargets;
        }

        /// <summary>
        /// Saves the 'apply to all targets' setting to ProjectSettings JSON.
        /// </summary>
        public static void SetApplyArgumentsForAllTargetsEnabled(bool value)
        {
            EnsureSettingsLoaded();
            _cachedSettings.applyArgumentsForAllTargets = value;
            SaveSettings(_cachedSettings);
        }

        /// <summary>
        /// Calls ApplyArgumentsForTarget for every active NamedBuildTarget in the project.
        /// </summary>
        [MenuItem("Edit/Polyfill/Player Settings/Apply Additional Compiler Arguments -langversion (All Installed Platforms)")]
        public static void ApplyArgumentsForAllTargets()
        {
            var value = GetLangVersion();
            if (!LangVersionOptions.IsSupported(value))
            {
                Debug.LogError($"[Polyfill] Not applied: this editor's compiler does not accept C# language version '{value}'. Choose one of {string.Join(", ", LangVersionOptions.Values)} in Project Settings > xpTURN Polyfill.");
                return;
            }

            SetApplyArgumentsForAllTargetsEnabled(true);
            foreach (var target in GetActiveNamedBuildTargets())
            {
                try
                {
                    ApplyArgumentsForTarget(target);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Polyfill] Skip {target.TargetName}: {ex.Message}");
                }
            }

            RegenerateProjectFiles();
        }

        /// <summary>
        /// Removes the -langversion setting (previously applied by ApplyArgumentsForAllTargets) from all installed targets.
        /// </summary>
        [MenuItem("Edit/Polyfill/Player Settings/Remove Additional Compiler Arguments -langversion (All Installed Platforms)")]
        public static void RemoveArgumentsForAllTargets()
        {
            SetApplyArgumentsForAllTargetsEnabled(false);
            foreach (var target in GetActiveNamedBuildTargets())
            {
                try
                {
                    RemoveArgumentsForTarget(target);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Polyfill] Skip {target.TargetName}: {ex.Message}");
                }
            }

            RegenerateProjectFiles();
        }

        /// <summary>
        /// Opens Project Settings &gt; xpTURN Polyfill.
        /// </summary>
        [MenuItem("Edit/Polyfill/Settings…")]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(PolyfillSettingsProvider.SettingsPath);
        }

        /// <summary>
        /// Regenerates .sln and .csproj files (same as Assets &gt; Open C# Project). Call after changing compiler args or define symbols so IDEs pick up changes.
        /// </summary>
        [MenuItem("Edit/Polyfill/Regenerate Project Files")]
        public static void RegenerateProjectFiles()
        {
            try
            {
                var editor = CodeEditor.Editor;
                if (editor?.CurrentCodeEditor == null)
                {
                    Debug.LogWarning("[Polyfill] Regenerate Project Files: no external code editor registered.");
                    return;
                }
                editor.CurrentCodeEditor.SyncAll();
                Debug.Log("[Polyfill] Project files (.sln / .csproj) regenerated.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Removes all -langversion:* arguments from the given build target's Additional Compiler Arguments.
        /// </summary>
        public static void RemoveArgumentsForTarget(NamedBuildTarget buildTarget)
        {
            var existing = PlayerSettings.GetAdditionalCompilerArguments(buildTarget);
            var result = new List<string>();
            foreach (var arg in existing)
            {
                if (string.IsNullOrEmpty(arg?.Trim())) continue;

                // Strip any -langversion:* (e.g. -langversion:preview, -langversion:12)
                var content = Regex.Replace(arg, @"(?i)-langversion:\S*", string.Empty).Trim();
                if (!string.IsNullOrEmpty(content))
                    result.Add(content);
            }
            PlayerSettings.SetAdditionalCompilerArguments(buildTarget, result.ToArray());

            // Remove Scripting Define Symbol 'CSHARP_PREVIEW'
            RemoveScriptingDefineSymbol(buildTarget, ScriptingDefineSymbolForLatestCSharp);

            Debug.Log($"[Polyfill] -langversion and Scripting Define Symbol removed for {buildTarget.TargetName}");
        }

        /// <summary>
        /// Returns NamedBuildTargets only for platforms installed in the current editor. Uninstalled platforms are excluded.
        /// </summary>
        internal static IEnumerable<NamedBuildTarget> GetActiveNamedBuildTargets()
        {
            var supportedGroups = new HashSet<BuildTargetGroup>();
#pragma warning disable IL3050
            var targets = (BuildTarget[])Enum.GetValues(typeof(BuildTarget));
#pragma warning restore IL3050
            foreach (var buildTarget in targets)
            {
                try
                {
                    var group = BuildPipeline.GetBuildTargetGroup(buildTarget);
                    if (group == BuildTargetGroup.Unknown) continue;
                    if (BuildPipeline.IsBuildTargetSupported(group, buildTarget))
                        supportedGroups.Add(group);
                }
                catch (Exception ex)
                {
                    // Some targets may throw from GetBuildTargetGroup etc.
                    Debug.LogWarning($"[Polyfill] Skip {buildTarget}: {ex.Message}");
                }
            }

            foreach (var group in supportedGroups)
            {
                NamedBuildTarget target;
                try
                {
                    target = NamedBuildTarget.FromBuildTargetGroup(group);
                }
                catch
                {
                    continue;
                }
                if (target.TargetName == "Unknown") continue;
                yield return target;
            }
        }

        /// <summary>
        /// Removes the given define symbol from Scripting Define Symbols for the build target.
        /// </summary>
        private static void RemoveScriptingDefineSymbol(NamedBuildTarget buildTarget, string symbol)
        {
            var current = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

            var list = new List<string>();
            foreach (var s in current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = s.Trim();
                if (string.IsNullOrEmpty(trimmed) || string.Equals(trimmed, symbol, StringComparison.Ordinal))
                    continue;

                list.Add(trimmed);
            }

            PlayerSettings.SetScriptingDefineSymbols(buildTarget, list.ToArray());
        }

        /// <summary>
        /// Writes -langversion:&lt;chosen value&gt; in place of any existing -langversion, and CSHARP_PREVIEW only for preview.
        /// Leaves the target untouched where nothing changes.
        /// </summary>
        public static void ApplyArgumentsForTarget(NamedBuildTarget buildTarget)
        {
            var value = GetLangVersion();
            var existing = PlayerSettings.GetAdditionalCompilerArguments(buildTarget);
            var merged = LangVersionOptions.MergeArguments(existing, value);
            if (!LangVersionOptions.SameEntries(existing, merged))
                PlayerSettings.SetAdditionalCompilerArguments(buildTarget, merged);

            var symbols = PlayerSettings.GetScriptingDefineSymbols(buildTarget).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var mergedSymbols = LangVersionOptions.MergeSymbols(symbols, value);
            if (!LangVersionOptions.SameEntries(symbols, mergedSymbols))
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, mergedSymbols);

            Debug.Log($"[Polyfill] Additional Compiler Arguments applied for {buildTarget.TargetName}: -langversion -> {LangVersionPrefix}{value}, {ScriptingDefineSymbolForLatestCSharp} {(value == LangVersionOptions.Preview ? "added" : "removed")}");
        }
    }
}
