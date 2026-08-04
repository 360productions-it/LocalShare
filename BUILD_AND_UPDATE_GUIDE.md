# 🚀 360 LocalShare - Build & Software Update Guide

This guide explains how to build stable releases, generate setup installers, and host software updates for **360 LocalShare**.

---

## 📌 1. Architecture Overview

- **Single-File Executable**: Built as a self-contained `.NET 10` single-file app for `win-x64`. End-users do **not** need to install the .NET Runtime.
- **Inno Setup Installer**: Compiles `360LocalShare_Setup_v1.0.0.exe` with desktop shortcuts, uninstaller, and automatic Windows Defender firewall rules.
- **In-App Auto-Updater**: Built-in service (`UpdateService.cs`) checks a remote manifest (`latest_version.json`), downloads new setup installers in the background, and installs updates silently.

---

## 🛠️ 2. How to Build a Release & Installer

### Prerequisites
1. **PowerShell 5.1+** (included in Windows 10/11)
2. **Inno Setup 6** (Optional, but required to generate setup `.exe` installers):
   - Download free from: [Inno Setup Downloads](https://jrsoftware.org/isdl.php)
   - Install to standard directory (`C:\Program Files (x86)\Inno Setup 6\`)

### Step-by-Step Build Instructions

1. Open PowerShell terminal in the project root:
   ```powershell
   cd d:\dotnet\LocalShare
   ```

2. Run the automated release build script:
   ```powershell
   powershell -File .\build-release.ps1
   ```

3. **Output Files Created**:
   - `dist/publish/LocalShare.App.exe` ➔ Standalone single-file executable.
   - `dist/installer/360LocalShare_Setup_v1.0.0.exe` ➔ Full Windows installer.
   - `dist/latest_version.json` ➔ Update manifest file for hosting online.

---

## 📡 3. How to Publish Software Updates

When you create a new version (e.g. `v1.1.0`), follow these steps:

### Step 1: Bump Application Version
Open `src/LocalShare.App/LocalShare.App.csproj` and update version fields:
```xml
<FileVersion>1.1.0.0</FileVersion>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
```

### Step 2: Build New Release
Run the build script:
```powershell
powershell -File .\build-release.ps1
```

### Step 3: Host the Installer & Manifest
1. Upload `360LocalShare_Setup_v1.1.0.exe` to **GitHub Releases** or your Web Server.
2. Edit `dist/latest_version.json` with the new version and download URL:
   ```json
   {
     "version": "1.1.0",
     "releaseDate": "2026-08-04",
     "downloadUrl": "https://github.com/Antigravity/360-LocalShare/releases/download/v1.1.0/360LocalShare_Setup_v1.1.0.exe",
     "changelog": "Added dark mode enhancements, multi-peer streaming, and performance optimizations.",
     "sha256": "",
     "isMandatory": false
   }
   ```
3. Upload `latest_version.json` to your web server or repository main branch (`https://raw.githubusercontent.com/.../dist/latest_version.json`).

---

## 🖥️ 4. How the Auto-Updater Works for End-Users

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
| **Build Full Release & Installer** | `powershell -File .\build-release.ps1` |
