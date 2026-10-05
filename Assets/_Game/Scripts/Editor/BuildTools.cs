using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SharkHunter.EditorTools
{
    public static class BuildTools
    {
        const string Scene = "Assets/_Game/Scenes/Main.unity";

        [MenuItem("Tools/Shark Hunter/Build WebGL")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Build/WebGL");

        [MenuItem("Tools/Shark Hunter/Build macOS")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Build/macOS/SharkHunter25D.app");

        static void Build(BuildTarget target, string output)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var opts = new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = output, target = target, options = BuildOptions.None };
            var report = BuildPipeline.BuildPlayer(opts);
            long bytes = 0;
            if (Directory.Exists(output)) foreach (var f in new DirectoryInfo(output).GetFiles("*", SearchOption.AllDirectories)) bytes += f.Length;
            Debug.Log($"Build {target}: {report.summary.result} in {report.summary.totalTime:mm\\:ss}, output {bytes / 1048576f:F1} MB");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
