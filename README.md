# Unity Easy Build Automator 🛠️🚀

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg?style=for-the-badge&logo=unity)](https://unity.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS-brightgreen.svg?style=for-the-badge)](https://github.com/Porphyri0n/Unity-Easy-Build-Automator)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

An elegant, autonomous build and version management tool for Unity Editor. Stop wrestling with manual version updates and folder naming. **Unity Easy Build Automator** streamlines your release pipeline by automating build targets, semantic versioning increments, changelog creation, and build history logging in one click.

---

## ✨ Key Features

- **⚡ Bypass Default Build Prompts:** No more manual target selection, file naming, or folder creation. Builds are packaged in one click.
- **🔢 Semantic Versioning (SemVer):** Reads current `PlayerSettings.bundleVersion` and calculates the next logical release version (Major, Minor, or Patch) on demand.
- **📁 Structured Directory Naming:** Automatically creates builds under clean, organized folders:
  `[TargetDirectory]/[AppName]_v[Version]_[Platform]/[AppName]_v[Version]_[Platform].exe`
- **📝 Automated Changelogs:** Generates and places a clean `Changelog.md` file alongside the executable containing date, platform, and patch notes.
- **📊 Internal Build History (JSON):** Tracks and logs every build's metadata (`timestamp`, `duration`, `size`, `version`, `status`, `notes`) inside a system file (`InternalBuildHistory.json`).
- **🔍 Reveal in Explorer:** Opens the target folder in File Explorer or Finder immediately after a successful build.

---

## 📸 Interface Preview & Architecture

Access the tool via Unity's top menu: **`Tools > Auto Build Manager`**.

```
Developer Enters Changelog Notes
         │
         ▼
Clicks [Start Build] Button
         │
 ┌───────┴─────────────────────────────────────────┐
 │ Automatic Background Processes:                 │
 │                                                 │
 │  1. Increments Version (e.g. v1.0.1 -> v1.0.2)  │
 │  2. Creates Output Folder with Custom Name      │
 │  3. Executes Unity Build Pipeline               │
 │  4. Updates Internal JSON Build History         │
 │  5. Writes Changelog.md in Output Folder        │
 └───────┬─────────────────────────────────────────┘
         │
         ▼
Target Folder is Opened in Explorer (Reveal)
```

---

## 🛠️ Code Architecture

The tool is built using clean C# scripting tailored for `UnityEditor` APIs:

| Script | Purpose |
| :--- | :--- |
| **[`AutoBuildWindow.cs`](file:///Assets/AutoBuild_Version_Logger/Editor/AutoBuildWindow.cs)** | Main Editor Window UI. Handles user input, target path selection, semantic version increment options, and triggers builds. |
| **[`VersionController.cs`](file:///Assets/AutoBuild_Version_Logger/Editor/VersionController.cs)** | Reads and parses semantic versions. Calculates the next `Major`, `Minor`, or `Patch` numbers dynamically. |
| **[`BuildPipelineManager.cs`](file:///Assets/AutoBuild_Version_Logger/Editor/BuildPipelineManager.cs)** | Directly interfaces with Unity's `BuildPipeline.BuildPlayer()`. Configures scenes, active platforms, and build paths. |
| **[`ChangelogLogger.cs`](file:///Assets/AutoBuild_Version_Logger/Editor/ChangelogLogger.cs)** | Generates public markdown changelogs and appends rich build analytics to the system's history file. |

---

## 💾 Logs & Data Structure

### 1. Internal History Log (`InternalBuildHistory.json`)
Located in your editor files, keeping record of all past builds:
```json
{
  "builds": [
    {
      "timestamp": "2026-06-07T17:05:00",
      "version": "v1.2.4",
      "platform": "StandaloneWindows64",
      "duration_seconds": 145,
      "build_size_mb": 450.2,
      "changelog": "- Fixed multiplayer synchronization issues.\n- Added new visual effects to the main lobby.",
      "status": "Success"
    }
  ]
}
```

### 2. Output Changelog (`Changelog.md`)
Generated in the executable directory:
```markdown
## Version: v1.2.4
**Date:** 07 June 2026
**Platform:** StandaloneWindows64

### Changes & New Features:
- Fixed multiplayer synchronization issues.
- Added new visual effects to the main lobby.
```

---

## 🚀 Getting Started

### Installation
1. Clone or download this repository.
2. Drag and drop the `Assets/AutoBuild_Version_Logger` folder into your Unity project's `Assets` directory.
3. Done! The tool is ready to use under the **`Tools > Auto Build Manager`** menu.

### How to Build
1. Open the **`Auto Build Manager`** window.
2. Choose your Version Increment type (`Patch`, `Minor`, or `Major`).
3. Select your output folder (e.g. `C:/Builds/`).
4. Type your changes in the **Changelog Notes** text area.
5. Click **Start Build**.

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
