using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

namespace AutoBuild
{
    /// <summary>
    /// Wraps Unity's BuildPipeline with custom naming, folder creation, and build reporting.
    /// </summary>
    public static class BuildPipelineManager
    {
        // ─────────────────────────────────────────────
        //  Build Result
        // ─────────────────────────────────────────────

        public struct BuildResult
        {
            public bool Success;
            public string OutputPath;
            public string OutputDirectory;
            public double DurationSeconds;
            public double BuildSizeMB;
            public string ErrorMessage;
        }

        // ─────────────────────────────────────────────
        //  Helper Methods
        // ─────────────────────────────────────────────

        /// <summary>
        /// Returns the product name from PlayerSettings.
        /// </summary>
        public static string GetProductName()
        {
            return PlayerSettings.productName;
        }

        /// <summary>
        /// Returns the active build target.
        /// </summary>
        public static BuildTarget GetActiveBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }

        /// <summary>
        /// Converts a BuildTarget to a readable short platform name.
        /// </summary>
        public static string GetPlatformDisplayName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                    return "Windows32";
                case BuildTarget.StandaloneWindows64:
                    return "Windows64";
                case BuildTarget.StandaloneOSX:
                    return "macOS";
                case BuildTarget.StandaloneLinux64:
                    return "Linux64";
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                default:
                    return target.ToString();
            }
        }

        /// <summary>
        /// Returns the executable file extension for the given build target.
        /// </summary>
        public static string GetExecutableExtension(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.Android:
                    return ".apk";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Returns paths of scenes enabled in Build Settings.
        /// </summary>
        public static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();
        }

        /// <summary>
        /// Generates the build folder name using the naming formula.
        /// Format: ProductName_vX.Y.Z_Platform
        /// </summary>
        public static string GenerateBuildFolderName(string productName, string version, string platformName)
        {
            string safeName = SanitizeFileName(productName);
            return $"{safeName}_{version}_{platformName}";
        }

        /// <summary>
        /// Generates the full build output path (folder + executable).
        /// Example: D:/Builds/DeadMargin_v1.2.4_Windows64/DeadMargin_v1.2.4_Windows64.exe
        /// </summary>
        public static string GenerateFullBuildPath(string baseOutputDir, string productName, string version, BuildTarget target)
        {
            string platformName = GetPlatformDisplayName(target);
            string folderName = GenerateBuildFolderName(productName, version, platformName);
            string extension = GetExecutableExtension(target);
            string fileName = folderName + extension;

            return Path.Combine(baseOutputDir, folderName, fileName);
        }

        /// <summary>
        /// Removes invalid characters from a file name.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }
            name = name.Replace(' ', '_');
            return name;
        }

        // ─────────────────────────────────────────────
        //  Main Build Method
        // ─────────────────────────────────────────────

        /// <summary>
        /// Starts the Unity build process.
        /// Automatically creates folders, applies naming formula, and returns a BuildResult.
        /// </summary>
        public static BuildResult BuildGame(string baseOutputDir, string version)
        {
            BuildResult result = new BuildResult();

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No enabled scenes found in Build Settings. Please add at least one scene.";
                Debug.LogError($"[AutoBuild] {result.ErrorMessage}");
                return result;
            }

            BuildTarget target = GetActiveBuildTarget();
            string productName = GetProductName();
            string fullPath = GenerateFullBuildPath(baseOutputDir, productName, version, target);
            string outputDirectory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            result.OutputPath = fullPath;
            result.OutputDirectory = outputDirectory;

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullPath,
                target = target,
                options = BuildOptions.None
            };

            Debug.Log($"[AutoBuild] Starting build...");
            Debug.Log($"[AutoBuild] Target: {fullPath}");
            Debug.Log($"[AutoBuild] Platform: {GetPlatformDisplayName(target)}");
            Debug.Log($"[AutoBuild] Scene count: {scenes.Length}");

            DateTime startTime = DateTime.Now;
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            TimeSpan elapsed = DateTime.Now - startTime;

            result.DurationSeconds = elapsed.TotalSeconds;

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                result.Success = true;
                result.BuildSizeMB = report.summary.totalSize / (1024.0 * 1024.0);

                Debug.Log($"[AutoBuild] ✓ Build succeeded! Duration: {result.DurationSeconds:F1}s, Size: {result.BuildSizeMB:F1} MB");
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = $"Build failed: {report.summary.result}";

                int errorCount = report.summary.totalErrors;
                if (errorCount > 0)
                {
                    result.ErrorMessage += $" ({errorCount} errors)";
                }

                Debug.LogError($"[AutoBuild] ✗ {result.ErrorMessage}");
            }

            return result;
        }
    }
}
