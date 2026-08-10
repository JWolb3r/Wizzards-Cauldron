using System;
using System.Collections.Generic;

namespace WizzardsCauldron.Core
{
    public static class PuzzleSolver
    {
        public static PuzzleSolution Solve(
            int maximumCapacity,
            IReadOnlyList<KnapsackItem> items)
        {
            if (maximumCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCapacity),
                    maximumCapacity,
                    "Maximum capacity cannot be negative.");
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var states =
                new SolverState[maximumCapacity + 1];

            for (int capacity = 0;
                 capacity <= maximumCapacity;
                 capacity++)
            {
                states[capacity] = SolverState.Empty;
            }

            for (int itemIndex = 0;
                 itemIndex < items.Count;
                 itemIndex++)
            {
                KnapsackItem item = items[itemIndex];

                if (item.FillValue > maximumCapacity)
                {
                    continue;
                }

                for (int capacity = maximumCapacity;
                     capacity >= item.FillValue;
                     capacity--)
                {
                    SolverState previous =
                        states[capacity - item.FillValue];

                    SolverState candidate =
                        CreateCandidate(previous, item);

                    if (IsBetter(
                        candidate,
                        states[capacity]))
                    {
                        states[capacity] = candidate;
                    }
                }
            }

            SolverState best = states[maximumCapacity];

            return new PuzzleSolution(
                best.Health,
                best.UsedCapacity,
                best.SelectedPotionIds);
        }

        private static SolverState CreateCandidate(
            SolverState previous,
            KnapsackItem item)
        {
            var selectedIds = new List<string>(
                previous.SelectedPotionIds.Count + 1);

            for (int index = 0;
                 index < previous.SelectedPotionIds.Count;
                 index++)
            {
                selectedIds.Add(
                    previous.SelectedPotionIds[index]);
            }

            selectedIds.Add(item.StableId);

            return new SolverState(
                previous.Health + item.HealthValue,
                previous.UsedCapacity + item.FillValue,
                selectedIds);
        }

        private static bool IsBetter(
            SolverState candidate,
            SolverState current)
        {
            if (candidate.Health != current.Health)
            {
                return candidate.Health > current.Health;
            }

            if (candidate.UsedCapacity !=
                current.UsedCapacity)
            {
                return candidate.UsedCapacity <
                    current.UsedCapacity;
            }

            return false;
        }

        private sealed class SolverState
        {
            public static SolverState Empty { get; } =
                new SolverState(
                    0,
                    0,
                    Array.Empty<string>());

            public SolverState(
                int health,
                int usedCapacity,
                IReadOnlyList<string> selectedPotionIds)
            {
                Health = health;
                UsedCapacity = usedCapacity;
                SelectedPotionIds = selectedPotionIds;
            }

            public int Health { get; }
            public int UsedCapacity { get; }

            public IReadOnlyList<string>
                SelectedPotionIds
            { get; }
        }
    }
}