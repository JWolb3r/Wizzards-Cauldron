using System.Collections;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.Presentation
{
    [DisallowMultipleComponent]
    public sealed class VisualFeedbackController : MonoBehaviour
    {
        [Header("Gameplay Sources (Read Only)")]
        [SerializeField]
        private CauldronIntake _cauldronIntake;

        [SerializeField]
        private GameSessionController _gameSession;

        [Header("Particle Feedback")]
        [SerializeField]
        private ParticleSystem _acceptedParticles;

        [SerializeField]
        private ParticleSystem _rejectedParticles;

        [SerializeField]
        private ParticleSystem _successParticles;

        [SerializeField]
        private ParticleSystem _resetParticles;

        [Header("Cauldron Accent")]
        [SerializeField]
        private Light _cauldronAccentLight;

        [SerializeField]
        private Renderer _runeRenderer;

        [SerializeField]
        private string _emissionColorProperty =
            "_EmissionColor";

        [Header("Feedback Colors")]
        [SerializeField]
        private Color _acceptedFallbackColor =
            new Color(0.1f, 0.85f, 1f, 1f);

        [SerializeField]
        private Color _rejectedColor =
            new Color(0.7f, 0.08f, 0.32f, 1f);

        [SerializeField]
        private Color _optimalGoldColor =
            new Color(1f, 0.63f, 0.15f, 1f);

        [SerializeField]
        private Color _optimalCyanColor =
            new Color(0.1f, 0.9f, 1f, 1f);

        [SerializeField]
        private Color _validAttemptColor =
            new Color(0.25f, 0.58f, 0.72f, 1f);

        [SerializeField]
        private Color _noPotionsColor =
            new Color(0.35f, 0.12f, 0.42f, 1f);

        [SerializeField]
        private Color _resetColor =
            new Color(0.2f, 0.75f, 1f, 1f);

        [Header("Pulse Timing")]
        [SerializeField, Min(0.05f)]
        private float _potionPulseDuration = 0.45f;

        [SerializeField, Min(0.05f)]
        private float _attemptPulseDuration = 1.1f;

        [SerializeField, Min(0.05f)]
        private float _resetPulseDuration = 0.65f;

        [Header("Pulse Strength")]
        [SerializeField, Min(0f)]
        private float _acceptedLightIntensity = 0.85f;

        [SerializeField, Min(0f)]
        private float _rejectedLightIntensity = 0.45f;

        [SerializeField, Min(0f)]
        private float _optimalLightIntensity = 1.15f;

        [SerializeField, Min(0f)]
        private float _validLightIntensity = 0.65f;

        [SerializeField, Min(0f)]
        private float _resetLightIntensity = 0.55f;

        [SerializeField, Min(0f)]
        private float _acceptedEmissionMultiplier = 1.35f;

        [SerializeField, Min(0f)]
        private float _rejectedEmissionMultiplier = 0.8f;

        [SerializeField, Min(0f)]
        private float _optimalEmissionMultiplier = 1.75f;

        [SerializeField, Min(0f)]
        private float _validEmissionMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float _resetEmissionMultiplier = 0.9f;

        private MaterialPropertyBlock _propertyBlock;

        private Coroutine _pulseRoutine;
        private bool _isSubscribed;
        private bool _baselineCaptured;
        private bool _hasRuneEmissionProperty;
        private int _emissionColorPropertyId;
        private float _baseLightIntensity;
        private Color _baseLightColor = Color.white;
        private Color _baseRuneEmissionColor = Color.black;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            CaptureVisualBaseline();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _isSubscribed)
            {
                return;
            }

            if (_cauldronIntake != null)
            {
                _cauldronIntake.PotionProcessed +=
                    HandlePotionProcessed;
            }

            if (_gameSession != null)
            {
                _gameSession.AttemptFinished +=
                    HandleAttemptFinished;

                _gameSession.SessionReset +=
                    HandleSessionReset;
            }

            _isSubscribed = true;
        }

        private void OnDisable()
        {
            UnsubscribeFromGameplayEvents();
            StopAllParticles();
            StopPulseAndRestoreBaseline();
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(
                _emissionColorProperty))
            {
                _emissionColorProperty =
                    "_EmissionColor";
            }

            _potionPulseDuration = Mathf.Max(
                0.05f,
                _potionPulseDuration);

            _attemptPulseDuration = Mathf.Max(
                0.05f,
                _attemptPulseDuration);

            _resetPulseDuration = Mathf.Max(
                0.05f,
                _resetPulseDuration);

            _acceptedLightIntensity = Mathf.Max(
                0f,
                _acceptedLightIntensity);

            _rejectedLightIntensity = Mathf.Max(
                0f,
                _rejectedLightIntensity);

            _optimalLightIntensity = Mathf.Max(
                0f,
                _optimalLightIntensity);

            _validLightIntensity = Mathf.Max(
                0f,
                _validLightIntensity);

            _resetLightIntensity = Mathf.Max(
                0f,
                _resetLightIntensity);

            _acceptedEmissionMultiplier = Mathf.Max(
                0f,
                _acceptedEmissionMultiplier);

            _rejectedEmissionMultiplier = Mathf.Max(
                0f,
                _rejectedEmissionMultiplier);

            _optimalEmissionMultiplier = Mathf.Max(
                0f,
                _optimalEmissionMultiplier);

            _validEmissionMultiplier = Mathf.Max(
                0f,
                _validEmissionMultiplier);

            _resetEmissionMultiplier = Mathf.Max(
                0f,
                _resetEmissionMultiplier);
        }

        private void HandlePotionProcessed(
            PotionController potion,
            PotionAcceptanceResult result)
        {
            StopParticleSystem(_acceptedParticles);
            StopParticleSystem(_rejectedParticles);

            if (result == PotionAcceptanceResult.Accepted)
            {
                Color potionColor = GetPotionColor(potion);

                PlayParticleSystem(
                    _acceptedParticles,
                    potionColor,
                    potionColor);

                StartPulse(
                    potionColor,
                    _acceptedLightIntensity,
                    _acceptedEmissionMultiplier,
                    _potionPulseDuration);

                return;
            }

            PlayParticleSystem(
                _rejectedParticles,
                _rejectedColor,
                _noPotionsColor);

            StartPulse(
                _rejectedColor,
                _rejectedLightIntensity,
                _rejectedEmissionMultiplier,
                _potionPulseDuration);
        }

        private void HandleAttemptFinished(
            AttemptResult result)
        {
            StopParticleSystem(_acceptedParticles);
            StopParticleSystem(_rejectedParticles);
            StopParticleSystem(_successParticles);

            if (result == null)
            {
                return;
            }

            switch (result.Outcome)
            {
                case AttemptOutcome.OptimalSolution:
                    PlayParticleSystem(
                        _successParticles,
                        _optimalGoldColor,
                        _optimalCyanColor);

                    StartPulse(
                        Color.Lerp(
                            _optimalGoldColor,
                            _optimalCyanColor,
                            0.45f),
                        _optimalLightIntensity,
                        _optimalEmissionMultiplier,
                        _attemptPulseDuration);
                    break;

                case AttemptOutcome.ValidSolution:
                    PlayParticleSystem(
                        _successParticles,
                        _validAttemptColor,
                        _optimalCyanColor);

                    StartPulse(
                        _validAttemptColor,
                        _validLightIntensity,
                        _validEmissionMultiplier,
                        _attemptPulseDuration * 0.75f);
                    break;

                case AttemptOutcome.NoPotionsSelected:
                    PlayParticleSystem(
                        _rejectedParticles,
                        _noPotionsColor,
                        _rejectedColor);

                    StartPulse(
                        _noPotionsColor,
                        _rejectedLightIntensity * 0.65f,
                        _rejectedEmissionMultiplier * 0.65f,
                        _potionPulseDuration);
                    break;
            }
        }

        private void HandleSessionReset()
        {
            StopAllParticles();
            StopPulseAndRestoreBaseline();

            PlayParticleSystem(
                _resetParticles,
                _resetColor,
                _acceptedFallbackColor);

            StartPulse(
                _resetColor,
                _resetLightIntensity,
                _resetEmissionMultiplier,
                _resetPulseDuration);
        }

        private void StartPulse(
            Color pulseColor,
            float lightIntensity,
            float emissionMultiplier,
            float duration)
        {
            StopPulseAndRestoreBaseline();

            _pulseRoutine = StartCoroutine(
                RunPulse(
                    pulseColor,
                    lightIntensity,
                    emissionMultiplier,
                    duration));
        }

        private IEnumerator RunPulse(
            Color pulseColor,
            float lightIntensity,
            float emissionMultiplier,
            float duration)
        {
            float elapsed = 0f;
            float targetLightIntensity = Mathf.Max(
                _baseLightIntensity,
                lightIntensity);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / duration);

                float strength = Mathf.Sin(
                    progress * Mathf.PI);

                ApplyPulseStrength(
                    pulseColor,
                    targetLightIntensity,
                    emissionMultiplier,
                    strength);

                yield return null;
            }

            RestoreVisualBaseline();
            _pulseRoutine = null;
        }

        private void ApplyPulseStrength(
            Color pulseColor,
            float targetLightIntensity,
            float emissionMultiplier,
            float strength)
        {
            if (_cauldronAccentLight != null)
            {
                _cauldronAccentLight.color = Color.Lerp(
                    _baseLightColor,
                    pulseColor,
                    strength);

                _cauldronAccentLight.intensity = Mathf.Lerp(
                    _baseLightIntensity,
                    targetLightIntensity,
                    strength);
            }

            if (!_hasRuneEmissionProperty ||
                _runeRenderer == null)
            {
                return;
            }

            Color targetEmission =
                pulseColor * emissionMultiplier;

            SetRuneEmission(Color.Lerp(
                _baseRuneEmissionColor,
                targetEmission,
                strength));
        }

        private void CaptureVisualBaseline()
        {
            EnsurePropertyBlock();
            _emissionColorPropertyId = Shader.PropertyToID(
                _emissionColorProperty);

            if (_cauldronAccentLight != null)
            {
                _baseLightIntensity =
                    _cauldronAccentLight.intensity;

                _baseLightColor =
                    _cauldronAccentLight.color;
            }

            _hasRuneEmissionProperty = false;

            if (_runeRenderer == null ||
                _runeRenderer.sharedMaterial == null)
            {
                _baselineCaptured = true;
                return;
            }

            if (!_runeRenderer.sharedMaterial.HasProperty(
                _emissionColorPropertyId))
            {
                _baselineCaptured = true;
                return;
            }

            _hasRuneEmissionProperty = true;
            _runeRenderer.GetPropertyBlock(_propertyBlock);

            _baseRuneEmissionColor =
                _propertyBlock.HasColor(_emissionColorPropertyId)
                    ? _propertyBlock.GetColor(
                        _emissionColorPropertyId)
                    : _runeRenderer.sharedMaterial.GetColor(
                        _emissionColorPropertyId);

            _baselineCaptured = true;
        }

        private void SetRuneEmission(Color color)
        {
            if (_runeRenderer == null)
            {
                return;
            }

            EnsurePropertyBlock();
            _runeRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(
                _emissionColorPropertyId,
                color);
            _runeRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void EnsurePropertyBlock()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void StopPulseAndRestoreBaseline()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            RestoreVisualBaseline();
        }

        private void RestoreVisualBaseline()
        {
            if (!_baselineCaptured)
            {
                return;
            }

            if (_cauldronAccentLight != null)
            {
                _cauldronAccentLight.color =
                    _baseLightColor;

                _cauldronAccentLight.intensity =
                    _baseLightIntensity;
            }

            if (_hasRuneEmissionProperty)
            {
                SetRuneEmission(_baseRuneEmissionColor);
            }
        }

        private void UnsubscribeFromGameplayEvents()
        {
            if (_cauldronIntake != null)
            {
                _cauldronIntake.PotionProcessed -=
                    HandlePotionProcessed;
            }

            if (_gameSession != null)
            {
                _gameSession.AttemptFinished -=
                    HandleAttemptFinished;

                _gameSession.SessionReset -=
                    HandleSessionReset;
            }

            _isSubscribed = false;
        }

        private Color GetPotionColor(
            PotionController potion)
        {
            if (potion == null || potion.Definition == null)
            {
                return _acceptedFallbackColor;
            }

            Color presentationColor =
                potion.Definition.PresentationColor;

            presentationColor.a = 1f;
            return presentationColor;
        }

        private static void PlayParticleSystem(
            ParticleSystem particles,
            Color firstColor,
            Color secondColor)
        {
            if (particles == null)
            {
                return;
            }

            ParticleSystem.MainModule main = particles.main;
            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    firstColor,
                    secondColor);

            particles.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear);

            particles.Play(true);
        }

        private void StopAllParticles()
        {
            StopParticleSystem(_acceptedParticles);
            StopParticleSystem(_rejectedParticles);
            StopParticleSystem(_successParticles);
            StopParticleSystem(_resetParticles);
        }

        private static void StopParticleSystem(
            ParticleSystem particles)
        {
            if (particles == null)
            {
                return;
            }

            particles.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear);
        }
    }
}
