using System;
using System.IO;
using System.Text.Json;
using Hangly.Windows.Models;
using Microsoft.Win32;

namespace Hangly.Windows.Services;

/// <summary>
/// Manages persistence of AppSettings to %LOCALAPPDATA%\Dangly\settings.json.
/// Also synchronizes LaunchAtLogin with the Windows Run registry key.
/// </summary>
public sealed class SettingsStore
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Dangly";

    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings Settings { get; private set; }

    public event Action? SettingsChanged;

    public SettingsStore()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string hanglyDir = Path.Combine(localAppData, AppName);
        Directory.CreateDirectory(hanglyDir);
        _settingsPath = Path.Combine(hanglyDir, "settings.json");

        Settings = Load();
        SyncLaunchAtLogin(Settings.LaunchAtLogin);
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null) return loaded;
            }
        }
        catch
        {
            // Fall back to defaults
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(Settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
            SyncLaunchAtLogin(Settings.LaunchAtLogin);
            SettingsChanged?.Invoke();
        }
        catch
        {
            // Silently handle save failures
        }
    }

    public void Update(Action<AppSettings> modify)
    {
        modify(Settings);
        Save();
    }

    private static void SyncLaunchAtLogin(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
            if (key == null) return;

            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return;

            if (enable)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Ignore registry permission issues
        }
    }
}
