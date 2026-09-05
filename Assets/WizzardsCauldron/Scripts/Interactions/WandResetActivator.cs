using System.Collections;
using TMPro;
using UnityEngine;

namespace WizzardsCauldron.Interactions
{
    /// <summary>
    /// Resets the room only when the existing WandTip enters the dedicated
    /// rune trigger. Legacy controller selection is disabled at runtime but
    /// retained in the prefab for reversibility.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class WandResetActivator : MonoBehaviour
    {
        [Header("Reset source")]
        [SerializeField]
        private RoomResetCoordinator _roomReset;

        [SerializeField]
        private Collider _wandTrigger;

        [Header("Legacy controller interaction")]
        [SerializeField]
        private Behaviour _legacyHoldControl;

        [SerializeField]
        private Behaviour _legacyInteractable;

        [Header("Presentation")]
        [SerializeField]
        private TMP_Text _statusText;

        [SerializeField]
        private Transform _runeFacingRoot;

        [SerializeField]
        private Transform _pulseRoot;

        [SerializeField]
        private Transform _outerRune;

        [SerializeField]
        private Transform _innerRune;

        [SerializeField]
        private ParticleSystem _activationParticles;

        [SerializeField]
        private Light _activationLight;

        [SerializeField, Min(0.1f)]
        private float _activationDuration = 0.9f;

        private Coroutine _feedbackRoutine;
        private Vector3 _baseScale = Vector3.one;
        private float _baseLightIntensity;
        private bool _awaitingWandExit;

        private void Awake()
        {
            DisableLegacyControllerInteraction();

            if (_pulseRoot != null)
            {
                _baseScale = _pulseRoot.localScale;
            }

            if (_activationLight != null)
            {
                _baseLightIntensity = _activationLight.intensity;
            }

            if (_roomReset == null || _wandTrigger == null ||
                !_wandTrigger.isTrigger)
            {
                if (_wandTrigger != null)
                {
                    _wandTrigger.enabled = false;
                }

                Debug.LogError(
                    "WandResetActivator is missing its reset source or wand trigger.",
                    this);
                enabled = false;
                return;
            }

            ShowIdleMessage();
            RestoreVisuals();
        }

        private void Start()
        {
            // Run once more after every component's Awake so the legacy status
            // text and controller interactable cannot overwrite wand mode.
            DisableLegacyControllerInteraction();
            ShowIdleMessage();
        }

        private void LateUpdate()
        {
            FaceMainCamera();

            if (_feedbackRoutine == null)
            {
                float delta = Time.unscaledDeltaTime;
                if (_outerRune != null)
                {
                    _outerRune.Rotate(0f, 0f, 14f * delta, Space.Self);
                }
                if (_innerRune != null)
                {
                    _innerRune.Rotate(0f, 0f, -20f * delta, Space.Self);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_awaitingWandExit ||
                other.GetComponent<WandTip>() == null)
            {
                return;
            }

            _awaitingWandExit = true;
            bool resetSucceeded = _roomReset.TryResetRoom();

            if (_statusText != null)
            {
                _statusText.text = resetSucceeded
                    ? "Reset spell accepted"
                    : "Reset spell unavailable";
            }

            PlayFeedback(resetSucceeded);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<WandTip>() == null)
            {
                return;
            }

            _awaitingWandExit = false;
            ShowIdleMessage();
        }

        private void OnDisable()
        {
            StopFeedback();
        }

        private void PlayFeedback(bool resetSucceeded)
        {
            StopFeedback();

            if (resetSucceeded && _activationParticles != null)
            {
                _activationParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                _activationParticles.Play(true);
            }

            _feedbackRoutine = StartCoroutine(
                RunFeedback(resetSucceeded));
        }

        private IEnumerator RunFeedback(bool resetSucceeded)
        {
            float elapsed = 0f;
            float scalePeak = resetSucceeded ? 1.42f : 1.15f;
            float lightPeak = resetSucceeded ? 0.75f : 0.2f;

            while (elapsed < _activationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / _activationDuration);
                float wave = Mathf.Sin(progress * Mathf.PI);

                if (_pulseRoot != null)
                {
                    _pulseRoot.localScale = _baseScale *
                        Mathf.Lerp(1f, scalePeak, wave);
                }

                if (_activationLight != null)
                {
                    _activationLight.intensity = Mathf.Lerp(
                        _baseLightIntensity,
                        lightPeak,
                        wave);
                }

                if (_outerRune != null)
                {
                    _outerRune.Rotate(
                        0f,
                        0f,
                        190f * Time.unscaledDeltaTime,
                        Space.Self);
                }

                if (_innerRune != null)
                {
                    _innerRune.Rotate(
                        0f,
                        0f,
                        -260f * Time.unscaledDeltaTime,
                        Space.Self);
                }

                yield return null;
            }

            RestoreVisuals();
            _feedbackRoutine = null;
        }

        private void StopFeedback()
        {
            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = null;
            }

            if (_activationParticles != null)
            {
                _activationParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            RestoreVisuals();
        }

        private void RestoreVisuals()
        {
            if (_pulseRoot != null)
            {
                _pulseRoot.localScale = _baseScale;
            }

            if (_activationLight != null)
            {
                _activationLight.intensity = _baseLightIntensity;
            }
        }

        private void ShowIdleMessage()
        {
            if (_statusText != null)
            {
                _statusText.text = "Touch with wand to reset";
            }
        }

        private void DisableLegacyControllerInteraction()
        {
            if (_legacyInteractable != null)
            {
                _legacyInteractable.enabled = false;
            }

            if (_legacyHoldControl != null)
            {
                _legacyHoldControl.enabled = false;
            }
        }

        private void FaceMainCamera()
        {
            Camera camera = Camera.main;
            if (_runeFacingRoot == null || camera == null)
            {
                return;
            }

            Vector3 direction =
                camera.transform.position - _runeFacingRoot.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                _runeFacingRoot.rotation = Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
            }
        }
    }
}
