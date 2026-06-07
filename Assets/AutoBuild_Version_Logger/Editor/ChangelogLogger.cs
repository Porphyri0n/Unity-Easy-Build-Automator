using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AutoBuild
{
    /// <summary>
    /// Logs build results in two formats:
    /// 1. InternalBuildHistory.json — internal JSON history
    /// 2. Changelog.md — markdown release notes in the build output folder
    /// </summary>
    public static class ChangelogLogger
    {
        private static readonly string LogDirectory = Path.Combine("Assets", "AutoBuild_Version_Logger", "Editor", "BuildLogs");
        private static readonly string JsonLogPath = Path.Combine(LogDirectory, "InternalBuildHistory.json");

        // ─────────────────────────────────────────────
        //  Data Structures
        // ─────────────────────────────────────────────

        [System.Serializable]
        public class BuildRecord
        {
            public string timestamp;
            public string version;
            public string platform;
            public double duration_seconds;
            public double build_size_mb;
            public string changelog;
            public string status;
        }

        [System.Serializable]
        public class BuildHistory
        {
            public List<BuildRecord> builds = new List<BuildRecord>();
        }

        // ─────────────────────────────────────────────
        //  JSON Log Operations
        // ─────────────────────────────────────────────

        /// <summary>
        /// Loads the internal JSON build history. Returns an empty BuildHistory if the file doesn't exist.
        /// </summary>
        public static BuildHistory LoadHistory()
        {
            if (!File.Exists(JsonLogPath))
                return new BuildHistory();

            try
            {
                string json = File.ReadAllText(JsonLogPath);
                BuildHistory history = JsonUtility.FromJson<BuildHistory>(json);
                return history ?? new BuildHistory();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AutoBuild] Failed to read JSON log: {ex.Message}. Creating new history.");
                return new BuildHistory();
            }
        }

        /// <summary>
        /// Appends a new build record to the JSON history and writes it to disk.
        /// </summary>
        public static void WriteJsonLog(BuildRecord record)
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            BuildHistory history = LoadHistory();
            history.builds.Add(record);

            string json = JsonUtility.ToJson(history, true);
            File.WriteAllText(JsonLogPath, json);

            Debug.Log($"[AutoBuild] Build record saved to JSON: {record.version} ({record.status})");
        }

        /// <summary>
        /// Helper method to create a new BuildRecord.
        /// </summary>
        public static BuildRecord CreateRecord(
            string version,
            string platform,
            double durationSeconds,
            double buildSizeMb,
            string changelog,
            string status)
        {
            return new BuildRecord
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                version = version,
                platform = platform,
                duration_seconds = Math.Round(durationSeconds, 1),
                build_size_mb = Math.Round(buildSizeMb, 1),
                changelog = changelog,
                status = status
            };
        }

        /// <summary>
        /// Returns the last successful build record, or null if none exists.
        /// </summary>
        public static BuildRecord GetLastSuccessfulBuild()
        {
            BuildHistory history = LoadHistory();

            for (int i = history.builds.Count - 1; i >= 0; i--)
            {
                if (history.builds[i].status == "Success")
                    return history.builds[i];
            }

            return null;
        }

        // ─────────────────────────────────────────────
        //  Markdown Changelog Operations
        // ─────────────────────────────────────────────

        /// <summary>
        /// Writes a Changelog.md to the build output folder.
        /// If the file already exists, prepends the new entry so the latest version is on top.
        /// </summary>
        public static void WriteMarkdownChangelog(string buildOutputDirectory, string version, string platform, string changelogText)
        {
            string changelogPath = Path.Combine(buildOutputDirectory, "Changelog.md");

            string formattedDate = DateTime.Now.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);

            // Convert changelog lines to bullet points
            string[] lines = changelogText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string bulletPoints = "";
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    if (trimmed.StartsWith("-") || trimmed.StartsWith("*"))
                        bulletPoints += trimmed + "\n";
                    else
                        bulletPoints += $"- {trimmed}\n";
                }
            }

            string newEntry =
$@"## Version: {version}
**Date:** {formattedDate}
**Platform:** {platform}

### Changes:
{bulletPoints}
---

";

            // If existing changelog exists, prepend the new entry
            string existingContent = "";
            if (File.Exists(changelogPath))
            {
                existingContent = File.ReadAllText(changelogPath);
            }

            string header = "# Changelog\n\n";
            string finalContent;

            if (existingContent.StartsWith("# Changelog"))
            {
                string afterHeader = existingContent.Substring(existingContent.IndexOf('\n') + 1).TrimStart('\n', '\r');
                finalContent = header + newEntry + afterHeader;
            }
            else if (!string.IsNullOrEmpty(existingContent))
            {
                finalContent = header + newEntry + existingContent;
            }
            else
            {
                finalContent = header + newEntry;
            }

            File.WriteAllText(changelogPath, finalContent);

            Debug.Log($"[AutoBuild] Changelog.md written: {changelogPath}");
        }
    }
}
