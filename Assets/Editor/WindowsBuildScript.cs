#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class WindowsBuildScript
{
    [MenuItem("Re9lay/Build Windows 64-bit")]
    public static void PerformBuild()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string buildDir = Path.Combine(projectRoot, "Builds/Windows");
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string exePath = Path.Combine(buildDir, "Re9lay.exe");

        string[] scenes = new[] { "Assets/Level1.unity" };
        if (EditorBuildSettings.scenes != null && EditorBuildSettings.scenes.Length > 0)
        {
            var activeScenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled) activeScenes.Add(s.path);
            }
            if (activeScenes.Count > 0) scenes = activeScenes.ToArray();
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("[WindowsBuildScript] Starting Windows 64-bit Standalone Player Build...");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            long sizeMB = (long)(summary.totalSize / (1024 * 1024));
            Debug.Log($"[WindowsBuildScript] SUCCESS: Windows build completed at {exePath} ({sizeMB} MB) in {summary.totalTime.TotalSeconds:F1}s");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError($"[WindowsBuildScript] FAILED: Build failed with {summary.totalErrors} errors (Result: {summary.result})");
        }
    }
}
#endif
