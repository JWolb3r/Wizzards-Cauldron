using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Interactions
{
    [DisallowMultipleComponent]
    public sealed class RoomResetCoordinator : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField]
        private GameSessionController _gameSession;

        [SerializeField]
        private CauldronIntake _cauldronIntake;

        [Header("Potion State")]
        [SerializeField]
        private PotionController[] _potions =
            new PotionController[0];

        [Header("Physical Objects")]
        [SerializeField]
        private PhysicsResettable[] _physicsObjects =
            new PhysicsResettable[0];

        public bool TryResetRoom()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "RoomResetCoordinator is missing one or " +
                    "more required Inspector references.",
                    this);

                return false;
            }

            _cauldronIntake.ResetTracking();

            // Consumed potions have disabled colliders and interaction.
            // Restore their poses while they are still inert so physics or
            // XR cannot act on them at the cauldron before they reappear.
            for (int index = 0;
                 index < _physicsObjects.Length;
                 index++)
            {
                _physicsObjects[index]
                    .RestoreInitialPose();
            }

            for (int index = 0;
                 index < _potions.Length;
                 index++)
            {
                _potions[index].ResetState();
            }

            if (!_gameSession.TryResetSession())
            {
                Debug.LogError(
                    "The game session could not be reset.",
                    this);

                return false;
            }

            return true;
        }

        [ContextMenu("Reset Room (Play Mode)")]
        private void ResetRoomFromContextMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.Log(
                    "Reset Room is only available during " +
                    "Play Mode.",
                    this);

                return;
            }

            if (TryResetRoom())
            {
                Debug.Log("Room reset complete.", this);
            }
        }

        private bool HasRequiredReferences()
        {
            if (_gameSession == null ||
                _cauldronIntake == null ||
                _potions == null ||
                _potions.Length == 0 ||
                _physicsObjects == null ||
                _physicsObjects.Length == 0)
            {
                return false;
            }

            for (int index = 0;
                 index < _potions.Length;
                 index++)
            {
                if (_potions[index] == null)
                {
                    return false;
                }
            }

            for (int index = 0;
                 index < _physicsObjects.Length;
                 index++)
            {
                if (_physicsObjects[index] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
