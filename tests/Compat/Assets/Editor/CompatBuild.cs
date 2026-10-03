using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CompatBuild
{
    public static void BuildMono() => Build(ScriptingImplementation.Mono2x, "Build/mono/Compat.app");
    public static void BuildIl2cpp() => Build(ScriptingImplementation.IL2CPP, "Build/il2cpp/Compat.app");

    public static void UseIl2cpp() => Configure(ScriptingImplementation.IL2CPP);
    public static void UseNetFramework() => SetApiLevel(ApiCompatibilityLevel.NET_Unity_4_8);
    public static void UseNetStandard() => SetApiLevel(ApiCompatibilityLevel.NET_Standard);

    static void Build(ScriptingImplementation backend, string path)
    {
        Configure(backend);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Compat.unity");
        var options = new BuildPlayerOptions { scenes = new[] { "Assets/Compat.unity" }, locationPathName = path, target = BuildTarget.StandaloneOSX, options = BuildOptions.None };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("COMPATBUILD|" + backend + "|" + report.summary.result + "|seconds=" + report.summary.totalTime.TotalSeconds.ToString("F0"));
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(2);
    }

    static void Configure(ScriptingImplementation backend)
    {
        PlayerSettings.productName = "Compat";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
        SetArm64();
        AssetDatabase.SaveAssets();
        Debug.Log("COMPATBUILD|backend|" + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
    }

    static void SetApiLevel(ApiCompatibilityLevel level)
    {
        PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, level);
        AssetDatabase.SaveAssets();
        Debug.Log("COMPATBUILD|api|" + PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone));
    }

    static void SetArm64()
    {
#pragma warning disable UAC0005
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#pragma warning restore UAC0005
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType("UnityEditor.OSXStandalone.UserBuildSettings");
            var property = type?.GetProperty("architecture");
            if (property == null) continue;
            property.SetValue(null, Enum.Parse(property.PropertyType, "ARM64"));
            Debug.Log("COMPATBUILD|arch|" + property.GetValue(null));
            return;
        }
        Debug.Log("COMPATBUILD|arch|not-set");
    }
}
