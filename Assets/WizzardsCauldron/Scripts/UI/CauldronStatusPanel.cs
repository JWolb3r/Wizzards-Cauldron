using TMPro;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.UI
{
    public sealed class CauldronStatusPanel : MonoBehaviour
    {
        [SerializeField] private GameSessionController _gameSession;

        [SerializeField] private QuestCampaignController _campaign;

        [Header("Gameplay Sources")]
        [SerializeField] private CauldronController _cauldron;
        [SerializeField] private CauldronIntake _intake;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private TMP_Text _capacityText;
        [SerializeField] private TMP_Text _messageText;

        private void OnEnable()
        {
            if (_campaign == null && Application.isPlaying)
            {
                _campaign = FindFirstObjectByType<
                    QuestCampaignController>(
                        FindObjectsInactive.Include);
            }

            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "CauldronStatusPanel is missing one or more " +
                    "required Inspector references.",
                    this);

                return;
            }

            _cauldron.TotalsChanged += RefreshTotals;
            _intake.PotionProcessed += HandlePotionProcessed;
            _gameSession.SessionReset += HandleSessionReset;

            if (_campaign != null)
            {
                _campaign.RoundStarted += HandleRoundStarted;
                _campaign.HintRevealed += HandleHintRevealed;
                _campaign.HintUnavailable += HandleHintUnavailable;
                _campaign.CampaignFinished += HandleCampaignFinished;
                _campaign.DifficultySelectionRequested +=
                    HandleDifficultySelectionRequested;
            }

            RefreshTotals();
            SetMessage("Add a potion");
        }

        private void OnDisable()
        {
            if (_cauldron != null)
            {
                _cauldron.TotalsChanged -= RefreshTotals;
            }

            if (_intake != null)
            {
                _intake.PotionProcessed -= HandlePotionProcessed;
            }
            if (_gameSession != null)
            {
                _gameSession.SessionReset -= HandleSessionReset;
            }

            if (_campaign != null)
            {
                _campaign.RoundStarted -= HandleRoundStarted;
                _campaign.HintRevealed -= HandleHintRevealed;
                _campaign.HintUnavailable -= HandleHintUnavailable;
                _campaign.CampaignFinished -= HandleCampaignFinished;
                _campaign.DifficultySelectionRequested -=
                    HandleDifficultySelectionRequested;
            }
        }

        private void RefreshTotals()
        {
            if (_cauldron == null)
            {
                return;
            }

            _healthText.text =
                $"Health: {_cauldron.TotalHealth}";

            _capacityText.text =
                $"Capacity: {_cauldron.UsedCapacity} / " +
                $"{_cauldron.MaximumCapacity}";
        }

        private void HandlePotionProcessed(
            PotionController potion,
            PotionAcceptanceResult result)
        {
            RefreshTotals();

            string potionName = GetPotionName(potion);

            switch (result)
            {
                case PotionAcceptanceResult.Accepted:
                    SetMessage(
                        _cauldron.UsedCapacity >
                        _cauldron.MaximumCapacity
                            ? $"{potionName} accepted - overfilled"
                            : $"{potionName} accepted");
                    break;

                case PotionAcceptanceResult.TooFull:
                    SetMessage("Not enough capacity");
                    break;

                case PotionAcceptanceResult.AlreadyUsed:
                    SetMessage("Potion already used");
                    break;

                case PotionAcceptanceResult.NotInPuzzle:
                    SetMessage("Potion not part of this puzzle");
                    break;

                case PotionAcceptanceResult.GameFinished:
                    SetMessage("Attempt already finished");
                    break;

                case PotionAcceptanceResult.InvalidPotion:
                    SetMessage("Invalid potion");
                    break;
            }
        }

        private void HandleSessionReset()
        {
            RefreshTotals();
            SetMessage("Add a potion");
        }

        private void HandleRoundStarted(
            QuestRoundInfo _)
        {
            RefreshTotals();
            SetMessage("Add potion | Wand + B/Y: Hint");
        }

        private void HandleHintRevealed(
            PotionController potion)
        {
            SetMessage(
                "Hint: " + GetPotionName(potion));
        }

        private void HandleHintUnavailable(
            string message)
        {
            SetMessage(message);
        }

        private void HandleCampaignFinished(
            QuestCampaignSummary summary)
        {
            SetMessage(
                $"Campaign complete: {summary.TotalStars}/" +
                $"{summary.MaximumStars} stars");
        }

        private void HandleDifficultySelectionRequested()
        {
            RefreshTotals();
            SetMessage(
                "Choose a difficulty with the wand");
        }

        public void ConfigureCampaign(
            QuestCampaignController campaign)
        {
            _campaign = campaign;
        }

        private void SetMessage(string message)
        {
            if (_messageText == null)
            {
                return;
            }

            if (_campaign != null &&
                _campaign.HasActiveRound)
            {
                _messageText.text =
                    $"{_campaign.CurrentRoundNumber}/" +
                    $"{_campaign.RoundCount} " +
                    $"{_campaign.CurrentRoundTitle}\n" +
                    message;
                return;
            }

            _messageText.text = message;
        }

        private bool HasRequiredReferences()
        {
            return
                _cauldron != null &&
                _intake != null &&
                _healthText != null &&
                _capacityText != null &&
                _gameSession != null &&
                _messageText != null;
        }

        private static string GetPotionName(
            PotionController potion)
        {
            if (potion == null)
            {
                return "Potion";
            }

            if (potion.Definition == null ||
                string.IsNullOrWhiteSpace(
                    potion.Definition.DisplayName))
            {
                return potion.name;
            }

            return potion.Definition.DisplayName;
        }
    }
}
