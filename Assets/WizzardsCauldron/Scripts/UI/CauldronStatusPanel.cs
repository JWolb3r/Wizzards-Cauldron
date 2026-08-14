using TMPro;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.UI
{
    public sealed class CauldronStatusPanel : MonoBehaviour
    {
        [SerializeField] private GameSessionController _gameSession;

        [Header("Gameplay Sources")]
        [SerializeField] private CauldronController _cauldron;
        [SerializeField] private CauldronIntake _intake;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private TMP_Text _capacityText;
        [SerializeField] private TMP_Text _messageText;

        private void OnEnable()
        {
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

            RefreshTotals();
            _messageText.text = "Add a potion";
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
                $"Capacity: {_cauldron.RemainingCapacity} / " +
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
                    _messageText.text =
                        $"{potionName} accepted";
                    break;

                case PotionAcceptanceResult.TooFull:
                    _messageText.text =
                        "Not enough capacity";
                    break;

                case PotionAcceptanceResult.AlreadyUsed:
                    _messageText.text =
                        "Potion already used";
                    break;

                case PotionAcceptanceResult.NotInPuzzle:
                    _messageText.text =
                        "Potion not part of this puzzle";
                    break;

                case PotionAcceptanceResult.GameFinished:
                    _messageText.text =
                        "Attempt already finished";
                    break;

                case PotionAcceptanceResult.InvalidPotion:
                    _messageText.text =
                        "Invalid potion";
                    break;
            }
        }

        private void HandleSessionReset()
        {
            RefreshTotals();
            _messageText.text = "Add a potion";
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