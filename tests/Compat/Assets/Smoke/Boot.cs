using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[assembly: UnityEngine.Scripting.AlwaysLinkAssembly]

namespace Compat
{
    public static class Boot
    {
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Run()
        {
            string outPath = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-compatSmoke") outPath = args[i + 1];
            if (outPath == null) return;
#if ENABLE_IL2CPP
            const string env = "il2cpp";
#else
            const string env = "mono";
#endif
            var lines = new List<string>();
            try
            {
                Smoke.Run((name, result) => lines.Add("SMOKE|" + env + "|" + name + "|" + result));
                lines.Add("SMOKE|" + env + "|end|DONE");
            }
            catch (Exception e)
            {
                lines.Add("SMOKE|" + env + "|fatal|" + e);
            }
            File.WriteAllLines(outPath, lines.ToArray());
            Application.Quit(0);
        }
#endif
    }
}
