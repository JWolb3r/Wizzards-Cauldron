using System;
using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Interactions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PotionController))]
    public sealed class PotionInspectionSource : MonoBehaviour
    {
        [SerializeField]
        private PotionController _potion;

        private bool _isInspecting;

        public PotionController Potion => _potion;

        public PotionDefinition Definition =>
            _potion != null
                ? _potion.Definition
                : null;

        public bool IsInspecting => _isInspecting;

        public event Action<PotionInspectionSource>
            InspectionBegan;

        public event Action<PotionInspectionSource>
            InspectionEnded;

        private void Reset()
        {
            _potion = GetComponent<PotionController>();
        }

        private void OnValidate()
        {
            if (_potion == null)
            {
                _potion =
                    GetComponent<PotionController>();
            }
        }

        private void Awake()
        {
            if (_potion == null)
            {
                Debug.LogError(
                    "PotionInspectionSource has no " +
                    "PotionController reference.",
                    this);

                enabled = false;
            }
        }

        public void BeginInspection()
        {
            if (!isActiveAndEnabled ||
                _isInspecting ||
                Definition == null)
            {
                return;
            }

            _isInspecting = true;
            InspectionBegan?.Invoke(this);
        }

        public void EndInspection()
        {
            if (!_isInspecting)
            {
                return;
            }

            _isInspecting = false;
            InspectionEnded?.Invoke(this);
        }

        private void OnDisable()
        {
            EndInspection();
        }
    }
}