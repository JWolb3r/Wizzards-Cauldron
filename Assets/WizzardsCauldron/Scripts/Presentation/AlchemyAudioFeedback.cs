using System;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.Presentation
{
    /// <summary>
    /// Lightweight, presentation-only audio feedback generated at runtime.
    /// It observes existing gameplay events and does not alter their outcome.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AlchemyAudioFeedback : MonoBehaviour
    {
        private const int SampleRate = 22050;

        [Header("Read-only gameplay sources")]
        [SerializeField]
        private CauldronIntake _cauldronIntake;

        [SerializeField]
        private GameSessionController _gameSession;

        [Header("Spatial output")]
        [SerializeField]
        private AudioSource _potionSource;

        [SerializeField]
        private AudioSource _resetSource;

        [SerializeField]
        private AudioSource _resultSource;

        private AudioClip _potionAcceptedClip;
        private AudioClip _potionRejectedClip;
        private AudioClip _resetClip;
        private AudioClip _optimalClip;
        private AudioClip _validClip;
        private AudioClip _failedClip;
        private bool _subscribed;

        public bool IsReady =>
            _cauldronIntake != null &&
            _gameSession != null &&
            _potionSource != null &&
            _resetSource != null &&
            _resultSource != null &&
            _potionAcceptedClip != null &&
            _resetClip != null &&
            _optimalClip != null;

        private void Awake()
        {
            _potionAcceptedClip = CreatePotionClip(
                "WC_Potion_Shatter_Magic",
                true);
            _potionRejectedClip = CreatePotionClip(
                "WC_Potion_Rejected",
                false);
            _resetClip = CreateResetClip();
            _optimalClip = CreateResultClip(
                "WC_Solution_Optimal",
                new[] { 523.25f, 659.25f, 783.99f, 1046.5f },
                1.15f);
            _validClip = CreateResultClip(
                "WC_Solution_Valid",
                new[] { 440f, 554.37f, 659.25f },
                0.9f);
            _failedClip = CreateResultClip(
                "WC_Solution_Failed",
                new[] { 220f, 174.61f, 146.83f },
                0.78f);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _subscribed)
            {
                return;
            }

            if (!IsReady)
            {
                Debug.LogError(
                    "AlchemyAudioFeedback is missing one or more required references.",
                    this);
                return;
            }

            _cauldronIntake.PotionProcessed += HandlePotionProcessed;
            _gameSession.AttemptFinished += HandleAttemptFinished;
            _gameSession.SessionReset += HandleSessionReset;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            _cauldronIntake.PotionProcessed -= HandlePotionProcessed;
            _gameSession.AttemptFinished -= HandleAttemptFinished;
            _gameSession.SessionReset -= HandleSessionReset;
            _subscribed = false;
        }

        private void HandlePotionProcessed(
            PotionController _,
            PotionAcceptanceResult result)
        {
            AudioClip clip = result == PotionAcceptanceResult.Accepted
                ? _potionAcceptedClip
                : _potionRejectedClip;
            _potionSource.PlayOneShot(clip, 0.9f);
        }

        private void HandleAttemptFinished(AttemptResult result)
        {
            if (result == null)
            {
                return;
            }

            AudioClip clip;
            switch (result.Outcome)
            {
                case AttemptOutcome.OptimalSolution:
                    clip = _optimalClip;
                    break;

                case AttemptOutcome.ValidSolution:
                    clip = _validClip;
                    break;

                default:
                    clip = _failedClip;
                    break;
            }

            _resultSource.PlayOneShot(clip, 0.95f);
        }

        private void HandleSessionReset()
        {
            _resetSource.PlayOneShot(_resetClip, 0.9f);
        }

        private static AudioClip CreatePotionClip(
            string clipName,
            bool accepted)
        {
            const float duration = 0.58f;
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(
                accepted ? 41713 : 9031);

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float value = 0f;

                // Short glass crack followed by a small magical splash.
                float crackEnvelope = Mathf.Exp(-time * 34f);
                float noise = (float)(random.NextDouble() * 2d - 1d);
                value += noise * crackEnvelope * 0.23f;
                value += Mathf.Sin(2f * Mathf.PI * 1850f * time) *
                         crackEnvelope * 0.21f;
                value += Mathf.Sin(2f * Mathf.PI * 2670f * time) *
                         Mathf.Exp(-time * 48f) * 0.12f;

                if (accepted)
                {
                    float magicTime = Mathf.Max(0f, time - 0.09f);
                    float magicEnvelope =
                        Mathf.Exp(-magicTime * 5.5f) *
                        Mathf.SmoothStep(0f, 1f, magicTime * 18f);
                    float frequency = Mathf.Lerp(330f, 760f, time / duration);
                    value += Mathf.Sin(2f * Mathf.PI * frequency * time) *
                             magicEnvelope * 0.16f;
                    value += Mathf.Sin(2f * Mathf.PI * 92f * time) *
                             Mathf.Exp(-magicTime * 7f) * 0.09f;
                }
                else
                {
                    value += Mathf.Sin(2f * Mathf.PI * 145f * time) *
                             Mathf.Exp(-time * 5f) * 0.16f;
                }

                samples[index] = Mathf.Clamp(value, -0.8f, 0.8f);
            }

            return CreateClip(clipName, samples);
        }

        private static AudioClip CreateResetClip()
        {
            const float duration = 0.82f;
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(6257);

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float progress = time / duration;
                float envelope = Mathf.Sin(progress * Mathf.PI);
                float frequency = Mathf.Lerp(780f, 165f, progress);
                float shimmer = Mathf.Sin(
                    2f * Mathf.PI * frequency * time) * envelope * 0.19f;
                float noise = (float)(random.NextDouble() * 2d - 1d) *
                              envelope * 0.055f;
                float endPulse = Mathf.Sin(2f * Mathf.PI * 110f * time) *
                                 Mathf.Pow(progress, 5f) * 0.13f;
                samples[index] = Mathf.Clamp(
                    shimmer + noise + endPulse,
                    -0.75f,
                    0.75f);
            }

            return CreateClip("WC_Room_Reset", samples);
        }

        private static AudioClip CreateResultClip(
            string clipName,
            float[] notes,
            float duration)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            float noteSpacing = duration / (notes.Length + 0.65f);

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float value = 0f;
                for (int noteIndex = 0; noteIndex < notes.Length; noteIndex++)
                {
                    float noteTime = time - noteIndex * noteSpacing;
                    if (noteTime < 0f)
                    {
                        continue;
                    }

                    float envelope = Mathf.Exp(-noteTime * 4.5f) *
                                     Mathf.Clamp01(noteTime * 80f);
                    float frequency = notes[noteIndex];
                    value += Mathf.Sin(
                        2f * Mathf.PI * frequency * noteTime) *
                        envelope * 0.16f;
                    value += Mathf.Sin(
                        2f * Mathf.PI * frequency * 2.01f * noteTime) *
                        envelope * 0.055f;
                }

                samples[index] = Mathf.Clamp(value, -0.8f, 0.8f);
            }

            return CreateClip(clipName, samples);
        }

        private static AudioClip CreateClip(
            string clipName,
            float[] samples)
        {
            AudioClip clip = AudioClip.Create(
                clipName,
                samples.Length,
                1,
                SampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
