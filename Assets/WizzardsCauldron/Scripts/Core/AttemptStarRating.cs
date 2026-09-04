using System;

namespace WizzardsCauldron.Core
{
    public static class AttemptStarRating
    {
        private const int MaximumStars = 3;
        private const int StrongSolutionPercentage = 70;

        public static int Calculate(
            AttemptResult result,
            bool hintUsed)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            int stars;
            switch (result.Outcome)
            {
                case AttemptOutcome.OptimalSolution:
                    stars = MaximumStars;
                    break;

                case AttemptOutcome.ValidSolution:
                    stars = IsStrongSolution(result)
                        ? 2
                        : 1;
                    break;

                default:
                    stars = 0;
                    break;
            }

            if (hintUsed)
            {
                stars--;
            }

            return Math.Max(0, stars);
        }

        private static bool IsStrongSolution(
            AttemptResult result)
        {
            if (result.BestPossibleHealth <= 0)
            {
                return false;
            }

            return
                result.PlayerHealth * 100 >=
                result.BestPossibleHealth *
                StrongSolutionPercentage;
        }
    }
}
