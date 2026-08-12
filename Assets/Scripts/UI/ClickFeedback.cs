using PsyCurio.Shop.Interaction;
using UnityEngine;

namespace PsyCurio.Shop.Ui
{
    /// <summary>
    /// The audible half of "no dead clicks": every click outcome has a sound.
    /// Placement ticks, removal tocks, a refused sixth item buzzes, and a
    /// click that hits nothing interactive gets a soft tap instead of silence.
    /// All clips are generated in code (sine bursts with exponential decay) —
    /// no licensed audio anywhere in the project.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ClickFeedback : MonoBehaviour
    {
        private const int SampleRate = 44100;

        [SerializeField] private ClickRouter router;
        [SerializeField] private ShopController controller;
        [SerializeField] private CounterSlots counterSlots;

        private AudioSource source;
        private AudioClip placeWhoosh;
        private AudioClip landThump;
        private AudioClip removeTock;
        private AudioClip refusalBuzz;
        private AudioClip deadTap;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            placeWhoosh = Whoosh("place-whoosh", 0.3f, 0.4f);
            landThump = Tone("land-thump", 160f, 0.09f, 0.6f);
            removeTock = Tone("remove-tock", 520f, 0.08f, 0.5f);
            refusalBuzz = Buzz("refusal-buzz", 175f, 0.22f, 0.5f);
            deadTap = Tone("dead-tap", 340f, 0.05f, 0.3f);
        }

        private void OnEnable()
        {
            controller.PlacementAccepted += PlayPlace;
            controller.ItemRemoved += PlayRemove;
            controller.ShopReset += PlayRemove;
            controller.PlacementRefused += PlayRefusal;
            router.DeadClicked += PlayDead;
            counterSlots.ItemLanded += PlayLanded;
        }

        private void OnDisable()
        {
            controller.PlacementAccepted -= PlayPlace;
            controller.ItemRemoved -= PlayRemove;
            controller.ShopReset -= PlayRemove;
            controller.PlacementRefused -= PlayRefusal;
            router.DeadClicked -= PlayDead;
            counterSlots.ItemLanded -= PlayLanded;
        }

        private void PlayPlace()
        {
            source.PlayOneShot(placeWhoosh);
        }

        private void PlayLanded()
        {
            source.PlayOneShot(landThump);
        }

        private void PlayRemove()
        {
            source.PlayOneShot(removeTock);
        }

        private void PlayRefusal()
        {
            source.PlayOneShot(refusalBuzz);
        }

        private void PlayDead()
        {
            source.PlayOneShot(deadTap);
        }

        private static AudioClip Tone(string name, float frequency, float seconds, float volume)
        {
            var samples = Fill(seconds, (i, t) =>
                Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 30f) * volume);
            return ToClip(name, samples);
        }

        /// <summary>Rising filtered-noise sweep — the flight sound.</summary>
        private static AudioClip Whoosh(string name, float seconds, float volume)
        {
            var random = new System.Random(7);
            var previous = 0f;
            var samples = Fill(seconds, (i, t) =>
            {
                var progress = t / seconds;
                // Noise through a one-pole low-pass whose cutoff rises with
                // progress, enveloped to swell and release.
                var noise = (float)random.NextDouble() * 2f - 1f;
                var smoothing = Mathf.Lerp(0.98f, 0.6f, progress);
                previous = previous * smoothing + noise * (1f - smoothing);
                var envelope = Mathf.Sin(progress * Mathf.PI);
                return previous * envelope * volume * 2.2f;
            });
            return ToClip(name, samples);
        }

        private static AudioClip Buzz(string name, float frequency, float seconds, float volume)
        {
            var samples = Fill(seconds, (i, t) =>
            {
                var wave = Mathf.Sin(2f * Mathf.PI * frequency * t)
                           + 0.4f * Mathf.Sin(2f * Mathf.PI * frequency * 2.02f * t);
                return Mathf.Clamp(wave, -0.7f, 0.7f) * Mathf.Exp(-t * 9f) * volume;
            });
            return ToClip(name, samples);
        }

        private static float[] Fill(float seconds, System.Func<int, float, float> generator)
        {
            var samples = new float[(int)(SampleRate * seconds)];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = generator(i, i / (float)SampleRate);
            }
            return samples;
        }

        private static AudioClip ToClip(string name, float[] samples)
        {
            // Non-streaming clip with data set once: AOT-safe on IL2CPP, no
            // callback timing risk on device.
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
