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
        [SerializeField] private PotionDefinition _definition;

        [Header("Runtime State")]
        [SerializeField] private PotionState _state = PotionState.Available;

        public PotionDefinition Definition => _definition;
        public PotionState State => _state;
        public bool IsAvailable => _state == PotionState.Available;

        private void Awake()
        {
            _state = PotionState.Available;
        }

        public void MarkUsed()
        {
            _state = PotionState.Used;
        }

        public void Lock()
        {
            _state = PotionState.Locked;
        }

        public void ResetState()
        {
            _state = PotionState.Available;
        }
    }
}