using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

namespace xpTURN.Polyfill.Editor
{
    /// <summary>
    /// Project Settings &gt; xpTURN Polyfill: whether to apply, the C# language version, and what Player Settings and .csproj
    /// hold now. Edits stay on the page until Apply.
    /// </summary>
    internal sealed class PolyfillSettingsProvider : SettingsProvider
    {
        internal const string SettingsPath = "Project/xpTURN Polyfill";
        private const float LabelWidth = 220f;

        private const double PlayerSettingsRefreshSeconds = 1.0;

        private bool _enabled;
        private string _langVersion;
        private string _playerSettingsLine;
        private string _playerSettingsWarning;
        private double _playerSettingsReadAt = double.MinValue;

        private PolyfillSettingsProvider()
            : base(SettingsPath, SettingsScope.Project, new[] { "Polyfill", "C#", "language version", "langversion", PlayerSettingsAdditionalCompilerArguments.ScriptingDefineSymbolForLatestCSharp })
        {
            label = "xpTURN Polyfill";
        }

        [SettingsProvider]
        internal static SettingsProvider Create()
        {
            return new PolyfillSettingsProvider();
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            Revert();
        }

        private void Revert()
        {
            _enabled = PlayerSettingsAdditionalCompilerArguments.GetApplyArgumentsForAllTargetsEnabled();
            _langVersion = PlayerSettingsAdditionalCompilerArguments.GetLangVersion();
            _playerSettingsReadAt = double.MinValue;
        }

        public override void OnGUI(string searchContext)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LabelWidth;
            try
            {
                DrawPage();
            }
            finally
            {
                EditorGUIUtility.labelWidth = labelWidth;
            }
        }

        private void DrawPage()
        {
            EditorGUILayout.LabelField("C# language version", EditorStyles.boldLabel);
            _enabled = EditorGUILayout.Toggle("Apply to all installed platforms", _enabled);

            var values = LangVersionOptions.Values;
            var supported = LangVersionOptions.IsSupported(_langVersion);
            using (new EditorGUI.DisabledScope(!_enabled))
            {
                var index = EditorGUILayout.Popup("Version", Array.IndexOf(values, _langVersion), values);
                if (index >= 0)
                {
                    _langVersion = values[index];
                    supported = true;
                }
            }

            if (!supported)
                EditorGUILayout.HelpBox($"'{_langVersion}' is not accepted by this editor's C# compiler. Choose one of {string.Join(", ", values)}.", MessageType.Error);
            else if (_enabled && _langVersion != LangVersionOptions.Preview && _langVersion != "10.0")
                EditorGUILayout.HelpBox($"Editors before Unity 6000.5 reject -langversion:{_langVersion} (CS1617) and compile nothing. Choose 10.0 or preview if they open this project.", MessageType.Info);

            if (Event.current.type == EventType.Layout && EditorApplication.timeSinceStartup - _playerSettingsReadAt > PlayerSettingsRefreshSeconds)
            {
                ReadPlayerSettings(out _playerSettingsLine, out _playerSettingsWarning);
                _playerSettingsReadAt = EditorApplication.timeSinceStartup;
            }
            EditorGUILayout.LabelField("Player Settings:", _playerSettingsLine ?? string.Empty);
            if (_playerSettingsWarning != null)
                EditorGUILayout.HelpBox(_playerSettingsWarning, MessageType.Warning);
            EditorGUILayout.LabelField(".csproj (IDE):", PlayerSettingsAdditionalCompilerArguments.GetApplyArgumentsForAllTargetsEnabled() && LangVersionOptions.IsSupported(PlayerSettingsAdditionalCompilerArguments.GetLangVersion())
                ? $"<LangVersion>{PlayerSettingsAdditionalCompilerArguments.GetLangVersion()}</LangVersion> — all projects"
                : "unchanged (IDE default)");

            var dirty = _enabled != PlayerSettingsAdditionalCompilerArguments.GetApplyArgumentsForAllTargetsEnabled()
                || _langVersion != PlayerSettingsAdditionalCompilerArguments.GetLangVersion();
            if (dirty)
                EditorGUILayout.HelpBox("Unapplied changes", MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!dirty))
                {
                    if (GUILayout.Button("Revert")) Revert();
                }
                using (new EditorGUI.DisabledScope(!dirty || (_enabled && !supported)))
                {
                    if (GUILayout.Button("Apply"))
                    {
                        Apply(_enabled, _langVersion);
                        _playerSettingsReadAt = double.MinValue;
                    }
                }
                if (GUILayout.Button("Regenerate Project Files")) PlayerSettingsAdditionalCompilerArguments.RegenerateProjectFiles();
            }
        }

        private static void ReadPlayerSettings(out string line, out string warning)
        {
            var saved = PlayerSettingsAdditionalCompilerArguments.GetLangVersion();
            var expected = PlayerSettingsAdditionalCompilerArguments.LangVersionPrefix + saved;
            var targets = PlayerSettingsAdditionalCompilerArguments.GetActiveNamedBuildTargets().ToList();
            var summaries = new List<string>();
            var matching = 0;
            foreach (var target in targets)
            {
                var args = PlayerSettings.GetAdditionalCompilerArguments(target);
                var symbols = PlayerSettings.GetScriptingDefineSymbols(target);
                var langVersions = args.SelectMany(a => a.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                    .Where(t => t.StartsWith(PlayerSettingsAdditionalCompilerArguments.LangVersionPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
                var hasPreviewSymbol = symbols.Split(';').Any(s => s.Trim() == PlayerSettingsAdditionalCompilerArguments.ScriptingDefineSymbolForLatestCSharp);
                var summary = (langVersions.Length == 0 ? "no -langversion" : string.Join(" ", langVersions))
                    + (hasPreviewSymbol ? " · " + PlayerSettingsAdditionalCompilerArguments.ScriptingDefineSymbolForLatestCSharp : string.Empty);
                summaries.Add($"{target.TargetName}: {summary}");
                if (langVersions.Length == 1 && string.Equals(langVersions[0], expected, StringComparison.OrdinalIgnoreCase)
                    && hasPreviewSymbol == (saved == LangVersionOptions.Preview))
                    matching++;
            }

            var first = summaries.Count > 0 ? summaries[0].Substring(summaries[0].IndexOf(':') + 2) : "no installed platform";
            line = $"{first} — {matching}/{targets.Count} platforms";
            warning = PlayerSettingsAdditionalCompilerArguments.GetApplyArgumentsForAllTargetsEnabled() && matching != targets.Count
                ? "Player Settings differ from the saved choice:\n" + string.Join("\n", summaries)
                : null;
        }

        internal static void Apply(bool enabled, string langVersion)
        {
            PlayerSettingsAdditionalCompilerArguments.SaveSettings(enabled, langVersion);
            if (enabled)
                PlayerSettingsAdditionalCompilerArguments.ApplyArgumentsForAllTargets();
            else
                PlayerSettingsAdditionalCompilerArguments.RemoveArgumentsForAllTargets();
        }
    }
}
