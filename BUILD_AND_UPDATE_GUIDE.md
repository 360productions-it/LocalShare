# 🚀 360 LocalShare - Build, Dynamic Versioning & Update Guide

This guide explains how to dynamically set and view application versions, build stable releases, generate setup installers, and host software updates for **360 LocalShare**.

---

## 📌 1. Dynamic Versioning Architecture

The application uses a **Single Source of Truth** for its version number:

1. **`Directory.Build.props`**: Solution-level property file holding the master version string (`<Version>1.2.0</Version>`).
2. **`AppVersionInfo.cs`**: Dynamically reads the assembly informational version at runtime (`AppVersionInfo.Version` and `AppVersionInfo.DisplayVersion`).
3. **UI Integration**:
   - **Sidebar Header**: Displays `LocalShare v1.2.0` in the top left brand section.
   - **Settings Dashboard**: Displays `v1.2.0` in the Software Updates & Maintenance card.
   - **Compiled Executable**: `LocalShare.App.exe` file version metadata is populated automatically.
   - **Update Checker**: Automatically compares dynamic local version against remote `latest_version.json`.

---

## 🛠️ 2. How to Dynamically Change Application Version

You can change the version dynamically using **either** of the following two simple methods:

### Method A: Via Command Line (Recommended)
Pass the `-Version` parameter when running the build script:
```powershell
powershell -File .\build-release.ps1 -Version 1.3.0
```
*This command automatically updates `Directory.Build.props`, builds `dist/publish/LocalShare.App.exe`, generates `dist/latest_version.json`, and compiles `dist/installer/360LocalShare_Setup_v1.3.0.exe`.*

### Method B: Manual File Edit
Edit `Directory.Build.props` in the root of the repository:
```xml
<Project>
  <PropertyGroup>
    <Version>1.3.0</Version>
    <AssemblyVersion>1.3.0.0</AssemblyVersion>
    <FileVersion>1.3.0.0</FileVersion>
    <InformationalVersion>1.3.0</InformationalVersion>
  </PropertyGroup>
</Project>
```
*Next time you run or compile the app, both the UI header, Settings dashboard, and compiled binaries will automatically update to `v1.3.0`.*

---

## 🛠️ 3. How to View Application Version

1. **In the Running App (UI)**:
   - Look at the sidebar top header under `LocalShare vX.Y.Z`.
   - Go to **⚙️ Settings & Profile** ➔ **🚀 Software Updates & Release Maintenance**.
2. **In Windows File Explorer**:
   - Right-click `LocalShare.App.exe` or `360LocalShare_Setup_vX.Y.Z.exe` ➔ **Properties** ➔ **Details** tab ➔ **File version / Product version**.
3. **In Code**:
   - Call `AppVersionInfo.Version` (e.g. `"1.2.0"`) or `AppVersionInfo.DisplayVersion` (e.g. `"v1.2.0"`).

---

## 📡 4. How to Publish Software Updates

When you release a new version (e.g. `v1.3.0`), follow these steps:

1. **Build the New Version**:
   ```powershell
   powershell -File .\build-release.ps1 -Version 1.3.0
   ```
2. **Host the Release Artifacts**:
   - Upload `dist/installer/360LocalShare_Setup_v1.3.0.exe` to **GitHub Releases** or your Web Server.
   - Deploy `dist/latest_version.json` to your update URL (`https://raw.githubusercontent.com/360productions-it/LocalShare/main/dist/latest_version.json`).

---

## 🖥️ 5. How the Auto-Updater Works for End-Users

1. **Check for Updates**:
   - Users open **360 LocalShare** ➔ Navigate to **⚙️ Settings & Profile**.
   - Under **🚀 Software Updates & Release Maintenance**, click **`🔍 Check for Updates`**.

2. **Automatic Download & Installation**:
   - If a newer version is detected, the app displays the **Changelog** and an **`📥 Download & Install Update Now`** button.
   - Clicking update downloads `360LocalShare_Setup_Update.exe` into Windows `%TEMP%` with real-time percentage progress.
   - The installer runs silently (`/SILENT /NORESTART`) in the background, updating application files and restarting 360 LocalShare automatically.

---

## ⚡ Summary of Commands

| Action | Command |
| :--- | :--- |
| **Run Dev Server** | `dotnet run --project src/LocalShare.App/LocalShare.App.csproj` |
| **Run Unit Tests** | `dotnet test LocalShare.slnx` |
| **Build Current Version Release** | `powershell -File .\build-release.ps1` |
| **Build & Change Version Dynamically** | `powershell -File .\build-release.ps1 -Version 1.3.0` |
