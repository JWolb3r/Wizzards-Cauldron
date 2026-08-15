using TMPro;
using UnityEngine;

namespace WizzardsCauldron.Interactions
{
    [DisallowMultipleComponent]
    public sealed class ResetHoldControl : MonoBehaviour
    {
        [Header("Reset Target")]
        [SerializeField]
        private RoomResetCoordinator _roomReset;

        [Header("Hold Configuration")]
        [SerializeField]
        [Min(0.1f)]
        private float _holdDuration = 1f;

        [Header("Feedback")]
        [SerializeField]
        private TMP_Text _statusText;

        private bool _isHolding;
        private bool _awaitingRelease;
        private float _heldTime;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "ResetHoldControl is missing one or more " +
                    "required Inspector references.",
                    this);

                enabled = false;
                return;
            }

            ShowIdle();
        }

        private void OnValidate()
        {
            _holdDuration = Mathf.Max(
                0.1f,
                _holdDuration);
        }

        private void Update()
        {
            if (!_isHolding)
            {
                return;
            }

            _heldTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                _heldTime / _holdDuration);

            ShowHolding(progress);

            if (progress < 1f)
            {
                return;
            }

            CompleteHold();
        }

        public void BeginHold()
        {
            if (!isActiveAndEnabled ||
                _isHolding ||
                _awaitingRelease)
            {
                return;
            }

            _heldTime = 0f;
            _isHolding = true;

            ShowHolding(0f);
        }

        public void CancelHold()
        {
            _isHolding = false;
            _awaitingRelease = false;
            _heldTime = 0f;

            ShowIdle();
        }

        private void CompleteHold()
        {
            _isHolding = false;
            _awaitingRelease = true;
            _heldTime = _holdDuration;

            if (_roomReset.TryResetRoom())
            {
                _statusText.text =
                    "Room reset\nRelease button";

                Debug.Log(
                    "Room reset completed from the " +
                    "physical control.",
                    this);
            }
            else
            {
                _statusText.text =
                    "Reset failed\nRelease button";

                Debug.LogError(
                    "The physical reset control could not " +
                    "reset the room.",
                    this);
            }
        }

        private void ShowIdle()
        {
            if (_statusText != null)
            {
                _statusText.text =
                    "Hold to reset";
            }
        }

        private void ShowHolding(float progress)
        {
            if (_statusText == null)
            {
                return;
            }

            int percentage = Mathf.CeilToInt(
                progress * 100f);

            _statusText.text =
                $"Keep holding\n{percentage}%";
        }

        private bool HasRequiredReferences()
        {
            return
                _roomReset != null &&
                _statusText != null;
        }
    }
}