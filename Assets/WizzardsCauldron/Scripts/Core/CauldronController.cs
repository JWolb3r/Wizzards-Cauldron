using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizzardsCauldron.Core
{
    public sealed class CauldronController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PuzzleDefinition _puzzleDefinition;

        [Header("Runtime State")]
        [SerializeField] private int _usedCapacity;
        [SerializeField] private int _totalHealth;
        [SerializeField] private bool _isLocked;

        private readonly HashSet<string> _acceptedPotionIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public PuzzleDefinition PuzzleDefinition => _puzzleDefinition;
        public int MaximumCapacity =>
            _puzzleDefinition != null
                ? _puzzleDefinition.CauldronCapacity
                : 0;

        public int UsedCapacity => _usedCapacity;
        public int RemainingCapacity =>
            Mathf.Max(0, MaximumCapacity - _usedCapacity);

        public int TotalHealth => _totalHealth;
        public bool IsLocked => _isLocked;

        public event Action TotalsChanged;

        private void Awake()
        {
            ClearRuntimeState();
        }

        public PotionAcceptanceResult TryAccept(
            PotionController potion)
        {
            if (potion == null || potion.Definition == null)
            {
                return PotionAcceptanceResult.InvalidPotion;
            }

            if (_isLocked)
            {
                return PotionAcceptanceResult.GameFinished;
            }

            PotionDefinition definition = potion.Definition;

            if (!ContainsPotion(definition))
            {
                return PotionAcceptanceResult.NotInPuzzle;
            }

            if (!potion.IsAvailable ||
                _acceptedPotionIds.Contains(definition.StableId))
            {
                return PotionAcceptanceResult.AlreadyUsed;
            }

            if (_usedCapacity + definition.FillValue >
                MaximumCapacity)
            {
                return PotionAcceptanceResult.TooFull;
            }

            _acceptedPotionIds.Add(definition.StableId);
            _usedCapacity += definition.FillValue;
            _totalHealth += definition.HealthValue;

            potion.MarkUsed();
            TotalsChanged?.Invoke();

            return PotionAcceptanceResult.Accepted;
        }

        public void Lock()
        {
            _isLocked = true;
        }

        public void ResetCauldron()
        {
            ClearRuntimeState();
            TotalsChanged?.Invoke();
        }

        public void Configure(PuzzleDefinition puzzleDefinition)
        {
            _puzzleDefinition = puzzleDefinition;
            ResetCauldron();
        }

        private bool ContainsPotion(PotionDefinition definition)
        {
            if (_puzzleDefinition == null)
            {
                return false;
            }

            IReadOnlyList<PotionDefinition> potions =
                _puzzleDefinition.Potions;

            for (int index = 0; index < potions.Count; index++)
            {
                if (potions[index] == definition)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearRuntimeState()
        {
            _acceptedPotionIds.Clear();
            _usedCapacity = 0;
            _totalHealth = 0;
            _isLocked = false;
        }
    }
}