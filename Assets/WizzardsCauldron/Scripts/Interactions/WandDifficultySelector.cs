using TMPro;
using UnityEngine;

namespace WizzardsCauldron.Interactions
{
    [DisallowMultipleComponent]
    public sealed class WandDifficultySelector : MonoBehaviour
    {
        [SerializeField]
        private QuestCampaignController _campaign;

        [SerializeField, Min(0)]
        private int _roundIndex;

        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private WandTip _wandTip;

        [SerializeField]
        private string _title = "Difficulty";

        [SerializeField]
        private string _details = string.Empty;

        [SerializeField, Min(0.005f)]
        private float _touchDepth = 0.045f;

        private bool _wasInside;

        public void Configure(
            QuestCampaignController campaign,
            int roundIndex,
            TMP_Text label,
            WandTip wandTip,
            string title,
            string details)
        {
            _campaign = campaign;
            _roundIndex = Mathf.Max(0, roundIndex);
            _label = label;
            _wandTip = wandTip;
            _title = title;
            _details = details;
            RefreshLabel();
        }

        private void OnEnable()
        {
            _wasInside = IsWandInside();
        }

        private void Update()
        {
            bool isInside = IsWandInside();
            if (isInside &&
                !_wasInside &&
                _campaign != null &&
                _campaign.IsSelectingDifficulty)
            {
                _campaign.TrySelectRound(_roundIndex);
            }

            _wasInside = isInside;
        }

        public void RefreshLabel()
        {
            if (_label == null)
            {
                return;
            }

            string best = _campaign != null &&
                _campaign.IsRoundCompleted(_roundIndex)
                    ? "Best: " +
                      _campaign.GetBestStars(_roundIndex) +
                      " / 3 stars"
                    : "Not completed";

            _label.text =
                _title + "\n" +
                _details + "\n" +
                best;
        }

        private bool IsWandInside()
        {
            if (_wandTip == null ||
                transform is not RectTransform rectTransform)
            {
                return false;
            }

            Vector3 localPoint = transform.InverseTransformPoint(
                _wandTip.transform.position);
            return Mathf.Abs(localPoint.z) <=
                   _touchDepth / Mathf.Max(
                       0.0001f,
                       transform.lossyScale.z) &&
                   rectTransform.rect.Contains(
                       new Vector2(localPoint.x, localPoint.y));
        }
    }
}
