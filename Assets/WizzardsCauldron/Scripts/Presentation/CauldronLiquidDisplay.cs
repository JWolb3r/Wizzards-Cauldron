using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CauldronLiquidDisplay :
        MonoBehaviour
    {
        [Header("Gameplay Source")]
        [SerializeField]
        private CauldronController _cauldron;

        [Header("Liquid Visual")]
        [SerializeField]
        private Transform _liquidVisual;

        [Header("Local Height Configuration")]
        [SerializeField]
        private float _bottomLocalY = 0.90f;

        [SerializeField]
        [Min(0.001f)]
        private float _emptyScaleY = 0.01f;

        [SerializeField]
        [Min(0.001f)]
        private float _fullScaleY = 0.12f;

        private float _scaleX;
        private float _scaleZ;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                if (_liquidVisual != null)
                {
                    _liquidVisual.gameObject
                        .SetActive(false);
                }

                Debug.LogError(
                    "CauldronLiquidDisplay is missing one " +
                    "or more required Inspector references.",
                    this);

                enabled = false;
                return;
            }

            Vector3 initialScale =
                _liquidVisual.localScale;

            _scaleX = initialScale.x;
            _scaleZ = initialScale.z;
        }

        private void OnEnable()
        {
            if (_cauldron == null ||
                _liquidVisual == null)
            {
                return;
            }

            _cauldron.TotalsChanged +=
                RefreshLiquid;

            RefreshLiquid();
        }

        private void OnDisable()
        {
            if (_cauldron != null)
            {
                _cauldron.TotalsChanged -=
                    RefreshLiquid;
            }
        }

        private void OnValidate()
        {
            _emptyScaleY = Mathf.Max(
                0.001f,
                _emptyScaleY);

            _fullScaleY = Mathf.Max(
                _emptyScaleY,
                _fullScaleY);
        }

        private void RefreshLiquid()
        {
            int maximumCapacity =
                _cauldron.MaximumCapacity;

            int usedCapacity =
                _cauldron.UsedCapacity;

            if (maximumCapacity <= 0 ||
                usedCapacity <= 0)
            {
                _liquidVisual.gameObject
                    .SetActive(false);

                return;
            }

            float fillRatio = Mathf.Clamp01(
                (float)usedCapacity /
                maximumCapacity);

            float scaleY = Mathf.Lerp(
                _emptyScaleY,
                _fullScaleY,
                fillRatio);

            _liquidVisual.localScale =
                new Vector3(
                    _scaleX,
                    scaleY,
                    _scaleZ);

            Vector3 localPosition =
                _liquidVisual.localPosition;

            // A Unity Cylinder is two local units tall.
            // Placing its center one scaleY above the
            // bottom keeps the lower edge stationary.
            localPosition.y =
                _bottomLocalY + scaleY;

            _liquidVisual.localPosition =
                localPosition;

            _liquidVisual.gameObject
                .SetActive(true);
        }

        private bool HasRequiredReferences()
        {
            return
                _cauldron != null &&
                _liquidVisual != null;
        }
    }
}