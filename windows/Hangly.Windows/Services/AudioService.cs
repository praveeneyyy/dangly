using System;
using System.Collections.Concurrent;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using Hangly.Windows.Models;

namespace Hangly.Windows.Services;

/// <summary>
/// Audio service that synthesizes and plays charm sounds with cooldown and intensity scaling.
/// </summary>
public sealed class AudioService
{
    private const double CooldownSeconds = 0.12;
    private DateTime _lastPlayTime = DateTime.MinValue;
    private readonly ConcurrentDictionary<CharmSound, float[]> _cachedSamples = new();

    public bool IsEnabled { get; set; } = true;
    public double MasterVolume { get; set; } = 1.0;
    public double Volume
    {
        get => MasterVolume;
        set => MasterVolume = value;
    }

    public AudioService()
    {
        // Pre-cache samples asynchronously
        Task.Run(() =>
        {
            foreach (CharmSound sound in Enum.GetValues<CharmSound>())
            {
                _cachedSamples[sound] = SoundSynthesizer.Samples(sound);
            }
        });
    }

    public void Play(CharmSound sound, double intensity = 1.0)
    {
        if (!IsEnabled) return;

        double volume = MasterVolume * Math.Clamp(intensity, 0.0, 1.0);
        if (volume <= 0.005) return;

        var now = DateTime.UtcNow;
        if ((now - _lastPlayTime).TotalSeconds < CooldownSeconds) return;
        _lastPlayTime = now;

        Task.Run(() =>
        {
            try
            {
                if (!_cachedSamples.TryGetValue(sound, out var samples))
                {
                    samples = SoundSynthesizer.Samples(sound);
                    _cachedSamples[sound] = samples;
                }

                byte[] wav = SoundSynthesizer.BuildWavBytes(samples, (float)volume);
                using var ms = new MemoryStream(wav);
                using var player = new SoundPlayer(ms);
                player.Play();
            }
            catch
            {
                // Silently ignore audio playback failures on systems with disabled audio
            }
        });
    }

    public void PlayCharm(ICharm charm, double intensity = 1.0)
    {
        if (!string.IsNullOrEmpty(charm.CustomSoundPath) && File.Exists(charm.CustomSoundPath))
        {
            PlayCustomFile(charm.CustomSoundPath, intensity);
        }
        else
        {
            Play(charm.Sound, intensity);
        }
    }

    public void PlayCustomFile(string wavPath, double intensity = 1.0)
    {
        if (!IsEnabled || string.IsNullOrEmpty(wavPath) || !File.Exists(wavPath)) return;

        double volume = MasterVolume * Math.Clamp(intensity, 0.0, 1.0);
        if (volume <= 0.005) return;

        var now = DateTime.UtcNow;
        if ((now - _lastPlayTime).TotalSeconds < CooldownSeconds) return;
        _lastPlayTime = now;

        Task.Run(() =>
        {
            try
            {
                using var player = new SoundPlayer(wavPath);
                player.Play();
            }
            catch
            {
                // Silently ignore audio playback failures on systems with disabled audio
            }
        });
    }
}
