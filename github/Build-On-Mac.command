#!/bin/bash
# ====================================================================
# Dangly - 1-Click macOS Builder
# ====================================================================
# Double-click this script in Finder to compile Dangly on your Mac.

set -e

cd "$(dirname "$0")"
ROOT="$(pwd)"

echo "==> Preparing build environment"

# If in a parent or github directory, locate the macos project folder
if [ -d "macos/Hangly.xcodeproj" ]; then
    cd macos
elif [ -d "../macos/Hangly.xcodeproj" ]; then
    cd ../macos
elif [ ! -d "Hangly.xcodeproj" ]; then
    if [ -f "Dangly-macOS.zip" ]; then
        echo "==> Unzipping Dangly-macOS.zip"
        unzip -q -o "Dangly-macOS.zip"
        cd macos
    else
        echo "Error: Could not locate Hangly.xcodeproj"
        read -p "Press Enter to exit..."
        exit 1
    fi
fi

# Check for xcodebuild
if ! command -v xcodebuild >/dev/null 2>&1; then
    echo "Error: xcodebuild not found. Install Xcode Command Line Tools: xcode-select --install"
    read -p "Press Enter to exit..."
    exit 1
fi

echo "==> Compiling Dangly natively for Apple Silicon (macOS 14+)..."
xcodebuild \
  -project "Hangly.xcodeproj" \
  -scheme "Hangly" \
  -configuration "Production" \
  -destination 'platform=macOS' \
  -derivedDataPath ".build" \
  build | grep -E "error:|warning:|BUILD SUCCEEDED|BUILD FAILED" || true

BUILT_APP=".build/Build/Products/Production/Hangly.app"

if [ ! -d "$BUILT_APP" ]; then
    echo "Error: Build did not produce an application bundle."
    read -p "Press Enter to exit..."
    exit 1
fi

# Reveal and prepare
OUTPUT_APP="$ROOT/Dangly.app"
rm -rf "$OUTPUT_APP"
cp -R "$BUILT_APP" "$OUTPUT_APP"
xattr -dr com.apple.quarantine "$OUTPUT_APP" 2>/dev/null || true

echo ""
echo "=========================================================="
echo " BUILD SUCCEEDED: $OUTPUT_APP"
echo "=========================================================="
echo ""
echo "Opening Dangly..."
open "$OUTPUT_APP"
