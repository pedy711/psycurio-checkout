using PsyCurio.Shop.Audio;
using UnityEngine;

namespace PsyCurio.Shop.Therapist
{
    /// <summary>
    /// Looping ambient noise bed whose level the therapist controls. The clip
    /// is generated in code (brown noise, seam cross-faded for a clickless
    /// loop) — soft room rumble at low levels, oppressive at full.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientNoise : MonoBehaviour
    {
        private const int SampleRate = ProceduralAudio.SampleRate;
        private const float LoopSeconds = 3.2f;
        private const float SeamCrossfadeSeconds = 0.25f;

        [Range(0f, 1f)]
        [SerializeField] private float level;

        private AudioSource source;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void SetLevel(float value)
        {
            // Callers (the therapist panel applying defaults) may run before
            // this component's own Awake — Unity guarantees no Awake order.
            EnsureInitialized();
            level = Mathf.Clamp01(value);
            source.volume = level;
        }

        private void EnsureInitialized()
        {
            if (source != null)
            {
                return;
            }
            source = GetComponent<AudioSource>();
            source.clip = GenerateLoop();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = level;
            source.Play();
        }

        private static AudioClip GenerateLoop()
        {
            var random = new System.Random(20260811);
            var total = (int)(SampleRate * LoopSeconds);
            var samples = new float[total];

            // Brown noise: integrated white noise, softly clamped.
            var value = 0f;
            for (var i = 0; i < total; i++)
            {
                value += ((float)random.NextDouble() * 2f - 1f) * 0.02f;
                value = Mathf.Clamp(value, -1f, 1f) * 0.999f;
                samples[i] = value;
            }

            // Normalize to a modest peak so full volume is loud, not clipping.
            var peak = 0f;
            foreach (var sample in samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
            for (var i = 0; i < total; i++)
            {
                samples[i] = samples[i] / peak * 0.6f;
            }

            // Cross-fade the tail into the head so the loop seam is silent.
            var seam = (int)(SampleRate * SeamCrossfadeSeconds);
            for (var i = 0; i < seam; i++)
            {
                var blend = i / (float)seam;
                samples[i] = samples[i] * blend + samples[total - seam + i] * (1f - blend);
            }

            var trimmed = new float[total - seam];
            System.Array.Copy(samples, trimmed, total - seam);
            return ProceduralAudio.FromSamples("ambient-brown-noise", trimmed);
        }
    }
}
