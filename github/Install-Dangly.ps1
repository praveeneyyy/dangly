<#
.SYNOPSIS
    Installs and sets up Dangly for Windows.
.DESCRIPTION
    Extracts Dangly-Windows.zip to %LOCALAPPDATA%\Dangly (or custom path),
    creates Start Menu and Desktop shortcuts with custom icon,
    validates .NET 10 desktop runtime availability, and optionally launches the app.
#>

[CmdletBinding()]
param(
    [string]$InstallPath = "$env:LOCALAPPDATA\Dangly",
    [switch]$DesktopShortcut = $true,
    [switch]$StartMenuShortcut = $true,
    [switch]$LaunchAfter = $true,
    [switch]$AutoStart = $false
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[+] $Message" -ForegroundColor Green
}

function Write-WarnMsg {
    param([string]$Message)
    Write-Host "[!] $Message" -ForegroundColor Yellow
}

function Write-ErrMsg {
    param([string]$Message)
    Write-Host "[-] $Message" -ForegroundColor Red
}

Clear-Host
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host "               Dangly - Windows App Setup                 " -ForegroundColor Magenta
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ZipSource = Join-Path $ScriptDir "Dangly-Windows.zip"

if (-not (Test-Path $ZipSource)) {
    # Check parent directory as fallback
    $ParentZip = Join-Path (Split-Path -Parent $ScriptDir) "dist\Dangly-Windows.zip"
    if (Test-Path $ParentZip) {
        $ZipSource = $ParentZip
    } else {
        Write-ErrMsg "Could not find 'Dangly-Windows.zip' in $ScriptDir"
        Write-Host "Please ensure 'Dangly-Windows.zip' is placed alongside this installer."
        Exit 1
    }
}

# 1. Runtime Check
Write-Step "Checking for .NET Desktop Runtime..."
$hasDotNet10 = $false
try {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft.WindowsDesktop.App 10\.") {
        $hasDotNet10 = $true
        Write-Success ".NET 10 Desktop Runtime detected."
    } elseif ($runtimes -match "Microsoft.WindowsDesktop.App") {
        Write-WarnMsg "Detected .NET Desktop Runtime, but .NET 10 is recommended."
        $hasDotNet10 = $true
    }
} catch {
    # dotnet CLI not found or errored
}

if (-not $hasDotNet10) {
    Write-WarnMsg ".NET 10 Desktop Runtime was not detected."
    Write-Host "  Dangly requires the .NET 10 Desktop Runtime (x64)." -ForegroundColor Yellow
    Write-Host "  Download link: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Gray
    Write-Host "  (If already installed system-wide without dotnet CLI on PATH, you can still continue)" -ForegroundColor Gray
    Write-Host ""
}

# 2. Prepare Installation Directory
Write-Step "Setting up target directory: $InstallPath"
if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

# 3. Extract Package
Write-Step "Extracting files from package..."
try {
    # Stop running Dangly if running
    $runningProc = Get-Process -Name "Dangly" -ErrorAction SilentlyContinue
    if ($runningProc) {
        Write-WarnMsg "Closing currently running Dangly process..."
        $runningProc | Stop-Process -Force
        Start-Sleep -Seconds 1
    }
    
    Expand-Archive -Path $ZipSource -DestinationPath $InstallPath -Force
    Write-Success "Extracted files successfully to $InstallPath."
} catch {
    Write-ErrMsg "Failed to extract package: $_"
    Exit 1
}

$ExePath = Join-Path $InstallPath "Dangly.exe"
if (-not (Test-Path $ExePath)) {
    Write-ErrMsg "Executable not found at $ExePath after extraction."
    Exit 1
}

# 4. Icon Resolution
$IconPath = Join-Path $InstallPath "Assets\Icons\app.ico"
if (-not (Test-Path $IconPath)) {
    $IconPath = Join-Path $InstallPath "Assets\Branding\app.ico"
}
if (-not (Test-Path $IconPath)) {
    $IconPath = $ExePath
}

# 5. Create Shortcuts
$WshShell = New-Object -ComObject WScript.Shell

if ($DesktopShortcut) {
    Write-Step "Creating Desktop shortcut..."
    $DesktopDir = [Environment]::GetFolderPath("Desktop")
    $DesktopLinkPath = Join-Path $DesktopDir "Dangly.lnk"
    $Shortcut = $WshShell.CreateShortcut($DesktopLinkPath)
    $Shortcut.TargetPath = $ExePath
    $Shortcut.WorkingDirectory = $InstallPath
    $Shortcut.Description = "Dangly - Desktop Physics Ornament"
    $Shortcut.IconLocation = "$IconPath,0"
    $Shortcut.Save()
    Write-Success "Desktop shortcut created."
}

if ($StartMenuShortcut) {
    Write-Step "Creating Start Menu shortcut..."
    $StartProgramsDir = [Environment]::GetFolderPath("Programs")
    $StartLinkPath = Join-Path $StartProgramsDir "Dangly.lnk"
    $Shortcut = $WshShell.CreateShortcut($StartLinkPath)
    $Shortcut.TargetPath = $ExePath
    $Shortcut.WorkingDirectory = $InstallPath
    $Shortcut.Description = "Dangly - Desktop Physics Ornament"
    $Shortcut.IconLocation = "$IconPath,0"
    $Shortcut.Save()
    Write-Success "Start Menu shortcut created."
}

# 6. Auto-start (Optional)
if ($AutoStart) {
    Write-Step "Configuring Launch on Windows Startup..."
    $RegPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    Set-ItemProperty -Path $RegPath -Name "Dangly" -Value "`"$ExePath`""
    Write-Success "Configured to run on user login."
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "              Dangly Setup Completed!                     " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installed Location : $InstallPath" -ForegroundColor Gray
Write-Host "Executable         : $ExePath" -ForegroundColor Gray
Write-Host ""

if ($LaunchAfter) {
    Write-Step "Launching Dangly..."
    Start-Process -FilePath $ExePath -WorkingDirectory $InstallPath
    Write-Success "Dangly is now running on your desktop!"
}
