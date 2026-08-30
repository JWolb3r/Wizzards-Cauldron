using NUnit.Framework;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Tests.EditMode
{
    public sealed class AttemptResultTests
    {
        private static PuzzleSolution
            CreateIntroSolution()
        {
            var items = new[]
            {
                new KnapsackItem("red", 1, 2),
                new KnapsackItem("green", 2, 2),
                new KnapsackItem("yellow", 3, 2)
            };

            return PuzzleSolver.Solve(4, items);
        }

        [Test]
        public void Create_NoSelection_ReturnsNoPotionsOutcome()
        {
            PuzzleSolution optimal =
                CreateIntroSolution();

            AttemptResult result =
                AttemptResult.Create(
                    0,
                    0,
                    4,
                    optimal);

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AttemptOutcome.NoPotionsSelected));

            Assert.That(
                result.PlayerHealth,
                Is.Zero);

            Assert.That(
                result.BestPossibleHealth,
                Is.EqualTo(5));
        }

        [Test]
        public void Create_NonOptimalSelection_ReturnsValidOutcome()
        {
            PuzzleSolution optimal =
                CreateIntroSolution();

            AttemptResult result =
                AttemptResult.Create(
                    1,
                    2,
                    4,
                    optimal);

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AttemptOutcome.ValidSolution));

            Assert.That(
                result.PlayerHealth,
                Is.EqualTo(1));

            Assert.That(
                result.BestPossibleHealth,
                Is.EqualTo(5));
        }

        [Test]
        public void Create_OptimalSelection_ReturnsOptimalOutcome()
        {
            PuzzleSolution optimal =
                CreateIntroSolution();

            AttemptResult result =
                AttemptResult.Create(
                    5,
                    4,
                    4,
                    optimal);

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AttemptOutcome.OptimalSolution));

            Assert.That(
                result.PlayerHealth,
                Is.EqualTo(5));
        }

        [Test]
        public void Create_OverfilledSelection_ReturnsOverfilledOutcome()
        {
            PuzzleSolution optimal =
                CreateIntroSolution();

            AttemptResult result =
                AttemptResult.Create(
                    8,
                    6,
                    4,
                    optimal);

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AttemptOutcome.Overfilled));

            Assert.That(
                result.UsedCapacity,
                Is.EqualTo(6));
        }

        [Test]
        public void Create_BestHealthWithWrongFill_IsNotOptimal()
        {
            PuzzleSolution optimal =
                CreateIntroSolution();

            AttemptResult result =
                AttemptResult.Create(
                    5,
                    3,
                    4,
                    optimal);

            Assert.That(
                result.Outcome,
                Is.EqualTo(
                    AttemptOutcome.ValidSolution));
        }

        [Test]
        public void Create_CopiesOptimalPotionIds()
        {
            PuzzleSolution optimal =
                CreateIntroSolution();

            AttemptResult result =
                AttemptResult.Create(
                    5,
                    4,
                    4,
                    optimal);

            CollectionAssert.AreEqual(
                new[] { "green", "yellow" },
                result.OptimalPotionIds);
        }
    }
}
