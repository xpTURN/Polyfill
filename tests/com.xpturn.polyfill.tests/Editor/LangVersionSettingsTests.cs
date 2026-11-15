using System;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;
using UnityEditor;
using xpTURN.Polyfill.Editor;

namespace xpTURN.Polyfill.Tests.Editor
{
    public class LangVersionSettingsTests
    {
        [Test]
        public void T1_Values_FollowTheCompiler()
        {
#if UNITY_6000_5_OR_NEWER
            CollectionAssert.AreEqual(new[] { "10.0", "11.0", "12.0", "preview" }, LangVersionOptions.Values);
#else
            CollectionAssert.AreEqual(new[] { "10.0", "preview" }, LangVersionOptions.Values);
#endif
            Assert.AreEqual("preview", LangVersionOptions.Default);
        }

        [TestCase(new string[0], "10.0", new[] { "-langversion:10.0" })]
        [TestCase(new[] { "-nullable:enable", "-langversion:9" }, "10.0", new[] { "-nullable:enable", "-langversion:10.0" })]
        [TestCase(new[] { "-langversion:9", "-nullable:enable" }, "preview", new[] { "-langversion:preview", "-nullable:enable" })]
        [TestCase(new[] { "-langversion:9", "-warnaserror", "-LangVersion:latest" }, "10.0", new[] { "-langversion:10.0", "-warnaserror" })]
        [TestCase(new[] { "-nullable:enable -langversion:9 -warnaserror" }, "10.0", new[] { "-nullable:enable -langversion:10.0 -warnaserror" })]
        [TestCase(new[] { "-langversion:preview" }, "preview", new[] { "-langversion:preview" })]
        [TestCase(new[] { "-langversion:preview", "  " }, "10.0", new[] { "-langversion:10.0" })]
        public void T2_MergeArguments(string[] existing, string value, string[] expected)
        {
            CollectionAssert.AreEqual(expected, LangVersionOptions.MergeArguments(existing, value));
        }

        [Test]
        public void T2_SameValue_IsUnchanged()
        {
            var existing = new[] { "-nullable:enable", "-langversion:preview" };
            Assert.IsTrue(LangVersionOptions.SameEntries(existing, LangVersionOptions.MergeArguments(existing, "preview")));
            Assert.IsFalse(LangVersionOptions.SameEntries(existing, LangVersionOptions.MergeArguments(existing, "10.0")));
        }

        [TestCase("{\"applyArgumentsForAllTargets\": true}", true, "preview")]
        [TestCase("{\"applyArgumentsForAllTargets\": true, \"langVersion\": \"10.0\"}", true, "10.0")]
        [TestCase("{\"applyArgumentsForAllTargets\": true, \"langVersion\": \"\"}", true, "preview")]
        [TestCase("{\"applyArgumentsForAllTargets\": true, \"langVersion\": null}", true, "preview")]
        [TestCase("{\"applyArgumentsForAllTargets\": true, \"langVersion\": 12}", true, "12")]
        [TestCase("{ \"applyArgumentsForAllTargets\": tr", false, "preview")]
        [TestCase("", false, "preview")]
        public void T3_ReadSettings(string json, bool enabled, string langVersion)
        {
            var path = Path.Combine(Path.GetTempPath(), "PolyfillT3-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, json);
            try
            {
                var settings = PlayerSettingsAdditionalCompilerArguments.LoadSettings(path);
                Assert.AreEqual(enabled, settings.applyArgumentsForAllTargets);
                Assert.AreEqual(langVersion, LangVersionOptions.Normalize(settings.langVersion));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void T3_MissingFile_IsOff()
        {
            var settings = PlayerSettingsAdditionalCompilerArguments.LoadSettings(Path.Combine(Path.GetTempPath(), "PolyfillT3-missing-" + Guid.NewGuid().ToString("N") + ".json"));
            Assert.IsFalse(settings.applyArgumentsForAllTargets);
            Assert.AreEqual("preview", LangVersionOptions.Normalize(settings.langVersion));
        }

        [Test]
        public void T3_RoundTrip()
        {
            var json = UnityEngine.JsonUtility.ToJson(new PlayerSettingsAdditionalCompilerArguments.Settings { applyArgumentsForAllTargets = true, langVersion = "10.0" }, true);
            var path = Path.Combine(Path.GetTempPath(), "PolyfillT3-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, json);
            try
            {
                var settings = PlayerSettingsAdditionalCompilerArguments.LoadSettings(path);
                Assert.IsTrue(settings.applyArgumentsForAllTargets);
                Assert.AreEqual("10.0", settings.langVersion);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestCase(new[] { "FOO", "BAR" }, "preview", new[] { "FOO", "BAR", "CSHARP_PREVIEW" })]
        [TestCase(new[] { "CSHARP_PREVIEW", "FOO" }, "preview", new[] { "CSHARP_PREVIEW", "FOO" })]
        [TestCase(new[] { "FOO", "CSHARP_PREVIEW", "BAR" }, "10.0", new[] { "FOO", "BAR" })]
        [TestCase(new[] { "FOO" }, "10.0", new[] { "FOO" })]
        [TestCase(new[] { " FOO ", "CSHARP_PREVIEW", "CSHARP_PREVIEW" }, "preview", new[] { "FOO", "CSHARP_PREVIEW" })]
        public void T4_MergeSymbols(string[] current, string value, string[] expected)
        {
            CollectionAssert.AreEqual(expected, LangVersionOptions.MergeSymbols(current, value));
        }

        private const string Csproj = "<Project>\r\n  <PropertyGroup>\r\n    <LangVersion>9.0</LangVersion>\r\n  </PropertyGroup>\r\n</Project>";

        [TestCase("xpTURN.Polyfill.Runtime.csproj", "10.0")]
        [TestCase("xpTURN.Polyfill.Runtime.csproj", "preview")]
        [TestCase("Foo.Player.csproj", "10.0")]
        public void T5_Csproj_GetsTheChosenValue(string name, string value)
        {
            var result = CsprojLangVersionPostprocessor.Process(name, Csproj, true, value);
            StringAssert.Contains("<LangVersion>" + value + "</LangVersion>", result);
            StringAssert.DoesNotContain("<LangVersion>9.0</LangVersion>", result);
        }

        [Test]
        public void T5_Csproj_Unchanged()
        {
            Assert.AreEqual(Csproj, CsprojLangVersionPostprocessor.Process("A.csproj", Csproj, false, "10.0"), "off");
            Assert.AreEqual(Csproj, CsprojLangVersionPostprocessor.Process("A.csproj", Csproj, true, "13.0"), "value outside the list");
            Assert.AreEqual("<Project />", CsprojLangVersionPostprocessor.Process("A.csproj", "<Project />", true, "10.0"), "no LangVersion");
            Assert.AreEqual(Csproj, CsprojLangVersionPostprocessor.Process("A.sln", Csproj, true, "10.0"), "not a .csproj");
        }

        [Test]
        public void T6_SettingsPage_IsRegistered()
        {
            var provider = PolyfillSettingsProvider.Create();
            Assert.AreEqual("Project/xpTURN Polyfill", provider.settingsPath);
            Assert.AreEqual(SettingsScope.Project, provider.scope);

            var fetch = typeof(SettingsService).GetMethod("FetchSettingsProviders", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            Assume.That(fetch, Is.Not.Null, "SettingsService.FetchSettingsProviders is internal and may be renamed");
            var providers = (SettingsProvider[])fetch.Invoke(null, null);
            Assert.AreEqual(1, providers.Count(p => p.settingsPath == "Project/xpTURN Polyfill"));
        }

        [Test]
        public void T6_SettingsMenu_IsListed()
        {
            CollectionAssert.Contains(Unsupported.GetSubmenus("Edit/Polyfill"), "Edit/Polyfill/Settings…");
        }

        [Test]
        public void T8_LangVersionOptionError_IsRecognised()
        {
            Assert.IsTrue(LangVersionDiagnostics.IsLangVersionOptionError("error CS1617: Invalid option '12.0' for /langversion. Use '/langversion:?' to list supported values."));
            Assert.IsFalse(LangVersionDiagnostics.IsLangVersionOptionError("error CS8652: The feature 'raw string literals' is currently in Preview and *unsupported*."));
            Assert.IsFalse(LangVersionDiagnostics.IsLangVersionOptionError("error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported"));
        }

        [Test]
        public void T8_Message_NamesTheArgumentAndTheChoices()
        {
            var message = LangVersionDiagnostics.BuildMessage("Standalone", new[] { "-nullable:enable", "-langversion:12.0" });
            StringAssert.Contains("Standalone: -nullable:enable -langversion:12.0", message);
            StringAssert.Contains(string.Join(", ", LangVersionOptions.Values), message);
            StringAssert.Contains("Project Settings > xpTURN Polyfill", message);
        }
    }
}
