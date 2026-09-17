using System;
using System.Linq;
using Hangly.Windows.Models;
using Hangly.Windows.Services;
using Xunit;

namespace Hangly.Windows.Tests;

public class AudioTests
{
    [Fact]
    public void SamplesProduceValidAudio()
    {
        foreach (CharmSound sound in Enum.GetValues<CharmSound>())
        {
            float[] samples = SoundSynthesizer.Samples(sound);
            Assert.NotEmpty(samples);

            double seconds = samples.Length / SoundSynthesizer.SampleRate;
            Assert.True(seconds >= 0.05, $"{sound} too short");
            Assert.True(seconds <= 2.0, $"{sound} too long");

            // Peak amplitude bounded
            float peak = samples.Max(s => Math.Abs(s));
            Assert.True(peak <= 1.0f);
            Assert.True(peak >= 0.4f);
        }
    }

    [Fact]
    public void OutputIsDeterministic()
    {
        foreach (CharmSound sound in Enum.GetValues<CharmSound>())
        {
            float[] first = SoundSynthesizer.Samples(sound);
            float[] second = SoundSynthesizer.Samples(sound);

            Assert.Equal(first.Length, second.Length);
            for (int i = 0; i < first.Length; i++)
            {
                Assert.Equal(first[i], second[i]);
            }
        }
    }

    [Fact]
    public void SoundsAreDistinct()
    {
        float[] bell = SoundSynthesizer.Samples(CharmSound.Bell);
        float[] wood = SoundSynthesizer.Samples(CharmSound.Wood);

        Assert.NotEqual(bell.Length, wood.Length);
    }

    [Fact]
    public void ReleaseDoesNotClick()
    {
        int lastMoment = (int)(0.005 * SoundSynthesizer.SampleRate);
        foreach (CharmSound sound in Enum.GetValues<CharmSound>())
        {
            float[] samples = SoundSynthesizer.Samples(sound);
            float tail = samples[^lastMoment..].Max(s => Math.Abs(s));
            Assert.True(tail < 0.05f, $"{sound} ends abruptly: {tail}");
        }
    }

    [Fact]
    public void NormalizationIsExact()
    {
        float[] samples = [0.1f, -0.5f, 0.25f];
        float[] loud = SoundSynthesizer.Normalized(samples, peak: 0.8f);
        Assert.Equal(0.8f, loud.Max(s => Math.Abs(s)), precision: 5);

        float[] zeroes = [0f, 0f, 0f];
        Assert.Equal(zeroes, SoundSynthesizer.Normalized(zeroes, peak: 0.8f));
    }
}
