# 🚀 360 LocalShare - Build, Dynamic Versioning & Update Guide

This guide explains how to dynamically set versions, build single-file releases, compile setup installers, and **automatically publish releases to GitHub**.

---

## 📌 1. Dynamic Versioning Architecture

The application uses a **Single Source of Truth** for its version number:

1. **`Directory.Build.props`**: Solution-level property file holding the master version string (`<Version>1.4.0</Version>`).
2. **`AppVersionInfo.cs`**: Dynamically reads assembly informational version at runtime (`AppVersionInfo.Version` and `AppVersionInfo.DisplayVersion`).
3. **UI Integration**:
   - **Sidebar Header**: Displays `LocalShare v1.4.0` in the top left brand section.
   - **Settings Dashboard**: Displays `v1.4.0` in the Software Updates & Maintenance card.
   - **Compiled Executable**: `LocalShare.App.exe` file version metadata is populated automatically.
   - **Update Checker**: Automatically compares dynamic local version against remote `latest_version.json`.

---

## 🛠️ 2. How to Build & Publish Releases Automatically

You can build and publish a release (e.g. `v1.4.0`) using **any** of the following methods:

### Method 1: Using GitHub Personal Access Token (PAT) [Recommended]
Run the build script with your GitHub Personal Access Token (PAT):
```powershell
powershell -File .\build-release.ps1 -Version 1.4.0 -GitHubToken "ghp_yourPersonalAccessTokenHere"
```
*This command automatically:*
1. Updates `Directory.Build.props` to `v1.4.0`.
2. Compiles `dist/publish/LocalShare.App.exe` single-file executable.
3. Generates `dist/latest_version.json` update manifest.
4. Compiles `dist/installer/360LocalShare_Setup_v1.4.0.exe` using Inno Setup.
5. **Creates GitHub Release `v1.4.0` and uploads `360LocalShare_Setup_v1.4.0.exe` directly to GitHub Releases!**

---

### Method 2: Using GitHub CLI (`gh`)
If you have **GitHub CLI** installed (`winget install --id GitHub.cli`):
```powershell
# 1. Build installer
powershell -File .\build-release.ps1 -Version 1.4.0

# 2. Upload to GitHub Releases
powershell -File .\publish-github-release.ps1 -Version 1.4.0
```

---

### Method 3: Manual Upload via Web Browser
If you prefer manual upload:
1. Build installer: `powershell -File .\build-release.ps1 -Version 1.4.0`
2. Open: [360productions-it/LocalShare New Release](https://github.com/360productions-it/LocalShare/releases/new)
3. Set Tag: `v1.4.0`
4. Attach File: `dist\installer\360LocalShare_Setup_v1.4.0.exe`
5. Publish Release!

---

## 🖥️ 3. How the Auto-Updater Works for End-Users

1. **Check for Updates**:
   - Users open **360 LocalShare** ➔ Navigate to **⚙️ Settings & Profile**.
   - Under **🚀 Software Updates & Release Maintenance**, click **`🔍 Check for Updates`**.

2. **Automatic Download & Installation**:
   - If a newer version is detected, the app displays the **Changelog** and an **`📥 Download & Install Update Now`** button.
   - Clicking update downloads `360LocalShare_Setup_v1.4.0.exe` into Windows `%TEMP%` with real-time percentage progress.
   - The installer runs silently (`/SILENT /NORESTART`) in the background, updating application files and restarting 360 LocalShare automatically.

---

## 🛡️ 4. Windows Defender & Anti-Virus Compatibility Guide

To ensure smooth installation without Windows Defender or SmartScreen false-positive blocks:

1. **Clean Installer Architecture**:
   - **No Hidden Netsh Process Executions**: The installer does not invoke silent `netsh` commands without elevation.
   - **Standard Packing**: Inno Setup uses `lzma2/max` instead of `lzma2/ultra64` to prevent generic heuristic detection.
   - **Explicit Manifest**: `LocalShare.App` embeds an explicit `app.manifest` declaring `asInvoker` execution level.

2. **Code Signing (Recommended for Production)**:
   Sign the compiled installer (`.exe`) with a Code Signing Certificate using `signtool.exe`:
   ```powershell
   signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /f "YourCert.pfx" /p "YourPassword" "dist\installer\360LocalShare_Setup_v1.4.0.exe"
   ```

3. **Submitting False Positives to Microsoft**:
   If Microsoft SmartScreen displays an "Unknown Publisher" warning on newly compiled binaries:
   - Submit your compiled setup binary to [Microsoft Security Intelligence Sample Submission](https://www.microsoft.com/wdsi/filesubmission).
   - Select **Software Developer** and submit as **Incorrectly Detected (False Positive)**. Microsoft typically whitelists clean submissions within a few hours.

---

## ⚡ Summary of Commands

| Action | Command |
| :--- | :--- |
| **Run Dev Server** | `dotnet run --project src/LocalShare.App/LocalShare.App.csproj` |
| **Run Unit Tests** | `dotnet test LocalShare.slnx` |
| **Build & Publish Release Automatically** | `powershell -File .\build-release.ps1 -Version 1.4.0 -GitHubToken "YOUR_TOKEN"` |
| **Upload Existing Installer Artifact** | `powershell -File .\publish-github-release.ps1 -Version 1.4.0 -GitHubToken "YOUR_TOKEN"` |

