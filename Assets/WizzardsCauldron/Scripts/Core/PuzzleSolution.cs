using System;
using System.Collections.Generic;

namespace WizzardsCauldron.Core
{
    public sealed class PuzzleSolution
    {
        private readonly IReadOnlyList<string>
            _selectedPotionIds;

        internal PuzzleSolution(
            int maximumHealth,
            int usedCapacity,
            IReadOnlyList<string> selectedPotionIds)
        {
            MaximumHealth = maximumHealth;
            UsedCapacity = usedCapacity;

            var copiedIds =
                new string[selectedPotionIds.Count];

            for (int index = 0;
                 index < selectedPotionIds.Count;
                 index++)
            {
                copiedIds[index] = selectedPotionIds[index];
            }

            _selectedPotionIds =
                Array.AsReadOnly(copiedIds);
        }

        public int MaximumHealth { get; }
        public int UsedCapacity { get; }

        public IReadOnlyList<string> SelectedPotionIds =>
            _selectedPotionIds;
    }
}