using System;
using UnityEngine;

namespace PsyCurio.Shop.Audio
{
    /// <summary>
    /// Shared plumbing for the project's generated audio (there are no
    /// licensed assets — every clip is code). Clips are non-streaming with
    /// data set once: AOT-safe on IL2CPP, no callback timing risk on device.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;

        /// <summary>Mono clip from a finished sample buffer.</summary>
        public static AudioClip FromSamples(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Fills a buffer of the given duration by evaluating the
        /// generator at each sample's time in seconds.</summary>
        public static float[] Fill(float seconds, Func<float, float> generator)
        {
            var samples = new float[(int)(SampleRate * seconds)];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = generator(i / (float)SampleRate);
            }
            return samples;
        }
    }
}
