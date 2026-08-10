using System;

namespace WizzardsCauldron.Core
{
    public readonly struct KnapsackItem
    {
        public KnapsackItem(
            string stableId,
            int healthValue,
            int fillValue)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException(
                    "Stable ID cannot be empty.",
                    nameof(stableId));
            }

            if (healthValue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(healthValue),
                    healthValue,
                    "Health value cannot be negative.");
            }

            if (fillValue <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fillValue),
                    fillValue,
                    "Fill value must be positive.");
            }

            StableId = stableId.Trim();
            HealthValue = healthValue;
            FillValue = fillValue;
        }

        public string StableId { get; }
        public int HealthValue { get; }
        public int FillValue { get; }
    }
}