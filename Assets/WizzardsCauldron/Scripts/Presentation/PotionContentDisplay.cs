using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PotionContentDisplay :
        MonoBehaviour
    {
        [Header("Gameplay Source")]
        [SerializeField]
        private PotionController _potion;

        [Header("Bottle Renderer")]
        [SerializeField]
        private Renderer _bottleRenderer;

        [SerializeField]
        [Min(0)]
        private int _glassMaterialSlot;

        [SerializeField]
        [Min(0)]
        private int _fluidMaterialSlot;

        [SerializeField]
        private Material _usedGlassMaterial;

        [SerializeField]
        private Material _invisibleFluidMaterial;

        private Material[] _availableMaterials;
        private Material[] _usedMaterials;

        private void Awake()
        {
            if (_potion == null)
            {
                _potion =
                    GetComponent<PotionController>();
            }

            // Alex's bottle is presented through a project-owned visual
            // wrapper in the dedicated visual scene.  Older scene revisions
            // serialized the renderer from the original FBX child, so that
            // reference can legitimately be empty after the wrapper is
            // installed.  Resolve a suitable child renderer at runtime
            // rather than disabling the component and losing the used-potion
            // material swap.
            if (_bottleRenderer == null)
            {
                _bottleRenderer =
                    FindBottleRenderer();
            }

            if (!CacheMaterials())
            {
                Debug.LogError(
                    "PotionContentDisplay is missing " +
                    "one or more required references.",
                    this);

                enabled = false;
            }
        }

        private Renderer FindBottleRenderer()
        {
            Renderer fallback = null;
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            int requiredSlot =
                Mathf.Max(_glassMaterialSlot, _fluidMaterialSlot);

            foreach (Renderer candidate in renderers)
            {
                if (candidate == null ||
                    candidate is ParticleSystemRenderer)
                {
                    continue;
                }

                Material[] materials = candidate.sharedMaterials;
                if (materials == null ||
                    materials.Length <= requiredSlot)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = candidate;
                }

                if (candidate.enabled)
                {
                    return candidate;
                }
            }

            return fallback;
        }

        private void OnEnable()
        {
            if (_potion == null ||
                _availableMaterials == null)
            {
                return;
            }

            _potion.StateChanged += RefreshVisual;
            RefreshVisual();
        }

        private void OnDisable()
        {
            if (_potion != null)
            {
                _potion.StateChanged -= RefreshVisual;
            }
        }

        private void OnValidate()
        {
            _glassMaterialSlot =
                Mathf.Max(0, _glassMaterialSlot);

            _fluidMaterialSlot =
                Mathf.Max(0, _fluidMaterialSlot);
        }

        private bool CacheMaterials()
        {
            if (_potion == null ||
                _bottleRenderer == null ||
                _usedGlassMaterial == null ||
                _invisibleFluidMaterial == null)
            {
                return false;
            }

            _availableMaterials =
                _bottleRenderer.sharedMaterials;

            if (_availableMaterials == null ||
                _availableMaterials.Length == 0 ||
                _glassMaterialSlot >=
                _availableMaterials.Length ||
                _fluidMaterialSlot >=
                _availableMaterials.Length)
            {
                return false;
            }

            _usedMaterials =
                (Material[])_availableMaterials.Clone();

            _usedMaterials[_glassMaterialSlot] =
                _usedGlassMaterial;

            _usedMaterials[_fluidMaterialSlot] =
                _invisibleFluidMaterial;

            return true;
        }

        private void RefreshVisual()
        {
            bool isUsed =
                _potion.State == PotionState.Used;

            _bottleRenderer.sharedMaterials =
                isUsed
                    ? _usedMaterials
                    : _availableMaterials;
        }
    }
}
