<div align="center">

<img src="Assets/Icons/hangly-icon-256.png" width="128" alt="Dangly Icon">

# Dangly

**A physics-simulated hanging charm ornament for your desktop.**

Nudge it and it swings, carries momentum, and settles with real physics — not a looping animation.

<br>

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%20%7C%20macOS%2014%2B-black?style=for-the-badge&logo=windows&logoColor=white)](#requirements)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Swift](https://img.shields.io/badge/Swift-6.0-F05138?style=for-the-badge&logo=swift&logoColor=white)](https://swift.org)
[![Physics](https://img.shields.io/badge/Physics-240%20Hz%20Verlet-00A67E?style=for-the-badge)](#the-physics-engine)

<br>

<img src="Assets/Screenshots/overlay-daruma.png" width="460" alt="Daruma charm hanging from a beaded cord on the desktop">

</div>

---

## Overview

**Dangly** puts a small, responsive, tactile ornament on your screen that obeys real physical laws. 

The cord is a twenty-segment Verlet rope solved at a fixed 240 Hz timestep. The beads threaded above the charm are their own simulated particles, riding the curve of the cord. Grab the charm, swing it, or fling it across the desktop — the momentum you give it is the momentum it keeps.

When nothing is moving, the simulation goes to sleep, consuming virtually 0% CPU and zero GPU overhead.

---

## Features

### 🪢 Real Verlet Physics Engine
- **Fixed 240 Hz Timestep**: Rope dynamics behave consistently across 60 Hz, 120 Hz, 144 Hz, and variable refresh rate displays.
- **Gauss-Seidel Distance Constraints**: Segment length relaxation with an adaptive pass budget. Links never stretch past 1.02× rest length.
- **Independent Bead Simulation**: Beads are simulated as individual Verlet particles tethered to their resting offsets and constrained along the quadratic rope spline.
- **Sleep / Wake State Machine**: Automatically throttles and suspends updates after 60 still frames. Instantly wakes upon cursor interaction.

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

### 🖼️ Charm Studio (Custom Image Uploads)
- **Drop Any Image**: Drag and drop any `.png`, `.jpg`, `.jpeg`, `.webp`, or `.bmp` file directly onto the desktop charm or Charm Studio.
- **Smart Background Cutout**: Integrated corner flood-fill and automated luminance background isolation with adjustable tolerance.
- **Physics Customization**: Fine-tune simulated mass (2.0 – 4.5) and knot inset (where the rope connects to your image).
- **Auto-Palette Tinting**: Extracted primary, secondary, and highlight colors automatically tint the cord.

### 🔔 Physical Audio Synthesis & Custom Sounds
- **On-Device Harmonic Synthesizer**: Generates striking audio via additive synthesis with percussive attack filtering at 44.1 kHz:
  - **Wood**: Resonant acoustic knock with filtered attack noise.
  - **Bell**: Clear chime with long-decay harmonic partials.
  - **Glass**: High crystal ping.
  - **Metal**: Metallic strike with inharmonic partials.
  - **Soft**: Cushioned fabric/felt tap.
- **Custom `.wav` Audio Support**: Attach your own custom sound files to any custom charm in Charm Studio.

### 🔒 100% Private & Native
- **Zero Telemetry**: No analytics, no crash reports, no network calls.
- **Native Implementation**: High-performance WPF on Windows (.NET 10) and native SwiftUI/AppKit on macOS.

---

## Desktop Controls & Shortcuts

| Action | Control |
|---|---|
| **Grab & Move** | Click and hold anywhere on the charm body |
| **Throw / Fling** | Drag with velocity and release; momentum carries into the swing |
| **Open Settings** | Double-click the charm body, or right-click the system tray / menu bar icon $\rightarrow$ **Settings…** |
| **Charm Studio** | Drop an image directly on the charm, or right-click tray icon $\rightarrow$ **✦ Open Charm Studio…** |
| **Reset Rope Position** | Right-click tray icon $\rightarrow$ **Reset Rope** |

---

## The Physics Engine

| Property | Implementation |
|---|---|
| **Integration** | Position-based Verlet (position & previous position; velocity is implicit) |
| **Timestep** | Fixed 240 Hz ($dt = 1/240 \text{ s}$), decoupled from monitor refresh rate |
| **Segments** | 20 rope segments with 21 nodes |
| **Constraints** | Gauss-Seidel distance constraint relaxation |
| **Curve Smoothing** | Quadratic Bezier spline sampled through node midpoints |
| **Bead Projection** | Dual-pass tethered sliding with non-penetration separation passes |
| **Power Consumption** | Suspends when still; ~0.02 ms compute time during motion |

---

## Building and Running

### Prerequisites
- **Windows**: Windows 10 or 11, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **macOS**: macOS 14 Sonoma or later, Apple Silicon, Xcode 16+

### Windows (.NET 10 / WPF)

1. Clone the repository:
   ```bash
   git clone <your-repo-url>
   cd Dangly
   ```

2. Build and run:
   ```bash
   dotnet run --project windows/Hangly.Windows/Hangly.Windows.csproj
   ```

3. Run automated tests:
   ```bash
   dotnet test windows/Hangly.Windows.sln
   ```

4. Publish self-contained release:
   ```bash
   dotnet publish windows/Hangly.Windows/Hangly.Windows.csproj -c Release -r win-x64 --self-contained
   ```

### macOS (Swift / SwiftUI)

1. Open the project:
   ```bash
   cd macos && open Hangly.xcodeproj
   ```

2. Build and test via command line:
   ```bash
   xcodebuild -project macos/Hangly.xcodeproj -scheme Hangly -configuration Debug build
   xcodebuild -project macos/Hangly.xcodeproj -scheme Hangly -configuration Debug test
   ```

---

## Repository Structure

```
├── Assets/
│   ├── Charms/            # Hand-drawn collection SVGs
│   ├── Icons/             # Application icons
│   └── Screenshots/       # Visual showcase assets
├── windows/
│   ├── Hangly.Windows/    # Native Windows WPF application (.NET 10)
│   │   ├── Models/        # Charms, palettes, settings data models
│   │   ├── Physics/       # 240 Hz Verlet solver, RopeCurve, RopeBead
│   │   ├── Rendering/     # Transparent DirectX/WPF overlay canvas
│   │   ├── Services/      # Audio synthesizer, image processor, stores
│   │   └── Views/         # Overlay, Settings, and Charm Studio windows
│   └── Hangly.Windows.Tests/ # Test suite covering physics, beads, audio, and SVG splitting
├── macos/
│   ├── Hangly/            # Native macOS app (SwiftUI & AppKit)
│   └── Tests/             # Swift Testing test suite
└── PHYSICS_SPEC.md        # Mathematical specification for the 240 Hz solver
```
