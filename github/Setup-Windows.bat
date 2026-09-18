@echo off
setlocal enabledelayedexpansion
title Dangly Windows Setup

echo ==========================================================
echo               Dangly - Windows App Setup                  
echo ==========================================================
echo.
echo Please select an option:
echo.
echo   [1] Install to %%LOCALAPPDATA%%\Dangly (Desktop + Start Menu shortcuts)
echo   [2] Quick Run directly from current directory
echo   [3] Create Desktop Shortcut for current directory
echo   [4] Exit
echo.

set /p choice="Enter your choice (1-4) [default: 1]: "
if "%choice%"=="" set choice=1

if "%choice%"=="1" goto INSTALL
if "%choice%"=="2" goto QUICKRUN
if "%choice%"=="3" goto SHORTCUT
if "%choice%"=="4" goto END

:INSTALL
echo.
echo Starting automated installation...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Dangly.ps1" -LaunchAfter
goto END

:QUICKRUN
echo.
echo Extracting portable package locally if needed...
if not exist "%~dp0windows_app" (
    powershell.exe -NoProfile -Command "Expand-Archive -Path '%~dp0Dangly-Windows.zip' -DestinationPath '%~dp0windows_app' -Force"
)
if exist "%~dp0windows_app\Dangly.exe" (
    start "" "%~dp0windows_app\Dangly.exe"
    echo Dangly launched!
) else (
    echo Error: Could not find Dangly.exe in %~dp0windows_app
)
goto END

:SHORTCUT
echo.
echo Creating Desktop shortcut...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Dangly.ps1" -DesktopShortcut -StartMenuShortcut:$false -LaunchAfter:$false
goto END

:END
echo.
pause
