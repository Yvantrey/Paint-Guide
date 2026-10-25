using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the Android APK. Use the menu RUGA > Build > Android APK, or from the command line:
// Unity.exe -batchmode -quit -projectPath <project> -buildTarget Android -executeMethod RugaBuild.BuildAndroid
public static class RugaBuild
{
    const string OutputPath = "Builds/RUGA.apk";

    [MenuItem("RUGA/Build/Android APK")]
    public static void BuildAndroid()
    {
        // Make sure the scene, prefabs and font assets are up to date first.
        RugaProjectSetup.SetupFullApp();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        var summary = report.summary;
        Debug.Log($"RUGA BUILD RESULT: {summary.result}, {summary.totalErrors} errors, {summary.totalWarnings} warnings, " +
                  $"{summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime:mm\\:ss}, output {summary.outputPath}");

        if (Application.isBatchMode)
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
