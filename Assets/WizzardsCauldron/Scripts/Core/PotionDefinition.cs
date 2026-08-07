using UnityEngine;

namespace WizzardsCauldron.Core
{
    [CreateAssetMenu(
        fileName = "SO_Potion",
        menuName = "Wizzards Cauldron/Potion Definition")]
    public sealed class PotionDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _stableId = "";
        [SerializeField] private string _displayName = "";

        [Header("Puzzle Values")]
        [SerializeField, Min(0)] private int _healthValue;
        [SerializeField, Min(1)] private int _fillValue = 1;

        [Header("Presentation")]
        [SerializeField] private Color _presentationColor = Color.white;

        public string StableId => _stableId;
        public string DisplayName => _displayName;
        public int HealthValue => _healthValue;
        public int FillValue => _fillValue;
        public Color PresentationColor => _presentationColor;

        private void OnValidate()
        {
            _stableId = _stableId.Trim();
            _healthValue = Mathf.Max(0, _healthValue);
            _fillValue = Mathf.Max(1, _fillValue);
        }
    }
}