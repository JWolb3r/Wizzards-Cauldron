using UnityEngine;

namespace WizzardsCauldron.Presentation
{
    /// <summary>
    /// Creates a small, seamless ambient loop at runtime so the project does
    /// not depend on an external music file or third-party audio license.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AstralAmbientMusic : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const float LoopSeconds = 24f;
        private const string ClipName = "WC Astral Alchemy Ambience";

        private static AudioClip _sharedClip;
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = true;
            _source.spatialBlend = 0f;
            _source.dopplerLevel = 0f;

            if (_sharedClip == null)
            {
                _sharedClip = BuildClip();
            }

            _source.clip = _sharedClip;
        }

        private void Start()
        {
            if (!_source.isPlaying)
            {
                _source.Play();
            }
        }

        private static AudioClip BuildClip()
        {
            int sampleCount = Mathf.RoundToInt(
                SampleRate * LoopSeconds);
            float[] samples = new float[sampleCount];
            float[] melodyNotes =
            {
                220f,
                261.6256f,
                329.6276f,
                293.6648f,
                246.9417f,
                329.6276f,
                392f,
                261.6256f
            };

            for (int index = 0; index < sampleCount; index++)
            {
                float time = (float)index / SampleRate;
                float slowBreath = 0.76f +
                    0.24f * Mathf.Sin(
                        2f * Mathf.PI * time / LoopSeconds);

                float pad =
                    Sine(time, SeamlessFrequency(110f)) * 0.32f +
                    Sine(time, SeamlessFrequency(164.8138f)) * 0.20f +
                    Sine(time, SeamlessFrequency(220f)) * 0.12f;

                float noteLength = LoopSeconds / melodyNotes.Length;
                int noteIndex = Mathf.Min(
                    melodyNotes.Length - 1,
                    Mathf.FloorToInt(time / noteLength));
                float noteTime = time - noteIndex * noteLength;
                float noteEnvelope = Mathf.Pow(
                    Mathf.Sin(Mathf.PI * noteTime / noteLength),
                    2f);
                float melody =
                    Sine(time, melodyNotes[noteIndex]) *
                    noteEnvelope * 0.18f;
                float shimmer =
                    Sine(time, melodyNotes[noteIndex] * 2f) *
                    noteEnvelope * 0.035f;

                samples[index] = Mathf.Clamp(
                    (pad * slowBreath + melody + shimmer) * 0.32f,
                    -0.72f,
                    0.72f);
            }

            AudioClip clip = AudioClip.Create(
                ClipName,
                sampleCount,
                1,
                SampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float SeamlessFrequency(float desiredFrequency)
        {
            return Mathf.Round(desiredFrequency * LoopSeconds) /
                LoopSeconds;
        }

        private static float Sine(float time, float frequency)
        {
            return Mathf.Sin(2f * Mathf.PI * frequency * time);
        }
    }
}
