using TMPro;
using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.UI
{
    public sealed class ResultPanel : MonoBehaviour
    {
        [Header("Gameplay Source")]
        [SerializeField]
        private GameSessionController _gameSession;

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

            _gameSession.AttemptFinished +=
                HandleAttemptFinished;

            RefreshFromSession();
        }

        private void OnDisable()
        {
            if (_gameSession != null)
            {
                _gameSession.AttemptFinished -=
                    HandleAttemptFinished;
            }
        }

        private void RefreshFromSession()
        {
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

        private void HideResult()
        {
            if (_resultContent != null)
            {
                _resultContent.SetActive(false);
            }
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

                case AttemptOutcome.OptimalSolution:
                    return "Optimal solution";

                default:
                    return "Attempt finished";
            }
        }
    }
}