using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.UI
{
    [DisallowMultipleComponent]
    public sealed class PotionInspectionPanel : MonoBehaviour
    {
        [Header("Inspection Sources")]
        [SerializeField]
        private PotionInspectionSource[] _sources =
            new PotionInspectionSource[0];

        [Header("Visibility")]
        [SerializeField]
        private GameObject _content;

        [Header("Text Fields")]
        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _healthText;

        [SerializeField]
        private TMP_Text _fillText;

        private readonly List<PotionInspectionSource>
            _activeSources =
                new List<PotionInspectionSource>();

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                HidePanel();
                return;
            }

            if (!HasRequiredReferences())
            {
                HidePanel();

                Debug.LogError(
                    "PotionInspectionPanel is missing one " +
                    "or more required Inspector references.",
                    this);

                return;
            }

            for (int index = 0;
                 index < _sources.Length;
                 index++)
            {
                PotionInspectionSource source =
                    _sources[index];

                source.InspectionBegan +=
                    HandleInspectionBegan;

                source.InspectionEnded +=
                    HandleInspectionEnded;

                if (source.IsInspecting)
                {
                    AddActiveSource(source);
                }
            }

            RefreshPanel();
        }

        private void OnDisable()
        {
            if (_sources != null)
            {
                for (int index = 0;
                     index < _sources.Length;
                     index++)
                {
                    PotionInspectionSource source =
                        _sources[index];

                    if (source == null)
                    {
                        continue;
                    }

                    source.InspectionBegan -=
                        HandleInspectionBegan;

                    source.InspectionEnded -=
                        HandleInspectionEnded;
                }
            }

            _activeSources.Clear();
            HidePanel();
        }

        private void HandleInspectionBegan(
            PotionInspectionSource source)
        {
            AddActiveSource(source);
            RefreshPanel();
        }

        private void HandleInspectionEnded(
            PotionInspectionSource source)
        {
            _activeSources.Remove(source);
            RefreshPanel();
        }

        private void AddActiveSource(
            PotionInspectionSource source)
        {
            if (source == null)
            {
                return;
            }

            _activeSources.Remove(source);
            _activeSources.Add(source);
        }

        private void RefreshPanel()
        {
            for (int index =
                     _activeSources.Count - 1;
                 index >= 0;
                 index--)
            {
                PotionInspectionSource source =
                    _activeSources[index];

                if (source == null ||
                    source.Definition == null)
                {
                    _activeSources.RemoveAt(index);
                    continue;
                }

                ShowPotion(source);
                return;
            }

            HidePanel();
        }

        private void ShowPotion(
            PotionInspectionSource source)
        {
            PotionDefinition definition =
                source.Definition;

            string displayName =
                string.IsNullOrWhiteSpace(
                    definition.DisplayName)
                    ? source.name
                    : definition.DisplayName;

            _nameText.text = displayName;

            _healthText.text =
                $"Health: {definition.HealthValue}";

            _fillText.text =
                $"Fill: {definition.FillValue}";

            _content.SetActive(true);
        }

        private void HidePanel()
        {
            if (_content != null)
            {
                _content.SetActive(false);
            }
        }

        private bool HasRequiredReferences()
        {
            if (_sources == null ||
                _sources.Length == 0 ||
                _content == null ||
                _nameText == null ||
                _healthText == null ||
                _fillText == null)
            {
                return false;
            }

            for (int index = 0;
                 index < _sources.Length;
                 index++)
            {
                if (_sources[index] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}