using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;

namespace AutoBuild
{
    /// <summary>
    /// AutoBuild & Version Logger main editor window.
    /// Intercepts Unity's Build button via BuildPlayerWindow.RegisterBuildPlayerHandler.
    /// Also accessible from Tools → Auto Build Manager.
    /// </summary>
    [InitializeOnLoad]
    public class AutoBuildWindow : EditorWindow
    {
        // ─────────────────────────────────────────────
        //  Build Button Intercept
        // ─────────────────────────────────────────────

        static AutoBuildWindow()
        {
            // Override how build options are obtained — prevents Unity's default file save dialog
            BuildPlayerWindow.RegisterGetBuildPlayerOptionsHandler(OnGetBuildPlayerOptions);

            // Override the actual build execution — opens our window instead
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildButtonClicked);
        }

        /// <summary>
        /// Called when Unity tries to gather build options (normally shows a file save dialog).
        /// We return the options as-is, skipping the file dialog entirely.
        /// Our AutoBuild window handles output directory selection.
        /// </summary>
        private static BuildPlayerOptions OnGetBuildPlayerOptions(BuildPlayerOptions defaultOptions)
        {
            // Return options without showing any file dialog
            return defaultOptions;
        }

        /// <summary>
        /// Called when the user clicks "Build" or "Build And Run" in File → Build Settings.
        /// Opens our AutoBuild window instead of proceeding with the default build.
        /// </summary>
        private static void OnBuildButtonClicked(BuildPlayerOptions options)
        {
            AutoBuildWindow window = GetWindow<AutoBuildWindow>();
            window.titleContent = new GUIContent("Auto Build Manager", EditorGUIUtility.IconContent("BuildSettings.Editor.Small").image);
            window.minSize = new Vector2(420, 580);
            window.Show();
            window.Focus();

            Debug.Log("[AutoBuild] Build button intercepted. AutoBuild window opened.");
        }

        // ─────────────────────────────────────────────
        //  Fields
        // ─────────────────────────────────────────────

        private string changelogText = "";
        private string outputDirectory = "C:/Builds/";
        private VersionBumpType bumpType = VersionBumpType.Patch;
        private Vector2 changelogScrollPos;
        private Vector2 windowScrollPos;
        private bool isBuilding = false;

        // Cached version info
        private SemanticVersion currentVersion;
        private SemanticVersion nextVersion;

        // Style references (lazy-initialized in OnGUI)
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle infoBoxStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;

        // ─────────────────────────────────────────────
        //  Menu & Window
        // ─────────────────────────────────────────────

        [MenuItem("Tools/Auto Build Manager")]
        public static void ShowWindow()
        {
            AutoBuildWindow window = GetWindow<AutoBuildWindow>();
            window.titleContent = new GUIContent("Auto Build Manager", EditorGUIUtility.IconContent("BuildSettings.Editor.Small").image);
            window.minSize = new Vector2(420, 580);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshVersionInfo();
        }

        private void OnFocus()
        {
            RefreshVersionInfo();
        }

        // ─────────────────────────────────────────────
        //  Version Info
        // ─────────────────────────────────────────────

        private void RefreshVersionInfo()
        {
            currentVersion = VersionController.GetCurrentVersion();
            nextVersion = VersionController.GetNextVersion(currentVersion, bumpType);
        }

        // ─────────────────────────────────────────────
        //  Styles
        // ─────────────────────────────────────────────

        private void InitStyles()
        {
            if (stylesInitialized)
                return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                fixedHeight = 36,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 10, 10),
                clipping = TextClipping.Overflow
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fixedHeight = 24,
                padding = new RectOffset(2, 0, 4, 4),
                margin = new RectOffset(0, 0, 6, 2),
                clipping = TextClipping.Overflow,
                wordWrap = false
            };

            infoBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                padding = new RectOffset(10, 10, 8, 8),
                richText = true
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40,
                padding = new RectOffset(20, 20, 8, 8)
            };

            stylesInitialized = true;
        }

        // ─────────────────────────────────────────────
        //  UI Drawing
        // ─────────────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();

            windowScrollPos = EditorGUILayout.BeginScrollView(windowScrollPos);

            // Add horizontal padding to entire window content
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.BeginVertical();

            // ── Header ──
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("⚙  AutoBuild & Version Logger", headerStyle, GUILayout.Height(36));
            DrawSeparator();

            // ── Version Info Section ──
            EditorGUILayout.LabelField("Version Info", subHeaderStyle, GUILayout.Height(24));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Current version (read-only)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Current Version", currentVersion.ToString());
                EditorGUI.EndDisabledGroup();

                // Bump type selector
                EditorGUI.BeginChangeCheck();
                bumpType = (VersionBumpType)EditorGUILayout.EnumPopup("Bump Type", bumpType);
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshVersionInfo();
                }

                // Next version (read-only)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Next Version", nextVersion.ToString());
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // ── Platform Info ──
            EditorGUILayout.LabelField("Platform Info", subHeaderStyle, GUILayout.Height(24));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                var target = BuildPipelineManager.GetActiveBuildTarget();
                string platformName = BuildPipelineManager.GetPlatformDisplayName(target);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Active Platform", platformName);
                EditorGUILayout.TextField("Build Target", target.ToString());
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // ── Change Notes ──
            EditorGUILayout.LabelField("Change Notes", subHeaderStyle, GUILayout.Height(24));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Describe the changes made in this version:", EditorStyles.miniLabel);
                changelogScrollPos = EditorGUILayout.BeginScrollView(changelogScrollPos, GUILayout.Height(120));
                changelogText = EditorGUILayout.TextArea(changelogText, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // ── Output Directory ──
            EditorGUILayout.LabelField("Output Directory", subHeaderStyle, GUILayout.Height(24));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    outputDirectory = EditorGUILayout.TextField(outputDirectory);

                    if (GUILayout.Button("Browse", GUILayout.Width(70)))
                    {
                        string selected = EditorUtility.OpenFolderPanel("Select Build Output Folder", outputDirectory, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            outputDirectory = selected;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Output path preview
                string previewPath = BuildPipelineManager.GenerateFullBuildPath(
                    outputDirectory,
                    BuildPipelineManager.GetProductName(),
                    nextVersion.ToString(),
                    BuildPipelineManager.GetActiveBuildTarget());

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Output Path Preview:", EditorStyles.miniLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(previewPath);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(12);

            // ── Start Build Button ──
            EditorGUI.BeginDisabledGroup(isBuilding);
            {
                Color originalBg = GUI.backgroundColor;
                GUI.backgroundColor = isBuilding ? Color.gray : new Color(0.2f, 0.8f, 0.4f);

                string buttonLabel = isBuilding ? "⏳  Building..." : "▶  Start Build";

                if (GUILayout.Button(buttonLabel, buttonStyle))
                {
                    ExecuteBuild();
                }

                GUI.backgroundColor = originalBg;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);

            // ── Last Build Info ──
            DrawLastBuildInfo();

            EditorGUILayout.Space(8);

            // Close horizontal padding
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────
        //  Last Build Info
        // ─────────────────────────────────────────────

        private void DrawLastBuildInfo()
        {
            var lastBuild = ChangelogLogger.GetLastSuccessfulBuild();

            if (lastBuild == null)
                return;

            DrawSeparator();
            EditorGUILayout.LabelField("Last Successful Build", subHeaderStyle, GUILayout.Height(24));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"<b>Version:</b> {lastBuild.version}    |    <b>Platform:</b> {lastBuild.platform}", infoBoxStyle);
                EditorGUILayout.LabelField($"<b>Date:</b> {lastBuild.timestamp}    |    <b>Duration:</b> {lastBuild.duration_seconds}s    |    <b>Size:</b> {lastBuild.build_size_mb} MB", infoBoxStyle);

                if (!string.IsNullOrEmpty(lastBuild.changelog))
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Notes:", EditorStyles.miniLabel);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextArea(lastBuild.changelog, GUILayout.Height(40));
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        //  Build Execution
        // ─────────────────────────────────────────────

        private void ExecuteBuild()
        {
            // ── Validation checks ──
            if (string.IsNullOrWhiteSpace(changelogText))
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Empty Change Notes",
                    "No change notes were entered. Do you want to continue anyway?",
                    "Yes, continue",
                    "Cancel");

                if (!proceed) return;
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                EditorUtility.DisplayDialog("Error", "Please select an output directory.", "OK");
                return;
            }

            string[] scenes = BuildPipelineManager.GetEnabledScenes();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "No enabled scenes found in Build Settings.\nPlease add at least one scene via File → Build Settings.",
                    "OK");
                return;
            }

            // ── Final confirmation ──
            string confirmMsg = $"The build will start with the following settings:\n\n" +
                                $"Version: {currentVersion} → {nextVersion}\n" +
                                $"Platform: {BuildPipelineManager.GetPlatformDisplayName(BuildPipelineManager.GetActiveBuildTarget())}\n" +
                                $"Output: {outputDirectory}\n\n" +
                                $"Do you want to proceed?";

            if (!EditorUtility.DisplayDialog("Confirm Build", confirmMsg, "Start Build", "Cancel"))
                return;

            // ── Start build ──
            isBuilding = true;

            try
            {
                // 1. Update version
                VersionController.ApplyVersion(nextVersion);

                // 2. Run build
                BuildPipelineManager.BuildResult result = BuildPipelineManager.BuildGame(
                    outputDirectory,
                    nextVersion.ToString());

                // 3. Write log records
                string statusText = result.Success ? "Success" : "Failed";
                string platformName = BuildPipelineManager.GetPlatformDisplayName(BuildPipelineManager.GetActiveBuildTarget());

                ChangelogLogger.BuildRecord record = ChangelogLogger.CreateRecord(
                    nextVersion.ToString(),
                    platformName,
                    result.DurationSeconds,
                    result.BuildSizeMB,
                    changelogText,
                    statusText);

                ChangelogLogger.WriteJsonLog(record);

                // 4. Write Markdown changelog (only on success)
                if (result.Success)
                {
                    ChangelogLogger.WriteMarkdownChangelog(
                        result.OutputDirectory,
                        nextVersion.ToString(),
                        platformName,
                        changelogText);

                    // 5. Open output folder
                    EditorUtility.RevealInFinder(result.OutputPath);

                    // Success dialog
                    EditorUtility.DisplayDialog(
                        "Build Succeeded ✓",
                        $"Version {nextVersion} built successfully!\n\n" +
                        $"Duration: {result.DurationSeconds:F1} seconds\n" +
                        $"Size: {result.BuildSizeMB:F1} MB\n" +
                        $"Location: {result.OutputDirectory}",
                        "OK");

                    // Clear changelog and refresh version
                    changelogText = "";
                    RefreshVersionInfo();
                }
                else
                {
                    // Failed — roll back version
                    VersionController.ApplyVersion(currentVersion);

                    EditorUtility.DisplayDialog(
                        "Build Failed ✗",
                        $"An error occurred during the build:\n\n{result.ErrorMessage}\n\n" +
                        $"Version number has been rolled back to: {currentVersion}",
                        "OK");

                    RefreshVersionInfo();
                }
            }
            catch (System.Exception ex)
            {
                // Unexpected error — roll back version
                VersionController.ApplyVersion(currentVersion);
                RefreshVersionInfo();

                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "Unexpected Error",
                    $"An unexpected error occurred during the build:\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                isBuilding = false;
                Repaint();
            }
        }

        // ─────────────────────────────────────────────
        //  UI Helpers
        // ─────────────────────────────────────────────

        private void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(4);
        }
    }
}
