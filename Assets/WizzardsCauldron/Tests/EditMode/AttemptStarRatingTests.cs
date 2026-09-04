using NUnit.Framework;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Tests.EditMode
{
    public sealed class AttemptStarRatingTests
    {
        [Test]
        public void Calculate_OptimalWithoutHint_ReturnsThreeStars()
        {
            AttemptResult result = CreateResult(10, 5, 5);

            Assert.That(
                AttemptStarRating.Calculate(result, false),
                Is.EqualTo(3));
        }

        [Test]
        public void Calculate_HintSubtractsOneStar()
        {
            AttemptResult result = CreateResult(10, 5, 5);

            Assert.That(
                AttemptStarRating.Calculate(result, true),
                Is.EqualTo(2));
        }

        [TestCase(7, 2)]
        [TestCase(6, 1)]
        public void Calculate_ValidSolutionUsesSeventyPercentThreshold(
            int playerHealth,
            int expectedStars)
        {
            AttemptResult result = CreateResult(
                playerHealth,
                4,
                5);

            Assert.That(
                AttemptStarRating.Calculate(result, false),
                Is.EqualTo(expectedStars));
        }

        [Test]
        public void Calculate_OverfilledAttemptReturnsZeroStars()
        {
            AttemptResult result = CreateResult(12, 6, 5);

            Assert.That(
                AttemptStarRating.Calculate(result, false),
                Is.Zero);
        }

        private static AttemptResult CreateResult(
            int playerHealth,
            int usedCapacity,
            int maximumCapacity)
        {
            PuzzleSolution optimal = PuzzleSolver.Solve(
                5,
                new[]
                {
                    new KnapsackItem("optimal", 10, 5)
                });

            return AttemptResult.Create(
                playerHealth,
                usedCapacity,
                maximumCapacity,
                optimal);
        }
    }
}
