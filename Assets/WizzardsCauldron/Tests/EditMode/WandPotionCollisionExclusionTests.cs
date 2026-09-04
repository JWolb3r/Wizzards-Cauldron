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

        [Test]
        public void WandGrabAssist_AddsOnlyAnInvisibleTriggerVolume()
        {
            var wand = new GameObject("Wand");
            var physicalShape = new GameObject("PhysicalShape");
            physicalShape.transform.SetParent(wand.transform, false);

            try
            {
                wand.AddComponent<Rigidbody>();
                CapsuleCollider physicalCollider =
                    physicalShape.AddComponent<CapsuleCollider>();
                physicalCollider.isTrigger = false;

                System.Type grabType = System.Type.GetType(
                    "UnityEngine.XR.Interaction.Toolkit.Interactables." +
                    "XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                Assert.That(grabType, Is.Not.Null);

                Component grab = wand.AddComponent(grabType);
                PropertyInfo collidersProperty =
                    grabType.GetProperty("colliders");
                Assert.That(collidersProperty, Is.Not.Null);

                var colliders =
                    (System.Collections.IList)collidersProperty.GetValue(
                        grab);
                colliders.Clear();
                colliders.Add(physicalCollider);

                WandHintInput hintInput =
                    wand.AddComponent<WandHintInput>();
                MethodInfo configureMethod =
                    typeof(WandHintInput).GetMethod(
                        "Configure",
                        BindingFlags.Instance |
                        BindingFlags.Public);
                Assert.That(configureMethod, Is.Not.Null);
                configureMethod.Invoke(
                    hintInput,
                    new object[] { null, grab });

                MethodInfo ensureMethod =
                    typeof(WandHintInput).GetMethod(
                        "EnsureGrabAssistCollider",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                Assert.That(ensureMethod, Is.Not.Null);
                ensureMethod.Invoke(hintInput, null);
                ensureMethod.Invoke(hintInput, null);

                Transform assist =
                    wand.transform.Find("WC_WandGrabAssist");
                Assert.That(assist, Is.Not.Null);

                CapsuleCollider assistCollider =
                    assist.GetComponent<CapsuleCollider>();
                Assert.That(assistCollider, Is.Not.Null);
                Assert.That(assistCollider.isTrigger, Is.True);
                Assert.That(assistCollider.radius, Is.EqualTo(0.075f));
                Assert.That(assistCollider.height, Is.EqualTo(0.55f));
                Assert.That(assistCollider.direction, Is.EqualTo(1));
                int assistReferenceCount = 0;
                foreach (object collider in colliders)
                {
                    if (ReferenceEquals(collider, assistCollider))
                    {
                        assistReferenceCount++;
                    }
                }

                Assert.That(assistReferenceCount, Is.EqualTo(1));

                Assert.That(physicalCollider.isTrigger, Is.False);
                Assert.That(colliders, Does.Contain(physicalCollider));
            }
            finally
            {
                Object.DestroyImmediate(wand);
            }
        }
    }
}
