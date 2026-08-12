using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Interactions
{
    public sealed class WandActivator : MonoBehaviour
    {
        [Header("Gameplay Source")]
        [SerializeField]
        private GameSessionController _gameSession;

        [Header("Finish Trigger")]
        [SerializeField]
        private Collider _finishTrigger;

        private void OnEnable()
        {
            if (!HasRequiredReferences())
            {
                if (_finishTrigger != null)
                {
                    _finishTrigger.enabled = false;
                }

                Debug.LogError(
                    "WandActivator is missing one or more " +
                    "required Inspector references.",
                    this);

                return;
            }

            if (!_finishTrigger.isTrigger)
            {
                _finishTrigger.enabled = false;

                Debug.LogError(
                    "WandActivator requires its assigned " +
                    "collider to be a trigger.",
                    this);

                return;
            }

            _gameSession.AttemptFinished +=
                HandleAttemptFinished;

            SetTargetEnabled(_gameSession.IsPlaying);
        }

        private void OnDisable()
        {
            if (_gameSession != null)
            {
                _gameSession.AttemptFinished -=
                    HandleAttemptFinished;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_gameSession == null ||
                !_gameSession.IsPlaying)
            {
                SetTargetEnabled(false);
                return;
            }

            WandTip wandTip =
                other.GetComponent<WandTip>();

            if (wandTip == null)
            {
                return;
            }

            if (_gameSession.TryFinishAttempt(out _))
            {
                SetTargetEnabled(false);
            }
        }

        private void HandleAttemptFinished(
            AttemptResult _)
        {
            SetTargetEnabled(false);
        }

        private void SetTargetEnabled(
            bool targetEnabled)
        {
            if (_finishTrigger != null)
            {
                _finishTrigger.enabled =
                    targetEnabled;
            }
        }

        private bool HasRequiredReferences()
        {
            return
                _gameSession != null &&
                _finishTrigger != null;
        }
    }
}