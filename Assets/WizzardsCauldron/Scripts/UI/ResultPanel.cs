using TMPro;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.UI
{
    public sealed class ResultPanel : MonoBehaviour
    {
        private static readonly Vector2 CampaignPanelSize =
            new Vector2(820f, 500f);

        [Header("Gameplay Source")]
        [SerializeField]
        private GameSessionController _gameSession;

        [SerializeField]
        private QuestCampaignController _campaign;

        [Header("Visibility")]
        [SerializeField]
        private GameObject _resultContent;

        [Header("Text Fields")]
        [SerializeField]
        private TMP_Text _outcomeText;

        [SerializeField]
        private TMP_Text _healthText;

        [SerializeField]
        private TMP_Text _capacityText;

        [SerializeField]
        private TMP_Text _bestHealthText;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_campaign == null)
            {
                _campaign = FindFirstObjectByType<
                    QuestCampaignController>(
                        FindObjectsInactive.Include);
            }

            if (_campaign != null)
            {
                ApplyCampaignLayout();
            }

            if (!HasRequiredReferences())
            {
                if (_resultContent != null)
                {
                    _resultContent.SetActive(false);
                }

                Debug.LogError(
                    "ResultPanel is missing one or more " +
                    "required Inspector references.",
                    this);

                return;
            }

            if (_campaign != null)
            {
                _campaign.RoundFinished +=
                    HandleRoundFinished;
                _campaign.RoundStarted +=
                    HandleRoundStarted;
                _campaign.CampaignFinished +=
                    HandleCampaignFinished;
                _campaign.DifficultySelectionRequested +=
                    HandleDifficultySelectionRequested;
            }
            else
            {
                _gameSession.AttemptFinished +=
                    HandleAttemptFinished;

                _gameSession.SessionReset +=
                    HandleSessionReset;
            }

            RefreshFromSession();
        }

        private void OnDisable()
        {
            if (_gameSession != null)
            {
                _gameSession.AttemptFinished -=
                    HandleAttemptFinished;

                _gameSession.SessionReset -=
                    HandleSessionReset;
            }

            if (_campaign != null)
            {
                _campaign.RoundFinished -=
                    HandleRoundFinished;
                _campaign.RoundStarted -=
                    HandleRoundStarted;
                _campaign.CampaignFinished -=
                    HandleCampaignFinished;
                _campaign.DifficultySelectionRequested -=
                    HandleDifficultySelectionRequested;
            }
        }

        private void RefreshFromSession()
        {
            if (_campaign != null)
            {
                if (_campaign.LatestRoundResult == null)
                {
                    HideResult();
                }
                else
                {
                    ShowCampaignResult(
                        _campaign.LatestRoundResult);
                }

                return;
            }

            AttemptResult latestResult =
                _gameSession.LatestResult;

            if (latestResult == null)
            {
                HideResult();
                return;
            }

            ShowResult(latestResult);
        }

        private void HandleAttemptFinished(
            AttemptResult result)
        {
            ShowResult(result);
        }

        private void HandleSessionReset()
        {
            HideResult();
        }

        private void HandleRoundFinished(
            QuestRoundResult result)
        {
            ShowCampaignResult(result);
        }

        private void HandleRoundStarted(
            QuestRoundInfo _)
        {
            HideResult();
        }

        private void HandleDifficultySelectionRequested()
        {
            HideResult();
        }

        private void HandleCampaignFinished(
            QuestCampaignSummary summary)
        {
            QuestRoundResult latest =
                _campaign.LatestRoundResult;
            if (latest == null)
            {
                return;
            }

            ShowCampaignResult(latest);
            _bestHealthText.text =
                $"Stars: {latest.Stars} / 3" +
                GetHintSuffix(latest) +
                $"\nCampaign: {summary.TotalStars} / " +
                $"{summary.MaximumStars}";
        }

        public void ConfigureCampaign(
            QuestCampaignController campaign)
        {
            _campaign = campaign;
        }

        private void ShowResult(
            AttemptResult result)
        {
            if (result == null)
            {
                HideResult();
                return;
            }

            _outcomeText.text =
                GetOutcomeHeading(result.Outcome);

            _healthText.text =
                $"Health: {result.PlayerHealth} / " +
                $"{result.BestPossibleHealth}";

            _capacityText.text =
                $"Capacity: {result.UsedCapacity} / " +
                $"{result.MaximumCapacity}";

            _bestHealthText.text =
                $"Best possible health: " +
                $"{result.BestPossibleHealth}";

            _resultContent.SetActive(true);
        }

        private void ShowCampaignResult(
            QuestRoundResult result)
        {
            if (result == null || result.Attempt == null)
            {
                HideResult();
                return;
            }

            ShowResult(result.Attempt);
            _outcomeText.text =
                result.Round.Title + "\n" +
                GetOutcomeHeading(result.Attempt.Outcome);

            string nextLine =
                $"Best total: {result.TotalStars} / " +
                $"{result.Round.RoundCount * 3}" +
                "\nDifficulty selection returning soon";

            _bestHealthText.text =
                $"Stars: {result.Stars} / 3" +
                GetHintSuffix(result) +
                $" | Best: {result.BestStars} / 3" +
                "\n" + nextLine;
        }

        private static string GetHintSuffix(
            QuestRoundResult result)
        {
            return result.HintUsed
                ? " (hint: -1)"
                : string.Empty;
        }

        private void HideResult()
        {
            if (_resultContent != null)
            {
                _resultContent.SetActive(false);
            }
        }

        private void ApplyCampaignLayout()
        {
            if (transform is RectTransform panelRect)
            {
                panelRect.sizeDelta = CampaignPanelSize;
            }

            SetTextLayout(
                _outcomeText,
                new Vector2(40f, -28f),
                new Vector2(740f, 125f),
                48f);
            SetTextLayout(
                _healthText,
                new Vector2(40f, -165f),
                new Vector2(740f, 58f),
                40f);
            SetTextLayout(
                _capacityText,
                new Vector2(40f, -228f),
                new Vector2(740f, 58f),
                40f);
            SetTextLayout(
                _bestHealthText,
                new Vector2(40f, -292f),
                new Vector2(740f, 180f),
                32f);
        }

        private static void SetTextLayout(
            TMP_Text text,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            text.enableAutoSizing = false;
            text.fontSize = fontSize;
        }

        private bool HasRequiredReferences()
        {
            return
                _gameSession != null &&
                _resultContent != null &&
                _outcomeText != null &&
                _healthText != null &&
                _capacityText != null &&
                _bestHealthText != null;
        }

        private static string GetOutcomeHeading(
            AttemptOutcome outcome)
        {
            switch (outcome)
            {
                case AttemptOutcome.NoPotionsSelected:
                    return "No potions selected";

                case AttemptOutcome.ValidSolution:
                    return "Valid solution";

                case AttemptOutcome.Overfilled:
                    return "Cauldron overfilled";

                case AttemptOutcome.OptimalSolution:
                    return "Optimal solution";

                default:
                    return "Attempt finished";
            }
        }
    }
}
