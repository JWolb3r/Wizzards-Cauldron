using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizzardsCauldron.Core
{
    [CreateAssetMenu(
        fileName = "SO_Puzzle",
        menuName = "Wizzards Cauldron/Puzzle Definition")]
    public sealed class PuzzleDefinition : ScriptableObject
    {
        [Header("Rules")]
        [SerializeField, Min(0)] private int _cauldronCapacity;

        [Header("Available Potions")]
        [SerializeField] private List<PotionDefinition> _potions =
            new List<PotionDefinition>();

        public int CauldronCapacity => _cauldronCapacity;
        public IReadOnlyList<PotionDefinition> Potions => _potions;

        private void OnValidate()
        {
            _cauldronCapacity = Mathf.Max(0, _cauldronCapacity);
        }

        public bool TryValidate(out string error)
        {
            if (_cauldronCapacity < 0)
            {
                error = "Cauldron capacity cannot be negative.";
                return false;
            }

            var stableIds =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < _potions.Count; index++)
            {
                PotionDefinition potion = _potions[index];

                if (potion == null)
                {
                    error = $"Potion slot {index} has no definition.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(potion.StableId))
                {
                    error =
                        $"Potion slot {index} has an empty stable ID.";
                    return false;
                }

                if (!stableIds.Add(potion.StableId))
                {
                    error =
                        $"Stable ID '{potion.StableId}' appears more than once.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        [ContextMenu("Validate Puzzle")]
        private void ValidatePuzzle()
        {
            if (TryValidate(out string error))
            {
                Debug.Log(
                    $"Puzzle '{name}' is valid.",
                    this);
                return;
            }

            Debug.LogWarning(
                $"Puzzle '{name}' is invalid: {error}",
                this);
        }
    }
}