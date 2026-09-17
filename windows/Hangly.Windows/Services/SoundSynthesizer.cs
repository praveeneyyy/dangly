using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hangly.Windows.Models;

namespace Hangly.Windows.Services;

/// <summary>
/// Produces mono PCM samples for each CharmSound using additive synthesis and exponential decay.
/// Exact mathematical port of macOS SoundSynthesizer.swift.
/// </summary>
public static class SoundSynthesizer
{
    public const double SampleRate = 44100.0;
    public const double Release = 0.12;

    public readonly record struct Partial(double Ratio, double Amplitude, double Decay);

    public readonly record struct Voice(
        double Fundamental,
        IReadOnlyList<Partial> Partials,
        double Duration,
        double Attack
    );

    public static readonly Voice BellVoice = new(
        Fundamental: 1046.0,
        Partials:
        [
            new(Ratio: 1.00, Amplitude: 1.00, Decay: 1.30),
            new(Ratio: 2.00, Amplitude: 0.55, Decay: 0.90),
            new(Ratio: 2.41, Amplitude: 0.40, Decay: 0.70),
            new(Ratio: 3.00, Amplitude: 0.30, Decay: 0.50),
            new(Ratio: 4.52, Amplitude: 0.18, Decay: 0.35),
            new(Ratio: 5.19, Amplitude: 0.10, Decay: 0.25)
        ],
        Duration: 1.5,
        Attack: 0.003
    );

    public static readonly Voice GlassVoice = new(
        Fundamental: 2600.0,
        Partials:
        [
            new(Ratio: 1.00, Amplitude: 1.0, Decay: 0.28),
            new(Ratio: 1.90, Amplitude: 0.5, Decay: 0.20),
            new(Ratio: 2.75, Amplitude: 0.3, Decay: 0.14)
        ],
        Duration: 0.35,
        Attack: 0.001
    );

    public static readonly Voice MetalVoice = new(
        Fundamental: 1800.0,
        Partials:
        [
            new(Ratio: 1.00, Amplitude: 1.00, Decay: 0.45),
            new(Ratio: 1.56, Amplitude: 0.60, Decay: 0.32),
            new(Ratio: 2.31, Amplitude: 0.35, Decay: 0.22),
            new(Ratio: 3.10, Amplitude: 0.20, Decay: 0.15)
        ],
        Duration: 0.55,
        Attack: 0.001
    );

    public static readonly Voice WoodVoice = new(
        Fundamental: 190.0,
        Partials:
        [
            new(Ratio: 1.0, Amplitude: 1.0, Decay: 0.03),
            new(Ratio: 1.7, Amplitude: 0.4, Decay: 0.02)
        ],
        Duration: 0.07,
        Attack: 0.0005
    );

    public static readonly Voice SoftVoice = new(
        Fundamental: 160.0,
        Partials: [new(Ratio: 1.0, Amplitude: 1.0, Decay: 0.045)],
        Duration: 0.09,
        Attack: 0.002
    );

    /// <summary>
    /// Full-scale samples in [-1, 1], peak-normalized per sound.
    /// </summary>
    public static float[] Samples(CharmSound sound) => sound switch
    {
        CharmSound.Bell => Normalized(Render(BellVoice), peak: 0.85f),
        CharmSound.Glass => Normalized(Render(GlassVoice), peak: 0.70f),
        CharmSound.Metal => Normalized(Render(MetalVoice), peak: 0.75f),
        CharmSound.Wood => Normalized(Mix(NoiseBurst(0.07, 0.012, 0.25, 0.7), Render(WoodVoice)), peak: 0.80f),
        CharmSound.Soft => Normalized(Mix(Render(SoftVoice), NoiseBurst(0.09, 0.006, 0.08, 0.15)), peak: 0.50f),
        _ => Normalized(Render(BellVoice), peak: 0.85f)
    };

    public static float[] Render(Voice voice)
    {
        int count = (int)(voice.Duration * SampleRate);
        double release = Math.Min(Release, voice.Duration * 0.3);
        float[] output = new float[count];

        for (int index = 0; index < count; index++)
        {
            double time = index / SampleRate;
            double attack = voice.Attack > 0 ? Math.Min(1.0, time / voice.Attack) : 1.0;
            double fade = Math.Min(1.0, (voice.Duration - time) / release);
            double envelope = attack * Math.Max(0.0, fade);

            double value = 0.0;
            foreach (var partial in voice.Partials)
            {
                double amplitude = partial.Amplitude * Math.Exp(-time / partial.Decay);
                value += amplitude * Math.Sin(2.0 * Math.PI * voice.Fundamental * partial.Ratio * time);
            }
            output[index] = (float)(value * envelope);
        }
        return output;
    }

    public static float[] NoiseBurst(double duration, double decay, double smoothing, double gain)
    {
        int count = (int)(duration * SampleRate);
        float[] output = new float[count];
        uint state = 0x9E3779B9;
        double filtered = 0.0;

        for (int index = 0; index < count; index++)
        {
            // Linear congruential generator: state = state * 1664525 + 1013904223
            state = unchecked((state * 1664525u) + 1013904223u);
            double white = ((double)state / uint.MaxValue * 2.0) - 1.0;
            filtered += smoothing * (white - filtered);
            double time = index / SampleRate;
            output[index] = (float)(filtered * Math.Exp(-time / decay) * gain);
        }
        return output;
    }

    public static float[] Mix(float[] first, float[] second)
    {
        int count = Math.Max(first.Length, second.Length);
        float[] output = new float[count];
        for (int i = 0; i < count; i++)
        {
            float left = i < first.Length ? first[i] : 0f;
            float right = i < second.Length ? second[i] : 0f;
            output[i] = left + right;
        }
        return output;
    }

    public static float[] Normalized(float[] samples, float peak)
    {
        float loudest = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float a = Math.Abs(samples[i]);
            if (a > loudest) loudest = a;
        }

        if (loudest <= float.Epsilon) return samples;
        float scale = peak / loudest;
        float[] result = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            result[i] = samples[i] * scale;
        }
        return result;
    }

    /// <summary>
    /// Builds standard 16-bit 44.1kHz mono WAV bytes from float samples.
    /// </summary>
    public static byte[] BuildWavBytes(float[] samples, float volume = 1.0f)
    {
        int sampleCount = samples.Length;
        int dataSize = sampleCount * 2;
        int fullSize = 44 + dataSize;

        using var ms = new MemoryStream(fullSize);
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8.ToArray());
        writer.Write(fullSize - 8);
        writer.Write("WAVE"u8.ToArray());

        // fmt subchunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // Subchunk1Size (16 for PCM)
        writer.Write((short)1); // AudioFormat (1 for PCM)
        writer.Write((short)1); // NumChannels (1 for Mono)
        writer.Write((int)SampleRate); // SampleRate
        writer.Write((int)SampleRate * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
        writer.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8)
        writer.Write((short)16); // BitsPerSample

        // data subchunk
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        float clampedVol = Math.Clamp(volume, 0f, 1f);
        for (int i = 0; i < sampleCount; i++)
        {
            float scaled = samples[i] * clampedVol;
            short pcm = (short)Math.Clamp((int)Math.Round(scaled * 32767.0f), -32768, 32767);
            writer.Write(pcm);
        }

        return ms.ToArray();
    }
}
