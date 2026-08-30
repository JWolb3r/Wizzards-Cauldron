using System;
using UnityEngine;

namespace WizzardsCauldron.Presentation
{
    /// <summary>
    /// Prevents selected potion colliders from being expelled by an obsolete
    /// invisible layout collider that already overlaps their authored poses.
    /// Every other collision remains enabled, including the real worktop.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class PotionSpawnCollisionExclusion : MonoBehaviour
    {
        [SerializeField]
        private Collider _excludedSurface;

        [SerializeField]
        private Collider[] _potionColliders = Array.Empty<Collider>();

        public Collider ExcludedSurface => _excludedSurface;
        public Collider[] PotionColliders => _potionColliders;

        public void Configure(
            Collider excludedSurface,
            Collider[] potionColliders)
        {
            _excludedSurface = excludedSurface;
            _potionColliders = potionColliders != null
                ? (Collider[])potionColliders.Clone()
                : Array.Empty<Collider>();
        }

        private void Awake()
        {
            SetCollisionIgnored(true);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SetCollisionIgnored(true);
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                SetCollisionIgnored(false);
            }
        }

        private void SetCollisionIgnored(bool ignored)
        {
            if (_excludedSurface == null || _potionColliders == null)
            {
                return;
            }

            for (int index = 0; index < _potionColliders.Length; index++)
            {
                Collider potionCollider = _potionColliders[index];
                if (potionCollider != null)
                {
                    Physics.IgnoreCollision(
                        potionCollider,
                        _excludedSurface,
                        ignored);
                }
            }
        }
    }
}
