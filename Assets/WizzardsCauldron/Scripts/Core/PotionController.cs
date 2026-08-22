using System;
using UnityEngine;

namespace WizzardsCauldron.Core
{
    public enum PotionState
    {
        Available,
        Used,
        Locked
    }

    public sealed class PotionController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private PotionDefinition _definition;

        [Header("Runtime State")]
        [SerializeField]
        private PotionState _state =
            PotionState.Available;

        public PotionDefinition Definition =>
            _definition;

        public PotionState State =>
            _state;

        public bool IsAvailable =>
            _state == PotionState.Available;

        public event Action StateChanged;

        private void Awake()
        {
            SetState(PotionState.Available);
        }

        public void MarkUsed()
        {
            SetState(PotionState.Used);
        }

        public void Lock()
        {
            SetState(PotionState.Locked);
        }

        public void ResetState()
        {
            SetState(PotionState.Available);
        }

        private void SetState(PotionState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            StateChanged?.Invoke();
        }
    }
}