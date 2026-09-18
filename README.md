<div align="center">

<img src="Assets/Branding/Dangly-Banner.png" width="600" alt="Dangly — Things shouldn't just sit there.">

<br>

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%20%7C%20macOS%2014%2B-black?style=for-the-badge&logo=windows&logoColor=white)](#prerequisites)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Swift](https://img.shields.io/badge/Swift-6.0-F05138?style=for-the-badge&logo=swift&logoColor=white)](https://swift.org)
[![Physics](https://img.shields.io/badge/Physics-240%20Hz%20Verlet%20%2B%20Elastic%20Recoil-00A67E?style=for-the-badge)](#the-physics-engine)

<br>

<img src="Assets/Screenshots/overlay-daruma.png" width="460" alt="Daruma charm hanging from a beaded cord on the desktop">

</div>

---

## Overview

**Dangly** puts a small, responsive, tactile ornament on your screen that obeys real physical laws.

The cord is a twenty-segment Verlet rope solved at a fixed 240 Hz timestep. Beads threaded above the charm are their own simulated particles, riding the curve of the cord. Grab the charm, pull it down to stretch the cord across your screen, swing it, or fling it across the desktop — the momentum you give it is the momentum it keeps.

When you let go of a stretched cord, stored elastic strain snaps it back upward and triggers persistent swinging oscillations before settling gently to rest. When nothing is moving, the simulation sleeps, consuming virtually 0% CPU and zero GPU overhead.

---

## ✨ Features

### 🪢 Advanced 240 Hz Verlet Physics Engine
- **Dynamic Stretch & Recoil**: Grab the charm and pull it all the way down to the bottom of your display. As distance grows, segment rest length dynamically expands. Upon release, stored elastic potential energy snaps the rope upward and translates into lively swinging oscillations.
- **Fixed 240 Hz Timestep**: Rope dynamics behave identically across 60 Hz, 120 Hz, 144 Hz, and variable refresh rate (VRR) monitors.
- **Gauss-Seidel Distance Constraints**: Adaptive pass budget for distance relaxation. Links maintain structural integrity and natural curvature.
- **Independent Simulated Beads**: Beads are simulated as individual Verlet particles tethered to resting offsets and projected along the quadratic rope spline with non-penetration separation.
- **Zero-Overhead Sleep State Machine**: Automatically sleeps after 60 still frames (~0% CPU). Instantly wakes on cursor touch or drag.

### 🧿 16 Built-in Charms
- **11 Cultural Collection Charms**: Hand-drawn protective and symbolic charms from around the world:
  - *Nazar Boncuğu* (Evil eye protector)
  - *Hamsa* (Hand of protection)
  - *Drishti Bommai* (Traditional ward against negative energy)
  - *Daruma* (Japanese talisman of perseverance)
  - *Maneki-neko* (Beckoning lucky cat)
  - *Ghanta* (Sacred temple bell)
  - *Scarab* (Ancient Egyptian amulet of renewal)
  - *Pánchang Jié* (Chinese endless mystic knot)
  - *Nimbu-mirchi* (Lemon and chili charm)
  - *Horseshoe* (Classic lucky talisman)
  - *Himmeli* (Nordic geometric mobile)
- **5 Geometric Classics**: Circle, Star, Heart, Diamond, and Retro Camera.

### 🎨 Minimalist Dark Settings & Studio
- **Sleek Customization Window**: Redesigned modern dark UI (`#0F0F11` dark surface, `#18181B` card containers, subtle borders, and smooth hover animations).
- **Charm Studio (Image Drop)**: Drag and drop any `.png`, `.jpg`, `.jpeg`, `.webp`, or `.bmp` file directly onto the charm or studio.
- **Smart Background Cutout**: Integrated corner flood-fill and luminance-based background isolation with live tolerance slider.
- **Auto-Palette Tinting**: Extracted primary, secondary, and highlight colors automatically tint the cord and bead scheme.

### 🔔 Physical Audio Synthesis & Custom Sounds
- **On-Device Synthesizer**: Generates striking audio via additive synthesis with percussive attack filtering at 44.1 kHz:
  - **Wood**: Resonant acoustic knock with filtered attack noise.
  - **Bell**: Clear chime with long-decay harmonic partials.
  - **Glass**: High crystal ping.
  - **Metal**: Metallic strike with inharmonic partials.
  - **Soft**: Cushioned fabric/felt tap.
- **Custom `.wav` Audio**: Attach custom sound files to any custom charm in Charm Studio.

### 🔒 100% Private & Native
- **Zero Telemetry**: No analytics, no crash reporting, no external requests.
- **Native Stacks**: High-performance WPF on Windows (.NET 10) and native SwiftUI/AppKit on macOS (Swift 6).

---

## 📦 Download & Quick Start

Pre-packaged releases are located in the [dist/](file:///c:/Projects/Hangly/dist/) directory:

### Windows
- **Direct Executable**: [dist/windows/Dangly.exe](file:///c:/Projects/Hangly/dist/windows/Dangly.exe)  
  *Double-click to run immediately (Windows 10/11).*
- **Portable Zip**: [dist/Dangly-Windows.zip](file:///c:/Projects/Hangly/dist/Dangly-Windows.zip) (~7.96 MB)  
  *Extract anywhere and run `Dangly.exe`.*

### macOS
- **1-Click Build Package**: [dist/Dangly-macOS.zip](file:///c:/Projects/Hangly/dist/Dangly-macOS.zip) (~11.56 MB)  
  *Unzip on Mac and double-click `Build-On-Mac.command` (or open in Xcode and hit Run).*
- **GitHub Actions (Apple Silicon)**:  
  *Pushing to GitHub triggers automated compilation on Apple Silicon runners (`macos-15`), publishing ready-to-use `.app` and `.dmg` downloads under the **Actions** tab.*

---

## 🎮 Controls & Gestures

| Action | Gesture / Control |
|---|---|
| **Grab & Move** | Click and hold anywhere on the charm body |
| **Stretch Cord** | Drag the charm downward toward the bottom of the screen |
| **Snap & Oscillate** | Release mouse after dragging to trigger elastic snap-back and swinging recoil |
| **Throw / Fling** | Drag with velocity and release; momentum carries into the swing |
| **Open Settings** | Double-click charm body, or right-click tray/menu bar icon $\rightarrow$ **Settings…** |
| **Charm Studio** | Drop an image directly on the charm, or right-click tray $\rightarrow$ **✦ Open Charm Studio…** |
| **Reset Rope** | Right-click tray icon $\rightarrow$ **Reset Rope** |

---

## ⚙️ The Physics Engine

| Property | Specification |
|---|---|
| **Integration** | Position-based Verlet (position & previous position; velocity is implicit) |
| **Timestep** | Fixed 240 Hz ($dt = 1/240 \text{ s}$), decoupled from display refresh rate |
| **Segments** | 20 rope segments with 21 nodes |
| **Stretch Dynamics** | Dynamic rest length expansion on downward drag; exponential spring damping on release |
| **Constraints** | Gauss-Seidel distance relaxation with dynamic stretch limits |
| **Spline Smoothing**| Quadratic Bezier spline sampled through node midpoints |
| **Bead Dynamics** | Tethered sliding with non-penetration separation passes |
| **Performance** | Suspends when still; ~0.02 ms solver execution during active motion |

---

## 🛠️ Building From Source

### Prerequisites
- **Windows**: Windows 10 or 11, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **macOS**: macOS 14 Sonoma or later, Apple Silicon, Xcode 16+

### Windows (.NET 10 / WPF)

```bash
# Clone the repository
git clone https://github.com/praveeneyyy/Dangly.git
cd Dangly

# Run the app
dotnet run --project windows/Hangly.Windows/Hangly.Windows.csproj

# Run unit tests
dotnet test windows/Hangly.Windows.sln

# Publish release binary
dotnet publish windows/Hangly.Windows/Hangly.Windows.csproj -c Release -o dist/windows
```

### macOS (Swift 6 / SwiftUI)

```bash
# Open in Xcode
cd macos && open Hangly.xcodeproj

# Or build via command-line / script
./Scripts/build-dmg.sh Production
```

---

## 📁 Repository Structure

```
├── Assets/
│   ├── Branding/          # Master artwork, banners, logo, and app icons
│   ├── Charms/            # 16 hand-drawn collection SVGs
│   ├── Icons/             # Multi-resolution icon sets (.ico, .png, .appiconset)
│   └── Screenshots/       # Visual showcase assets
├── dist/                  # Packaged distribution binaries & zips
│   ├── windows/           # Compiled Windows Release build (Dangly.exe)
│   ├── Dangly-Windows.zip # Portable Windows zip package
│   └── Dangly-macOS.zip   # Full macOS project bundle with 1-click build script
├── Docs/                  # Consolidated project documentation
│   ├── Architecture.md    # Dual-platform architecture comparison
│   ├── CHANGELOG.md       # Release history and updates
│   ├── CONTRIBUTING.md    # Contribution guidelines and workflow
│   ├── Charm-System.md    # Charm rendering and catalogue specs
│   ├── DISTRIBUTION.md    # Build and notarization guides
│   ├── FAQ.md             # Frequently asked questions
│   ├── Physics.md         # Overview of rope dynamics
│   ├── PHYSICS_SPEC.md    # Mathematical specification for the 240 Hz solver
│   ├── SECURITY.md        # Security policies and disclosures
│   └── SVG-Import.md      # Vector import and path extraction specs
├── windows/
│   ├── Hangly.Windows/    # Native Windows WPF application (.NET 10)
│   │   ├── Models/        # Charms, palettes, settings data models
│   │   ├── Physics/       # 240 Hz Verlet solver, RopeCurve, dynamic stretch
│   │   ├── Rendering/     # Transparent DirectX/WPF overlay canvas
│   │   ├── Services/      # Audio synthesizer, image processor, stores
│   │   └── Views/         # Minimalist SettingsWindow, Overlay, Charm Studio
│   └── Hangly.Windows.Tests/ # Test suite covering physics, beads, audio, and recoil
└── macos/
    ├── Hangly/            # Native macOS app (SwiftUI & AppKit)
    │   ├── Physics/       # Swift 6 Verlet solver with drag stretch & recoil
    │   ├── Services/      # Full screen-height overlay window controller
    │   └── Views/         # SwiftUI interface & views
    └── Tests/             # Swift Testing suite
```

---

## 📄 License & Credits

Built with precision by the Dangly engineering team. All documentation and specifications can be found in the [Docs/](Docs/) folder.
