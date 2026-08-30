using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.Presentation;

namespace WizzardsCauldron.Tests.PlayMode
{
    public sealed class CauldronConsumptionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PotionsDisappearExplodeOverfillAndReset()
        {
            yield return SceneManager.LoadSceneAsync(
                "SCN_InteractionTest_Visual",
                LoadSceneMode.Single);
            yield return null;

            CauldronController cauldron =
                Object.FindFirstObjectByType<CauldronController>();
            CauldronIntake intake =
                Object.FindFirstObjectByType<CauldronIntake>();
            GameSessionController session =
                Object.FindFirstObjectByType<GameSessionController>();
            RoomResetCoordinator reset =
                Object.FindFirstObjectByType<RoomResetCoordinator>();
            CauldronIntakeProxy intakeProxy =
                Object.FindFirstObjectByType<CauldronIntakeProxy>();
            PotionController[] potions = Object
                .FindObjectsByType<PotionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .OrderBy(potion => potion.Definition.StableId)
                .ToArray();

            Assert.That(cauldron, Is.Not.Null);
            Assert.That(intake, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(reset, Is.Not.Null);
            Assert.That(intakeProxy, Is.Not.Null);
            Assert.That(potions.Length, Is.EqualTo(8));

            ParticleSystem explosion = GameObject.Find(
                    "AcceptedPotionSpark")
                .GetComponent<ParticleSystem>();
            Assert.That(explosion, Is.Not.Null);

            int expectedFill = 0;
            int expectedHealth = 0;
            Vector3 firstExplosionPosition = Vector3.zero;

            for (int index = 0; index < potions.Length; index++)
            {
                PotionController potion = potions[index];
                PotionConsumptionPresentation presentation =
                    potion.GetComponent<PotionConsumptionPresentation>();
                Assert.That(presentation, Is.Not.Null);

                Vector3 consumedPosition = potion.transform.position;
                if (index == 0)
                {
                    BoxCollider proxyCollider =
                        intakeProxy.GetComponent<BoxCollider>();
                    Rigidbody potionBody = potion.GetComponent<Rigidbody>();
                    potionBody.position = proxyCollider.bounds.center;
                    potionBody.rotation = Quaternion.identity;
                    potionBody.linearVelocity = Vector3.zero;
                    potionBody.angularVelocity = Vector3.zero;
                    Physics.SyncTransforms();

                    for (int frame = 0;
                         frame < 20 && potion.IsAvailable;
                         frame++)
                    {
                        yield return new WaitForFixedUpdate();
                    }

                    consumedPosition = potion.transform.position;
                    Assert.That(
                        potion.IsAvailable,
                        Is.False,
                        "The visible cauldron intake did not process " +
                        "a bottle placed inside its opening.");
                }
                else
                {
                    Assert.That(
                        intake.TryProcessPotion(potion),
                        Is.True);
                    consumedPosition = potion.transform.position;
                }

                expectedFill += potion.Definition.FillValue;
                expectedHealth += potion.Definition.HealthValue;

                Assert.That(potion.State, Is.EqualTo(PotionState.Used));
                Assert.That(presentation.IsConsumed, Is.True);
                Assert.That(
                    potion.GetComponentsInChildren<Renderer>(true)
                        .Any(renderer => renderer.enabled),
                    Is.False);
                Assert.That(
                    potion.GetComponentsInChildren<Collider>(true)
                        .Any(collider => collider.enabled),
                    Is.False);
                Assert.That(
                    presentation.GrabInteractable.enabled,
                    Is.False);

                if (index == 0)
                {
                    firstExplosionPosition =
                        consumedPosition + Vector3.up * 0.04f;
                    Assert.That(
                        Vector3.Distance(
                            explosion.transform.position,
                            firstExplosionPosition),
                        Is.LessThan(0.001f));
                    Assert.That(explosion.isPlaying, Is.True);
                }
            }

            Assert.That(cauldron.UsedCapacity, Is.EqualTo(expectedFill));
            Assert.That(cauldron.TotalHealth, Is.EqualTo(expectedHealth));
            Assert.That(
                cauldron.UsedCapacity,
                Is.GreaterThan(cauldron.MaximumCapacity));

            Assert.That(
                session.TryFinishAttempt(out AttemptResult result),
                Is.True);
            Assert.That(result.Outcome, Is.EqualTo(AttemptOutcome.Overfilled));

            Assert.That(reset.TryResetRoom(), Is.True);
            yield return null;

            Assert.That(cauldron.UsedCapacity, Is.Zero);
            Assert.That(cauldron.TotalHealth, Is.Zero);
            foreach (PotionController potion in potions)
            {
                PotionConsumptionPresentation presentation =
                    potion.GetComponent<PotionConsumptionPresentation>();
                Assert.That(potion.State, Is.EqualTo(PotionState.Available));
                Assert.That(presentation.IsConsumed, Is.False);
                Assert.That(
                    potion.GetComponentsInChildren<Renderer>(true)
                        .Any(renderer => renderer.enabled),
                    Is.True);
                Assert.That(
                    potion.GetComponentsInChildren<Collider>(true)
                        .Any(collider => collider.enabled && !collider.isTrigger),
                    Is.True);
                Assert.That(
                    presentation.GrabInteractable.enabled,
                    Is.True);
            }
        }
    }
}
