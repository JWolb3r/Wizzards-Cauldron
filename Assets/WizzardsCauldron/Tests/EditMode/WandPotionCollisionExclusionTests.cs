using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.Tests.EditMode
{
    public sealed class WandPotionCollisionExclusionTests
    {
        [Test]
        public void ApplyCollisionExclusions_IgnoresOnlyPotionColliders()
        {
            var wand = new GameObject("Wand");
            var potion = new GameObject("Potion");
            var table = new GameObject("Table");

            try
            {
                Collider wandCollider =
                    wand.AddComponent<CapsuleCollider>();
                Collider potionCollider =
                    potion.AddComponent<CapsuleCollider>();
                Collider tableCollider =
                    table.AddComponent<BoxCollider>();
                potion.AddComponent<PotionController>();

                WandPotionCollisionExclusion exclusion =
                    wand.AddComponent<WandPotionCollisionExclusion>();

                MethodInfo applyMethod =
                    typeof(WandPotionCollisionExclusion).GetMethod(
                        "ApplyCollisionExclusions",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(applyMethod, Is.Not.Null);
                int ignoredPairCount =
                    (int)applyMethod.Invoke(exclusion, null);

                Assert.That(ignoredPairCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(
                    Physics.GetIgnoreCollision(
                        wandCollider,
                        potionCollider),
                    Is.True);
                Assert.That(
                    Physics.GetIgnoreCollision(
                        wandCollider,
                        tableCollider),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(wand);
                Object.DestroyImmediate(potion);
                Object.DestroyImmediate(table);
            }
        }
    }
}
