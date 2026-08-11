using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Scripted Android build so the deliverable APK is produced by a reviewable,
/// repeatable command rather than remembered dialog clicks. Player settings
/// (IL2CPP, ARM64, identifiers) live in ProjectSettings — this only builds.
/// </summary>
public static class BuildTools
{
    [MenuItem("PsyCurio/Build Android APK")]
    public static void BuildAndroid()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Shop.unity" },
            locationPathName = "Builds/psycurio-checkout.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"BuildTools: {report.summary.result}, errors={report.summary.totalErrors}, "
            + $"size={report.summary.totalSize / (1024 * 1024)} MB, time={report.summary.totalTime.TotalMinutes:F1} min");

        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
