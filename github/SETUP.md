# Dangly — App Setup Guide (Windows & macOS)

Welcome to **Dangly**! This directory contains the standalone application packages, binaries, and 1-click automated setup scripts for both **Windows 10/11** and **macOS 14+ (Apple Silicon)**.

---

## 📂 Included Files & Packages

| File | Platform | Description |
|:---|:---|:---|
| [`Setup-Windows.bat`](file:///c:/Projects/Dangly/github/Setup-Windows.bat) | Windows | **1-Click Interactive Setup**: Installs Dangly, extracts assets, and creates Desktop & Start Menu shortcuts |
| [`Install-Dangly.ps1`](file:///c:/Projects/Dangly/github/Install-Dangly.ps1) | Windows | Automated PowerShell installer engine with optional arguments |
| [`Dangly-Windows.zip`](file:///c:/Projects/Dangly/github/Dangly-Windows.zip) | Windows | Complete standalone portable zip with all assets, sounds, and DLLs (~7.96 MB) |
| [`Dangly.exe`](file:///c:/Projects/Dangly/github/Dangly.exe) | Windows | Direct application executable |
| [`Setup-macOS.command`](file:///c:/Projects/Dangly/github/Setup-macOS.command) | macOS | **1-Click Interactive Setup**: Builds/extracts Dangly, clears Gatekeeper quarantine, and installs to `/Applications` |
| [`Build-On-Mac.command`](file:///c:/Projects/Dangly/github/Build-On-Mac.command) | macOS | 1-Click native compilation script for Apple Silicon |
| [`Dangly-macOS.zip`](file:///c:/Projects/Dangly/github/Dangly-macOS.zip) | macOS | Complete native Apple Silicon project bundle with assets (~4.44 MB) |
| [`SHA256SUMS.txt`](file:///c:/Projects/Dangly/github/SHA256SUMS.txt) | All | Official cryptographic checksums for package integrity verification |

---

## 🪟 Windows Setup (Windows 10 / 11)

### Prerequisites
- **.NET 10 Desktop Runtime (x64)**: Required to run the WPF application.  
  *If not already installed, download from [Microsoft .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).*

### Method 1: 1-Click Automated Setup (Recommended)
1. Double-click [`Setup-Windows.bat`](file:///c:/Projects/Dangly/github/Setup-Windows.bat).
2. Choose Option `[1]` to install Dangly to `%LOCALAPPDATA%\Dangly`.
3. The script automatically:
   - Verifies the .NET runtime.
   - Extracts [`Dangly-Windows.zip`](file:///c:/Projects/Dangly/github/Dangly-Windows.zip).
   - Generates Desktop and Start Menu shortcuts with the official Dangly icon.
   - Launches Dangly immediately.

### Method 2: Portable / Manual Run
1. Extract [`Dangly-Windows.zip`](file:///c:/Projects/Dangly/github/Dangly-Windows.zip) into any folder.
2. Double-click `Dangly.exe`.

> [!NOTE]
> **Windows SmartScreen**: If you see *"Windows protected your PC"* upon running `Dangly.exe` for the first time, click **More info** $\rightarrow$ **Run anyway**.

---

## 🍏 macOS Setup (macOS 14 Sonoma or later, Apple Silicon)

### Prerequisites
- **Apple Silicon Mac** (M1, M2, M3, M4 or later).
- **Xcode Command Line Tools** (for local compilation): Install by opening Terminal and running:
  ```bash
  xcode-select --install
  ```

### Method 1: 1-Click Automated Setup (Recommended)
1. Double-click [`Setup-macOS.command`](file:///c:/Projects/Dangly/github/Setup-macOS.command) in Finder.
2. If Terminal prompts for permission to run, allow it.
3. The script will:
   - Confirm Apple Silicon architecture.
   - Compile the native SwiftUI / AppKit bundle using `xcodebuild`.
   - Automatically strip the macOS Gatekeeper quarantine flag (`xattr -dr com.apple.quarantine`).
   - Prompt you to install `Dangly.app` directly to `/Applications`.
   - Launch Dangly on your screen.

### Method 2: 1-Click Build
1. Double-click [`Build-On-Mac.command`](file:///c:/Projects/Dangly/github/Build-On-Mac.command).
2. The script builds `Dangly.app` and launches it immediately.

### Method 3: Open in Xcode
1. Unzip [`Dangly-macOS.zip`](file:///c:/Projects/Dangly/github/Dangly-macOS.zip).
2. Open `macos/Hangly.xcodeproj` in Xcode.
3. Press **Cmd + R** to run.

> [!IMPORTANT]
> **Gatekeeper Warning**: If macOS displays *"Dangly cannot be opened because the developer cannot be verified"*, right-click `Dangly.app` $\rightarrow$ select **Open** $\rightarrow$ click **Open** once to grant permission.

---

## 🎮 Desktop Gestures & Controls

| Action | Control / Gesture |
|:---|:---|
| **Grab & Move** | Click and hold anywhere on the charm body |
| **Stretch Cord** | Drag the charm downward toward the bottom of the screen |
| **Snap & Recoil** | Release mouse to trigger elastic snap-back and swinging oscillations |
| **Throw / Fling** | Drag with velocity and release; momentum carries into the swing |
| **Open Settings** | Right-click the system tray icon or double-click the charm |
| **Sleep / Wake** | Automatically sleeps after 60 still frames (~0% CPU); wakes instantly on hover/touch |
