using UnityEditor;
using UnityEngine;
using System;

namespace AutoBuild
{
    /// <summary>
    /// Manages Semantic Versioning (Major.Minor.Patch) by reading/writing PlayerSettings.bundleVersion.
    /// </summary>
    public enum VersionBumpType
    {
        Patch,
        Minor,
        Major
    }

    [System.Serializable]
    public struct SemanticVersion : IEquatable<SemanticVersion>
    {
        public int Major;
        public int Minor;
        public int Patch;

        public SemanticVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Parses a version string in "v1.2.3" or "1.2.3" format.
        /// Returns false and a default v0.0.0 if parsing fails.
        /// </summary>
        public static bool TryParse(string versionString, out SemanticVersion result)
        {
            result = new SemanticVersion(0, 0, 0);

            if (string.IsNullOrEmpty(versionString))
                return false;

            string cleaned = versionString.TrimStart('v', 'V').Trim();
            string[] parts = cleaned.Split('.');

            if (parts.Length < 3)
                return false;

            if (int.TryParse(parts[0], out int major) &&
                int.TryParse(parts[1], out int minor) &&
                int.TryParse(parts[2], out int patch))
            {
                result = new SemanticVersion(major, minor, patch);
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"v{Major}.{Minor}.{Patch}";
        }

        /// <summary>
        /// Returns version without 'v' prefix. Used for writing to PlayerSettings.
        /// </summary>
        public string ToStringWithoutPrefix()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public bool Equals(SemanticVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override bool Equals(object obj)
        {
            return obj is SemanticVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch);
        }

        public static bool operator ==(SemanticVersion a, SemanticVersion b) => a.Equals(b);
        public static bool operator !=(SemanticVersion a, SemanticVersion b) => !a.Equals(b);
    }

    public static class VersionController
    {
        private static readonly SemanticVersion DefaultVersion = new SemanticVersion(0, 0, 1);

        /// <summary>
        /// Reads and parses the current version from PlayerSettings.bundleVersion.
        /// Falls back to v0.0.1 if parsing fails.
        /// </summary>
        public static SemanticVersion GetCurrentVersion()
        {
            string raw = PlayerSettings.bundleVersion;

            if (SemanticVersion.TryParse(raw, out SemanticVersion version))
                return version;

            Debug.LogWarning($"[AutoBuild] Could not parse current version: \"{raw}\". Using default {DefaultVersion}.");
            return DefaultVersion;
        }

        /// <summary>
        /// Calculates the next version based on the specified bump type.
        /// </summary>
        public static SemanticVersion GetNextVersion(SemanticVersion current, VersionBumpType bumpType)
        {
            switch (bumpType)
            {
                case VersionBumpType.Major:
                    return new SemanticVersion(current.Major + 1, 0, 0);

                case VersionBumpType.Minor:
                    return new SemanticVersion(current.Major, current.Minor + 1, 0);

                case VersionBumpType.Patch:
                default:
                    return new SemanticVersion(current.Major, current.Minor, current.Patch + 1);
            }
        }

        /// <summary>
        /// Writes the given version to PlayerSettings.bundleVersion.
        /// </summary>
        public static void ApplyVersion(SemanticVersion version)
        {
            PlayerSettings.bundleVersion = version.ToStringWithoutPrefix();
            Debug.Log($"[AutoBuild] Version updated: {version}");
        }
    }
}
