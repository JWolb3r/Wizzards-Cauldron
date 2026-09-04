using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.Tests.PlayMode
{
    public sealed class QuestCampaignPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayerSelectsRoundsWithWandStyleFlow()
        {
            yield return SceneManager.LoadSceneAsync(
                "SCN_InteractionTest_Visual",
                LoadSceneMode.Single);
            yield return null;

            QuestCampaignController campaign =
                Object.FindFirstObjectByType<
                    QuestCampaignController>();
            GameSessionController session =
                Object.FindFirstObjectByType<
                    GameSessionController>();
            CauldronIntake intake =
                Object.FindFirstObjectByType<CauldronIntake>();
            PotionController[] allPotions = Object
                .FindObjectsByType<PotionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            WandDifficultySelector[] selectors = Object
                .FindObjectsByType<WandDifficultySelector>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Assert.That(campaign, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(intake, Is.Not.Null);
            Assert.That(allPotions.Length, Is.EqualTo(7));
            Assert.That(selectors.Length, Is.EqualTo(3));
            Assert.That(
                selectors.All(selector =>
                    selector.gameObject.activeInHierarchy &&
                    selector.GetComponent<Collider>() == null),
                Is.True);

            Assert.That(campaign.IsSelectingDifficulty, Is.True);
            Assert.That(campaign.HasActiveRound, Is.False);
            Assert.That(session.IsPlaying, Is.False);
            Assert.That(
                allPotions.Count(potion =>
                    potion.gameObject.activeInHierarchy),
                Is.Zero);

            Assert.That(campaign.TrySelectRound(0), Is.True);
            Assert.That(
                selectors.All(selector =>
                    !selector.gameObject.activeInHierarchy),
                Is.True);
            AssertRound(campaign, session, allPotions, 1, 4, 3);

            Assert.That(
                campaign.TryUseHint(
                    out PotionController hintedPotion),
                Is.True);
            Assert.That(hintedPotion, Is.Not.Null);
            Assert.That(hintedPotion.gameObject.activeInHierarchy, Is.True);
            Assert.That(campaign.TryUseHint(out _), Is.False);

            foreach (string potionId in
                session.OptimalSolution.SelectedPotionIds)
            {
                PotionController potion = allPotions.Single(item =>
                    item.Definition.StableId == potionId);
                Assert.That(
                    intake.TryProcessPotion(potion),
                    Is.True);
            }

            Assert.That(
                session.TryFinishAttempt(
                    out AttemptResult firstResult),
                Is.True);
            Assert.That(
                firstResult.Outcome,
                Is.EqualTo(AttemptOutcome.OptimalSolution));
            Assert.That(
                campaign.LatestRoundResult.Stars,
                Is.EqualTo(2));
            Assert.That(
                campaign.LatestRoundResult.HintUsed,
                Is.True);

            yield return new WaitForSecondsRealtime(
                campaign.SelectionReturnDelay + 0.2f);
            Assert.That(campaign.IsSelectingDifficulty, Is.True);
            Assert.That(campaign.HasActiveRound, Is.False);
            Assert.That(campaign.GetBestStars(0), Is.EqualTo(2));
            Assert.That(
                selectors.All(selector =>
                    selector.gameObject.activeInHierarchy),
                Is.True);

            Assert.That(campaign.TrySelectRound(1), Is.True);
            AssertRound(campaign, session, allPotions, 2, 6, 5);

            Assert.That(session.TryFinishAttempt(out _), Is.True);
            Assert.That(
                campaign.LatestRoundResult.Stars,
                Is.Zero);

            yield return new WaitForSecondsRealtime(
                campaign.SelectionReturnDelay + 0.2f);
            Assert.That(campaign.IsSelectingDifficulty, Is.True);

            Assert.That(campaign.TrySelectRound(2), Is.True);
            AssertRound(campaign, session, allPotions, 3, 8, 7);

            QuestCampaignSummary summary = null;
            campaign.CampaignFinished += result => summary = result;
            Assert.That(session.TryFinishAttempt(out _), Is.True);

            Assert.That(campaign.IsCampaignComplete, Is.True);
            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.TotalStars, Is.EqualTo(2));
            Assert.That(summary.MaximumStars, Is.EqualTo(9));

            yield return new WaitForSecondsRealtime(
                campaign.SelectionReturnDelay + 0.2f);
            Assert.That(campaign.IsSelectingDifficulty, Is.True);
            Assert.That(campaign.TrySelectRound(0), Is.True);
            AssertRound(campaign, session, allPotions, 1, 4, 3);
        }

        private static void AssertRound(
            QuestCampaignController campaign,
            GameSessionController session,
            PotionController[] potions,
            int expectedRoundNumber,
            int expectedCapacity,
            int expectedActivePotions)
        {
            Assert.That(
                campaign.CurrentRoundNumber,
                Is.EqualTo(expectedRoundNumber));
            Assert.That(campaign.RoundCount, Is.EqualTo(3));
            Assert.That(
                session.PuzzleDefinition.CauldronCapacity,
                Is.EqualTo(expectedCapacity));
            Assert.That(
                potions.Count(potion =>
                    potion.gameObject.activeInHierarchy),
                Is.EqualTo(expectedActivePotions));
        }
    }
}
