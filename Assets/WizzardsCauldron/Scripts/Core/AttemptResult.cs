using System;
using System.Collections.Generic;

namespace WizzardsCauldron.Core
{
    public sealed class AttemptResult
    {
        private readonly IReadOnlyList<string>
            _optimalPotionIds;

        private AttemptResult(
            int playerHealth,
            int usedCapacity,
            int maximumCapacity,
            int bestPossibleHealth,
            AttemptOutcome outcome,
            IReadOnlyList<string> optimalPotionIds)
        {
            PlayerHealth = playerHealth;
            UsedCapacity = usedCapacity;
            MaximumCapacity = maximumCapacity;
            BestPossibleHealth = bestPossibleHealth;
            Outcome = outcome;

            var copiedIds =
                new string[optimalPotionIds.Count];

            for (int index = 0;
                 index < optimalPotionIds.Count;
                 index++)
            {
                copiedIds[index] = optimalPotionIds[index];
            }

            _optimalPotionIds =
                Array.AsReadOnly(copiedIds);
        }

        public int PlayerHealth { get; }
        public int UsedCapacity { get; }
        public int MaximumCapacity { get; }
        public int BestPossibleHealth { get; }
        public AttemptOutcome Outcome { get; }

        public IReadOnlyList<string> OptimalPotionIds =>
            _optimalPotionIds;

        public static AttemptResult Create(
            int playerHealth,
            int usedCapacity,
            int maximumCapacity,
            PuzzleSolution optimalSolution)
        {
            if (playerHealth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerHealth));
            }

            if (usedCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(usedCapacity));
            }

            if (maximumCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCapacity));
            }

            if (usedCapacity > maximumCapacity)
            {
                throw new ArgumentException(
                    "Used capacity cannot exceed " +
                    "maximum capacity.",
                    nameof(usedCapacity));
            }

            if (optimalSolution == null)
            {
                throw new ArgumentNullException(
                    nameof(optimalSolution));
            }

            AttemptOutcome outcome =
                DetermineOutcome(
                    playerHealth,
                    usedCapacity,
                    optimalSolution.MaximumHealth);

            return new AttemptResult(
                playerHealth,
                usedCapacity,
                maximumCapacity,
                optimalSolution.MaximumHealth,
                outcome,
                optimalSolution.SelectedPotionIds);
        }

        private static AttemptOutcome DetermineOutcome(
            int playerHealth,
            int usedCapacity,
            int bestPossibleHealth)
        {
            if (usedCapacity == 0)
            {
                return AttemptOutcome.NoPotionsSelected;
            }

            if (playerHealth == bestPossibleHealth)
            {
                return AttemptOutcome.OptimalSolution;
            }

            return AttemptOutcome.ValidSolution;
        }
    }
}