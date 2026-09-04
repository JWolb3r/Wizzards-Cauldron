using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.Presentation;
using Action = System.Action;

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
            QuestCampaignController campaign =
                Object.FindFirstObjectByType<
                    QuestCampaignController>();
            CauldronIntakeProxy intakeProxy =
                Object.FindFirstObjectByType<CauldronIntakeProxy>();
            PotionController[] allPotions = Object
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
            Assert.That(campaign, Is.Not.Null);
            Assert.That(allPotions.Length, Is.EqualTo(7));
            Assert.That(campaign.TrySelectRound(0), Is.True);
            yield return null;

            PotionController[] potions = allPotions
                .Where(potion =>
                    potion.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(potions.Length, Is.EqualTo(3));
            Assert.That(
                allPotions.Any(potion =>
                    string.Equals(
                        potion.Definition.StableId,
                        "red",
                        System.StringComparison.OrdinalIgnoreCase)),
                Is.False);

            AstralAmbientMusic ambientMusic =
                Object.FindFirstObjectByType<AstralAmbientMusic>();
            Assert.That(ambientMusic, Is.Not.Null);
            AudioSource ambientSource =
                ambientMusic.GetComponent<AudioSource>();
            Assert.That(ambientSource, Is.Not.Null);
            Assert.That(ambientSource.clip, Is.Not.Null);
            Assert.That(ambientSource.loop, Is.True);
            Assert.That(ambientSource.mute, Is.False);
            Assert.That(ambientSource.volume, Is.GreaterThanOrEqualTo(0.3f));
            Assert.That(ambientSource.isPlaying, Is.True);

            AlchemyAudioFeedback audioFeedback =
                Object.FindFirstObjectByType<AlchemyAudioFeedback>();
            Assert.That(audioFeedback, Is.Not.Null);
            Assert.That(audioFeedback.IsReady, Is.True);
            AudioSource potionSfx = GameObject.Find(
                    "PotionIntoCauldronSfx")
                .GetComponent<AudioSource>();
            AudioSource resetSfx = GameObject.Find(
                    "ResetRuneSfx")
                .GetComponent<AudioSource>();
            AudioSource resultSfx = GameObject.Find(
                    "SolutionResultSfx")
                .GetComponent<AudioSource>();

            WandActivator finishActivator =
                Object.FindFirstObjectByType<WandActivator>();
            SphereCollider finishHitbox = finishActivator
                .GetComponent<SphereCollider>();
            Assert.That(finishHitbox, Is.Not.Null);
            Assert.That(finishHitbox.isTrigger, Is.True);
            Assert.That(finishHitbox.radius, Is.InRange(0.85f, 0.95f));

            GameObject submitLabel = GameObject.Find(
                "SubmitSolutionLabel");
            Assert.That(submitLabel, Is.Not.Null);
            Assert.That(
                submitLabel.GetComponent("TextMeshPro"),
                Is.Not.Null);

            Vector3[] resetPositions = potions
                .Select(potion => potion.transform.position)
                .ToArray();

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
                    Assert.That(potionSfx.isPlaying, Is.True);
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
            Assert.That(resultSfx.isPlaying, Is.True);

            Vector3[] positionsWhenMadeAvailable =
                new Vector3[potions.Length];
            bool[] availabilityObserved =
                new bool[potions.Length];
            Action[] stateHandlers = new Action[potions.Length];

            for (int index = 0; index < potions.Length; index++)
            {
                int potionIndex = index;
                stateHandlers[index] = () =>
                {
                    if (potions[potionIndex].State !=
                        PotionState.Available)
                    {
                        return;
                    }

                    availabilityObserved[potionIndex] = true;
                    positionsWhenMadeAvailable[potionIndex] =
                        potions[potionIndex].transform.position;
                };
                potions[index].StateChanged += stateHandlers[index];
            }

            Assert.That(reset.TryResetRoom(), Is.True);

            for (int index = 0; index < potions.Length; index++)
            {
                potions[index].StateChanged -= stateHandlers[index];

                Assert.That(
                    availabilityObserved[index],
                    Is.True,
                    potions[index].name +
                    " did not become available during reset.");
                float distance = Vector3.Distance(
                    resetPositions[index],
                    positionsWhenMadeAvailable[index]);
                Assert.That(
                    distance,
                    Is.LessThan(0.02f),
                    potions[index].name +
                    " was made available before its start pose " +
                    "was restored.");
            }

            yield return null;
            Assert.That(resetSfx.isPlaying, Is.True);

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

            for (int frame = 0; frame < 30; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            for (int index = 0; index < potions.Length; index++)
            {
                Rigidbody body = potions[index].GetComponent<Rigidbody>();
                float distance = Vector3.Distance(
                    resetPositions[index],
                    potions[index].transform.position);
                Debug.Log(
                    "[WC_RESET_STABILITY] " + potions[index].name +
                    " distance=" + distance.ToString("F3") +
                    " velocity=" + body.linearVelocity.magnitude.ToString("F3") +
                    " angular=" + body.angularVelocity.magnitude.ToString("F3"));
                Assert.That(
                    distance,
                    Is.LessThan(0.4f),
                    potions[index].name +
                    " moved abnormally after the room reset.");
            }
        }

        [UnityTest]
        public IEnumerator ConsumedPotionWaitsForSelectRelease()
        {
            var potionObject = new GameObject(
                "ConsumedPotionReleaseGateTest");
            potionObject.SetActive(false);
            potionObject.transform.position =
                new Vector3(1f, 2f, 3f);

            Rigidbody body = potionObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            PotionController potion =
                potionObject.AddComponent<PotionController>();
            PhysicsResettable resettable =
                potionObject.AddComponent<PhysicsResettable>();
            FakeGrabInteractable grab =
                potionObject.AddComponent<FakeGrabInteractable>();
            PotionConsumptionPresentation presentation =
                potionObject.AddComponent<
                    PotionConsumptionPresentation>();

            FieldInfo interactionField = typeof(PhysicsResettable)
                .GetField(
                    "_interactionBehaviour",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(interactionField, Is.Not.Null);
            interactionField.SetValue(resettable, grab);
            presentation.Configure(potion, grab);

            potionObject.SetActive(true);
            yield return null;

            var interactor = new FakeSelectingInteractor
            {
                isSelectActive = true
            };
            grab.SetSelecting(interactor);

            Vector3 initialPosition = potionObject.transform.position;
            potionObject.transform.position =
                initialPosition + Vector3.one * 5f;
            body.position = potionObject.transform.position;
            Physics.SyncTransforms();

            potion.MarkUsed();
            Assert.That(presentation.IsConsumed, Is.True);
            Assert.That(grab.enabled, Is.False);
            Assert.That(grab.interactorsSelecting, Is.Empty);

            resettable.RestoreInitialPose();
            potion.ResetState();

            Assert.That(
                Vector3.Distance(
                    initialPosition,
                    potionObject.transform.position),
                Is.LessThan(0.001f));
            Assert.That(
                grab.enabled,
                Is.False,
                "The consumed potion became grabbable while its " +
                "original controller still held select.");

            yield return null;
            Assert.That(grab.enabled, Is.False);

            interactor.isSelectActive = false;
            yield return null;

            Assert.That(
                grab.enabled,
                Is.True,
                "The potion did not become grabbable after select " +
                "was released.");

            Object.Destroy(potionObject);
        }

        private sealed class FakeSelectingInteractor
        {
            public bool isSelectActive { get; set; }
        }

        private sealed class FakeGrabInteractable : MonoBehaviour
        {
            private readonly List<object> _interactorsSelecting =
                new List<object>();

            public IReadOnlyList<object> interactorsSelecting =>
                _interactorsSelecting;

            public void SetSelecting(object interactor)
            {
                _interactorsSelecting.Clear();
                _interactorsSelecting.Add(interactor);
            }

            private void OnDisable()
            {
                _interactorsSelecting.Clear();
            }
        }
    }
}
