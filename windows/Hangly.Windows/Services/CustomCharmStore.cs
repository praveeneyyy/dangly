using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Hangly.Windows.Models;
using Hangly.Windows.Models.Charms;

namespace Hangly.Windows.Services;

public sealed class CustomCharmStore
{
    private const string AppName = "Dangly";
    private const string CharmsDirName = "Charms";
    private const string ManifestName = "manifest.json";

    private readonly string _charmsDir;
    private readonly string _manifestPath;
    private readonly Dictionary<Guid, CustomCharm> _cache = new();
    private readonly List<CustomCharmEntry> _entries = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<CustomCharmEntry> Entries => _entries.AsReadOnly();

    public event Action? StoreChanged;

    public CustomCharmStore(string? customDirectory = null)
    {
        if (customDirectory != null)
        {
            _charmsDir = customDirectory;
        }
        else
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _charmsDir = Path.Combine(localAppData, AppName, CharmsDirName);
        }

        Directory.CreateDirectory(_charmsDir);
        _manifestPath = Path.Combine(_charmsDir, ManifestName);

        LoadManifest();
    }

    private void LoadManifest()
    {
        _entries.Clear();
        _cache.Clear();

        try
        {
            if (File.Exists(_manifestPath))
            {
                string json = File.ReadAllText(_manifestPath);
                var loaded = JsonSerializer.Deserialize<List<CustomCharmEntry>>(json, JsonOptions);
                if (loaded != null)
                {
                    // Only keep entries whose image file actually exists
                    foreach (var entry in loaded)
                    {
                        string imgPath = Path.Combine(_charmsDir, entry.ImageFileName);
                        if (File.Exists(imgPath))
                        {
                            _entries.Add(entry);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore corrupted manifest, start clean
        }
    }

    private void SaveManifest()
    {
        try
        {
            string json = JsonSerializer.Serialize(_entries, JsonOptions);
            File.WriteAllText(_manifestPath, json);
            StoreChanged?.Invoke();
        }
        catch
        {
            // Ignore write failures
        }
    }

    public CustomCharm Add(ProcessedCharmImage processed, string name, CharmSound sound = CharmSound.Wood, string? customSoundPath = null)
    {
        var id = Guid.NewGuid();
        string fileName = $"{id:D}.png";
        string filePath = Path.Combine(_charmsDir, fileName);

        File.WriteAllBytes(filePath, processed.PngData);

        var entry = new CustomCharmEntry
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(name) ? "Custom Charm" : name.Trim(),
            CreatedAt = DateTime.UtcNow,
            ImageFileName = fileName,
            Metrics = processed.Metrics,
            Palette = processed.Palette,
            Sound = sound,
            CustomSoundPath = customSoundPath
        };

        _entries.Add(entry);
        SaveManifest();

        using var ms = new MemoryStream(processed.PngData);
        var bitmap = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        bitmap.Freeze();

        var charm = new CustomCharm(entry, bitmap);
        _cache[id] = charm;

        return charm;
    }

    public void Remove(Guid id)
    {
        int index = _entries.FindIndex(e => e.Id == id);
        if (index >= 0)
        {
            var entry = _entries[index];
            _entries.RemoveAt(index);
            _cache.Remove(id);

            string filePath = Path.Combine(_charmsDir, entry.ImageFileName);
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { }
            }

            SaveManifest();
        }
    }

    public CustomCharmEntry? GetEntry(Guid id) => _entries.FirstOrDefault(e => e.Id == id);

    public CustomCharm? GetCharm(Guid id)
    {
        if (_cache.TryGetValue(id, out var cached))
            return cached;

        var entry = GetEntry(id);
        if (entry == null) return null;

        string filePath = Path.Combine(_charmsDir, entry.ImageFileName);
        if (!File.Exists(filePath)) return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            bitmap.Freeze();

            var charm = new CustomCharm(entry, bitmap);
            _cache[id] = charm;
            return charm;
        }
        catch
        {
            return null;
        }
    }
}
