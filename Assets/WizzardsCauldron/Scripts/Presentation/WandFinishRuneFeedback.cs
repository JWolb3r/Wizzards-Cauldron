using System.Collections;
using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Presentation
{
    /// <summary>
    /// Presentation-only feedback for the existing wand finish trigger.
    /// It observes session events without changing evaluation or XR input.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WandFinishRuneFeedback : MonoBehaviour
    {
        [Header("Read-only gameplay source")]
        [SerializeField]
        private GameSessionController _gameSession;

        [Header("Rune presentation")]
        [SerializeField]
        private Transform _pulseRoot;

        [SerializeField]
        private Transform _outerRing;

        [SerializeField]
        private Transform _innerRing;

        [SerializeField]
        private ParticleSystem _activationParticles;

        [SerializeField]
        private Light _activationLight;

        [Header("Motion")]
        [SerializeField, Min(0f)]
        private float _idleOuterDegreesPerSecond = 12f;

        [SerializeField, Min(0f)]
        private float _idleInnerDegreesPerSecond = 18f;

        [SerializeField, Min(0.1f)]
        private float _activationDuration = 1.05f;

        [SerializeField, Min(0f)]
        private float _activationLightIntensity = 0.8f;

        private Coroutine _activationRoutine;
        private Vector3 _basePulseScale = Vector3.one;
        private float _baseLightIntensity;
        private bool _subscribed;

        private void Awake()
        {
            CaptureBaseline();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _subscribed ||
                _gameSession == null)
            {
                return;
            }

            _gameSession.AttemptFinished += HandleAttemptFinished;
            _gameSession.SessionReset += HandleSessionReset;
            _subscribed = true;
            RestoreBaseline();
        }

        private void OnDisable()
        {
            if (_gameSession != null && _subscribed)
            {
                _gameSession.AttemptFinished -= HandleAttemptFinished;
                _gameSession.SessionReset -= HandleSessionReset;
            }

            _subscribed = false;
            StopFeedback();
        }

        private void Update()
        {
            if (!Application.isPlaying || _gameSession == null ||
                !_gameSession.IsPlaying)
            {
                return;
            }

            float speedMultiplier = _activationRoutine == null ? 1f : 5f;
            float delta = Time.unscaledDeltaTime * speedMultiplier;

            if (_outerRing != null)
            {
                _outerRing.Rotate(
                    0f,
                    0f,
                    _idleOuterDegreesPerSecond * delta,
                    Space.Self);
            }

            if (_innerRing != null)
            {
                _innerRing.Rotate(
                    0f,
                    0f,
                    -_idleInnerDegreesPerSecond * delta,
                    Space.Self);
            }
        }

        private void HandleAttemptFinished(AttemptResult _)
        {
            StopFeedback();

            if (_activationParticles != null)
            {
                _activationParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                _activationParticles.Play(true);
            }

            _activationRoutine = StartCoroutine(RunActivationPulse());
        }

        private void HandleSessionReset()
        {
            StopFeedback();
        }

        private IEnumerator RunActivationPulse()
        {
            float elapsed = 0f;

            while (elapsed < _activationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / _activationDuration);
                float wave = Mathf.Sin(progress * Mathf.PI);

                if (_pulseRoot != null)
                {
                    _pulseRoot.localScale = _basePulseScale *
                        Mathf.Lerp(1f, 1.38f, wave);
                }

                if (_activationLight != null)
                {
                    _activationLight.intensity = Mathf.Lerp(
                        _baseLightIntensity,
                        _activationLightIntensity,
                        wave);
                }

                yield return null;
            }

            RestoreBaseline();
            _activationRoutine = null;
        }

        private void CaptureBaseline()
        {
            if (_pulseRoot != null)
            {
                _basePulseScale = _pulseRoot.localScale;
            }

            if (_activationLight != null)
            {
                _baseLightIntensity = _activationLight.intensity;
            }
        }

        private void StopFeedback()
        {
            if (_activationRoutine != null)
            {
                StopCoroutine(_activationRoutine);
                _activationRoutine = null;
            }

            if (_activationParticles != null)
            {
                _activationParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            RestoreBaseline();
        }

        private void RestoreBaseline()
        {
            if (_pulseRoot != null)
            {
                _pulseRoot.localScale = _basePulseScale;
            }

            if (_activationLight != null)
            {
                _activationLight.intensity = _baseLightIntensity;
            }
        }
    }
}
