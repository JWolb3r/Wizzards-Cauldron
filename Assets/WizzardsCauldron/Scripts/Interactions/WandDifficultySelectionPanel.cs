using UnityEngine;

namespace WizzardsCauldron.Interactions
{
    [DisallowMultipleComponent]
    public sealed class WandDifficultySelectionPanel : MonoBehaviour
    {
        [SerializeField]
        private QuestCampaignController _campaign;

        [SerializeField]
        private GameObject _selectionContent;

        [SerializeField]
        private Canvas _gameplayStatusCanvas;

        [SerializeField]
        private WandDifficultySelector[] _selectors =
            System.Array.Empty<WandDifficultySelector>();

        public void Configure(
            QuestCampaignController campaign,
            GameObject selectionContent,
            Canvas gameplayStatusCanvas,
            WandDifficultySelector[] selectors)
        {
            _campaign = campaign;
            _selectionContent = selectionContent;
            _gameplayStatusCanvas = gameplayStatusCanvas;
            _selectors = selectors != null
                ? (WandDifficultySelector[])selectors.Clone()
                : System.Array.Empty<WandDifficultySelector>();
        }

        private void OnEnable()
        {
            if (_campaign == null)
            {
                return;
            }

            _campaign.DifficultySelectionRequested +=
                ShowSelection;
            _campaign.RoundStarted += HandleRoundStarted;
            SetSelectionVisible(
                _campaign.IsSelectingDifficulty);
        }

        private void Start()
        {
            if (_campaign != null &&
                _campaign.IsSelectingDifficulty)
            {
                ShowSelection();
            }
        }

        private void OnDisable()
        {
            if (_campaign == null)
            {
                return;
            }

            _campaign.DifficultySelectionRequested -=
                ShowSelection;
            _campaign.RoundStarted -= HandleRoundStarted;
        }

        private void ShowSelection()
        {
            for (int index = 0;
                 index < _selectors.Length;
                 index++)
            {
                if (_selectors[index] != null)
                {
                    _selectors[index].RefreshLabel();
                }
            }

            SetSelectionVisible(true);
        }

        private void HandleRoundStarted(QuestRoundInfo _)
        {
            SetSelectionVisible(false);
        }

        private void SetSelectionVisible(bool visible)
        {
            if (_selectionContent != null)
            {
                _selectionContent.SetActive(visible);
            }

            if (_gameplayStatusCanvas != null)
            {
                _gameplayStatusCanvas.enabled = !visible;
            }
        }
    }
}
