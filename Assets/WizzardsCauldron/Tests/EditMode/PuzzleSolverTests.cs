using NUnit.Framework;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Tests.EditMode
{
    public sealed class PuzzleSolverTests
    {
        [Test]
        public void Solve_IntroPuzzle_ReturnsGreenAndYellow()
        {
            var items = new[]
            {
                new KnapsackItem("red", 1, 2),
                new KnapsackItem("green", 2, 2),
                new KnapsackItem("yellow", 3, 2)
            };

            PuzzleSolution result =
                PuzzleSolver.Solve(4, items);

            Assert.That(
                result.MaximumHealth,
                Is.EqualTo(5));

            Assert.That(
                result.UsedCapacity,
                Is.EqualTo(4));

            CollectionAssert.AreEqual(
                new[] { "green", "yellow" },
                result.SelectedPotionIds);
        }

        [Test]
        public void Solve_ExactFit_SelectsItem()
        {
            var items = new[]
            {
                new KnapsackItem("exact", 7, 5)
            };

            PuzzleSolution result =
                PuzzleSolver.Solve(5, items);

            Assert.That(
                result.MaximumHealth,
                Is.EqualTo(7));

            Assert.That(
                result.UsedCapacity,
                Is.EqualTo(5));

            CollectionAssert.AreEqual(
                new[] { "exact" },
                result.SelectedPotionIds);
        }

        [Test]
        public void Solve_OverweightItem_DoesNotSelectIt()
        {
            var items = new[]
            {
                new KnapsackItem("heavy", 100, 6)
            };

            PuzzleSolution result =
                PuzzleSolver.Solve(5, items);

            Assert.That(
                result.MaximumHealth,
                Is.Zero);

            Assert.That(
                result.UsedCapacity,
                Is.Zero);

            Assert.That(
                result.SelectedPotionIds,
                Is.Empty);
        }

        [Test]
        public void Solve_EmptyInput_ReturnsEmptySolution()
        {
            PuzzleSolution result =
                PuzzleSolver.Solve(
                    4,
                    System.Array.Empty<KnapsackItem>());

            Assert.That(
                result.MaximumHealth,
                Is.Zero);

            Assert.That(
                result.UsedCapacity,
                Is.Zero);

            Assert.That(
                result.SelectedPotionIds,
                Is.Empty);
        }

        [Test]
        public void Solve_ZeroCapacity_ReturnsEmptySolution()
        {
            var items = new[]
            {
                new KnapsackItem("red", 1, 2)
            };

            PuzzleSolution result =
                PuzzleSolver.Solve(0, items);

            Assert.That(
                result.MaximumHealth,
                Is.Zero);

            Assert.That(
                result.UsedCapacity,
                Is.Zero);

            Assert.That(
                result.SelectedPotionIds,
                Is.Empty);
        }

        [Test]
        public void Solve_SingleItem_DoesNotReuseIt()
        {
            var items = new[]
            {
                new KnapsackItem("single", 3, 2)
            };

            PuzzleSolution result =
                PuzzleSolver.Solve(4, items);

            Assert.That(
                result.MaximumHealth,
                Is.EqualTo(3));

            Assert.That(
                result.UsedCapacity,
                Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[] { "single" },
                result.SelectedPotionIds);
        }

        [Test]
        public void Solve_EqualHealth_PrefersLowerCapacity()
        {
            var items = new[]
            {
                new KnapsackItem("heavy", 10, 3),
                new KnapsackItem("light", 10, 2)
            };

            PuzzleSolution result =
                PuzzleSolver.Solve(3, items);

            Assert.That(
                result.MaximumHealth,
                Is.EqualTo(10));

            Assert.That(
                result.UsedCapacity,
                Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[] { "light" },
                result.SelectedPotionIds);
        }

        [Test]
        public void Solve_DoesNotReorderInput()
        {
            var items = new[]
            {
                new KnapsackItem("first", 1, 1),
                new KnapsackItem("second", 2, 2),
                new KnapsackItem("third", 3, 3)
            };

            PuzzleSolver.Solve(4, items);

            Assert.That(
                items[0].StableId,
                Is.EqualTo("first"));

            Assert.That(
                items[1].StableId,
                Is.EqualTo("second"));

            Assert.That(
                items[2].StableId,
                Is.EqualTo("third"));
        }
    }
}