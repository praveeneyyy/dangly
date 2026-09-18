#!/bin/bash
# ====================================================================
# Dangly - macOS App Setup & Launcher
# ====================================================================
# Double-click this script in Finder to set up and run Dangly on macOS.

set -e

# Change directory to the folder containing this script
cd "$(dirname "$0")"
SCRIPT_DIR="$(pwd)"

echo "=========================================================="
echo "               Dangly - macOS App Setup                   "
echo "=========================================================="
echo ""

# Colors
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# 1. Check Architecture
ARCH=$(uname -m)
if [ "$ARCH" != "arm64" ]; then
    echo -e "${YELLOW}[!] Warning: Dangly is optimized for Apple Silicon (M1/M2/M3/M4, arm64). Detected: $ARCH${NC}"
else
    echo -e "${GREEN}[+] Apple Silicon architecture confirmed ($ARCH).${NC}"
fi

# 2. Extract macOS package if present
if [ -f "Dangly-macOS.zip" ] && [ ! -d "macos" ]; then
    echo -e "${CYAN}[*] Extracting Dangly-macOS.zip...${NC}"
    unzip -q -o "Dangly-macOS.zip"
    echo -e "${GREEN}[+] Package extracted.${NC}"
fi

# Determine source location
MACOS_DIR=""
if [ -d "macos/Hangly.xcodeproj" ]; then
    MACOS_DIR="$SCRIPT_DIR/macos"
elif [ -d "../macos/Hangly.xcodeproj" ]; then
    MACOS_DIR="$(cd .. && pwd)/macos"
elif [ -d "Hangly.xcodeproj" ]; then
    MACOS_DIR="$SCRIPT_DIR"
fi

# 3. Check for pre-existing Dangly.app
BUILT_APP=""
if [ -d "$SCRIPT_DIR/Dangly.app" ]; then
    BUILT_APP="$SCRIPT_DIR/Dangly.app"
elif [ -d "/Applications/Dangly.app" ]; then
    BUILT_APP="/Applications/Dangly.app"
fi

if [ -n "$BUILT_APP" ]; then
    echo -e "${GREEN}[+] Found existing application: $BUILT_APP${NC}"
    echo ""
    read -p "Do you want to launch it now? [Y/n]: " RUN_NOW
    RUN_NOW=${RUN_NOW:-Y}
    if [[ "$RUN_NOW" =~ ^[Yy]$ ]]; then
        echo -e "${CYAN}[*] Removing Gatekeeper quarantine attribute...${NC}"
        xattr -dr com.apple.quarantine "$BUILT_APP" 2>/dev/null || true
        open "$BUILT_APP"
        echo -e "${GREEN}[+] Dangly launched! Check your top-right screen area.${NC}"
        exit 0
    fi
fi

# 4. Check Xcode build tools
if ! command -v xcodebuild >/dev/null 2>&1; then
    echo -e "${RED}[-] Xcode Command Line Tools not detected.${NC}"
    echo "  To build Dangly from source, install Xcode command-line tools by running:"
    echo "    xcode-select --install"
    echo "  Or open macos/Hangly.xcodeproj directly in the Xcode application."
    echo ""
    read -p "Press Enter to exit..."
    exit 1
fi

if [ -z "$MACOS_DIR" ]; then
    echo -e "${RED}[-] Could not locate Hangly.xcodeproj.${NC}"
    read -p "Press Enter to exit..."
    exit 1
fi

# 5. Build Dangly
echo -e "${CYAN}[*] Compiling Dangly natively for macOS...${NC}"
BUILD_DIR="$SCRIPT_DIR/.build"

xcodebuild \
  -project "$MACOS_DIR/Hangly.xcodeproj" \
  -scheme "Hangly" \
  -configuration "Production" \
  -destination 'platform=macOS' \
  -derivedDataPath "$BUILD_DIR" \
  build | grep -E "error:|warning:|BUILD SUCCEEDED|BUILD FAILED" || true

PROD_APP="$BUILD_DIR/Build/Products/Production/Hangly.app"

if [ ! -d "$PROD_APP" ]; then
    echo -e "${RED}[-] Build failed or output app was not created.${NC}"
    echo "Try opening $MACOS_DIR/Hangly.xcodeproj in Xcode directly to inspect any build diagnostics."
    read -p "Press Enter to exit..."
    exit 1
fi

# 6. Copy and prepare Dangly.app
TARGET_APP="$SCRIPT_DIR/Dangly.app"
rm -rf "$TARGET_APP"
cp -R "$PROD_APP" "$TARGET_APP"

# Strip quarantine
echo -e "${CYAN}[*] Configuring permissions and Gatekeeper clearance...${NC}"
xattr -dr com.apple.quarantine "$TARGET_APP" 2>/dev/null || true

echo -e "${GREEN}[+] Dangly.app successfully compiled!${NC}"
echo ""

# 7. Prompt installation to /Applications
read -p "Install Dangly to your /Applications folder? [Y/n]: " INSTALL_APP
INSTALL_APP=${INSTALL_APP:-Y}

if [[ "$INSTALL_APP" =~ ^[Yy]$ ]]; then
    echo -e "${CYAN}[*] Installing to /Applications/Dangly.app...${NC}"
    rm -rf "/Applications/Dangly.app"
    cp -R "$TARGET_APP" "/Applications/Dangly.app"
    xattr -dr com.apple.quarantine "/Applications/Dangly.app" 2>/dev/null || true
    echo -e "${GREEN}[+] Installed to /Applications/Dangly.app${NC}"
    TARGET_APP="/Applications/Dangly.app"
fi

echo ""
echo -e "${GREEN}==========================================================${NC}"
echo -e "${GREEN}               Setup Finished Successfully!               ${NC}"
echo -e "${GREEN}==========================================================${NC}"
echo ""

read -p "Launch Dangly now? [Y/n]: " LAUNCH_APP
LAUNCH_APP=${LAUNCH_APP:-Y}
if [[ "$LAUNCH_APP" =~ ^[Yy]$ ]]; then
    open "$TARGET_APP"
    echo -e "${GREEN}[+] Dangly is now running!${NC}"
fi

echo ""
exit 0
