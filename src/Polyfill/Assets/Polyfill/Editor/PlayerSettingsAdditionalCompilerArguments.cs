using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
        private const string SettingsFileName = "xpTURN.Polyfill.Settings.json";

        private const string LangVersionPrefix = "-langversion:";
        private static readonly string DesiredLangVersion = "-langversion:preview";

        [Serializable]
        private class Settings
        {
            public bool applyArgumentsForAllTargets;
        }

        private static Settings _cachedSettings;

        private static string GetSettingsPath()
        {
            var projectSettingsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectSettings"));
            return Path.Combine(projectSettingsDir, SettingsFileName);
        }

        /// <summary>Reads from file once on assembly load and caches the result.</summary>
        private static Settings LoadSettings()
        {
            var path = GetSettingsPath();
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
            _cachedSettings = LoadSettings();
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EnsureSettingsLoaded();
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
                if (string.IsNullOrEmpty(arg.Trim())) continue;

                var content = arg.Replace(DesiredLangVersion, string.Empty).Trim();
                if (!string.IsNullOrEmpty(content))
                    result.Add(content);
            }
            PlayerSettings.SetAdditionalCompilerArguments(buildTarget, result.ToArray());
            Debug.Log($"[Polyfill] -langversion removed for {buildTarget.TargetName}");
        }

        /// <summary>
        /// Returns NamedBuildTargets only for platforms installed in the current editor. Uninstalled platforms are excluded.
        /// </summary>
        private static IEnumerable<NamedBuildTarget> GetActiveNamedBuildTargets()
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
        /// -langversion: replaces existing value if present, otherwise adds it. Other arguments are merged.
        /// Skips if already set to DesiredLangVersion (preview).
        /// </summary>
        public static void ApplyArgumentsForTarget(NamedBuildTarget buildTarget)
        {
            var existing = PlayerSettings.GetAdditionalCompilerArguments(buildTarget);
            if (IsAlreadyDesiredLangVersion(existing))
                return;

            var result = new List<string>();
            var langVersionReplaced = false;

            foreach (var arg in existing)
            {
                if (IsLangVersionArgument(arg))
                {
                    if (!langVersionReplaced)
                    {
                        // Detect -langversion:9, -langversion:9.0, -langversion:11 etc. and replace with desired value
                        var pattern = @"(?i)-langversion:\S*";
                        var content = Regex.Replace(arg, pattern, DesiredLangVersion);
                        result.Add(content.Trim());
                        langVersionReplaced = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(arg.Trim()))
                        result.Add(arg);
                }
            }

            if (!langVersionReplaced)
                result.Add(DesiredLangVersion);

            PlayerSettings.SetAdditionalCompilerArguments(buildTarget, result.ToArray());
            Debug.Log($"[Polyfill] Additional Compiler Arguments applied for {buildTarget.TargetName}: -langversion -> {DesiredLangVersion}");
        }

        /// <summary>
        /// Detects whether existing arguments already contain the desired -langversion value (e.g. preview).
        /// Only considers -langversion arguments; returns true only when the trimmed value matches DesiredLangVersion.
        /// </summary>
        private static bool IsAlreadyDesiredLangVersion(string[] existing)
        {
            if (existing == null) return false;
            foreach (var arg in existing)
            {
                if (arg == null) continue;
                if (!IsLangVersionArgument(arg)) continue;
                if (arg.Contains(DesiredLangVersion, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Detects whether the argument is a -langversion: argument.
        /// </summary>
        private static bool IsLangVersionArgument(string arg)
        {
            return arg != null && arg.Contains(LangVersionPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
