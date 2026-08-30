using System;
using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Presentation
{
    /// <summary>
    /// Makes a consumed potion disappear without deactivating its root object,
    /// so the existing room reset can restore the same interactive instance.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PotionController))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PotionConsumptionPresentation : MonoBehaviour
    {
        [SerializeField]
        private PotionController _potion;

        [SerializeField]
        private Behaviour _grabInteractable;

        private Renderer[] _renderers = Array.Empty<Renderer>();
        private Collider[] _colliders = Array.Empty<Collider>();
        private bool[] _rendererEnabledStates = Array.Empty<bool>();
        private bool[] _colliderEnabledStates = Array.Empty<bool>();
        private Rigidbody _rigidbody;
        private bool _initialGrabEnabled;
        private bool _initialIsKinematic;
        private bool _initialUseGravity;
        private bool _initialDetectCollisions;
        private bool _isConsumed;

        public PotionController Potion => _potion;
        public Behaviour GrabInteractable => _grabInteractable;
        public bool IsConsumed => _isConsumed;

        public void Configure(
            PotionController potion,
            Behaviour grabInteractable)
        {
            _potion = potion;
            _grabInteractable = grabInteractable;
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureInitialState();
        }

        private void OnEnable()
        {
            if (_potion == null)
            {
                ResolveReferences();
            }

            if (_potion == null)
            {
                Debug.LogError(
                    "PotionConsumptionPresentation has no PotionController.",
                    this);
                return;
            }

            _potion.StateChanged += RefreshVisibility;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (_potion != null)
            {
                _potion.StateChanged -= RefreshVisibility;
            }
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (_potion == null)
            {
                _potion = GetComponent<PotionController>();
            }

            if (_grabInteractable != null)
            {
                return;
            }

            Behaviour[] behaviours = GetComponents<Behaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                Behaviour candidate = behaviours[index];
                if (candidate != null &&
                    string.Equals(
                        candidate.GetType().Name,
                        "XRGrabInteractable",
                        StringComparison.Ordinal))
                {
                    _grabInteractable = candidate;
                    return;
                }
            }
        }

        private void CaptureInitialState()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            _rendererEnabledStates = new bool[_renderers.Length];
            _colliderEnabledStates = new bool[_colliders.Length];

            for (int index = 0; index < _renderers.Length; index++)
            {
                _rendererEnabledStates[index] =
                    _renderers[index] != null &&
                    _renderers[index].enabled;
            }

            for (int index = 0; index < _colliders.Length; index++)
            {
                _colliderEnabledStates[index] =
                    _colliders[index] != null &&
                    _colliders[index].enabled;
            }

            _initialGrabEnabled =
                _grabInteractable != null &&
                _grabInteractable.enabled;
            _initialIsKinematic = _rigidbody.isKinematic;
            _initialUseGravity = _rigidbody.useGravity;
            _initialDetectCollisions = _rigidbody.detectCollisions;
        }

        private void RefreshVisibility()
        {
            if (_potion.State == PotionState.Used)
            {
                Consume();
            }
            else if (_isConsumed)
            {
                Restore();
            }
        }

        private void Consume()
        {
            if (_isConsumed)
            {
                return;
            }

            _isConsumed = true;

            if (_grabInteractable != null)
            {
                _grabInteractable.enabled = false;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.detectCollisions = false;

            for (int index = 0; index < _colliders.Length; index++)
            {
                if (_colliders[index] != null)
                {
                    _colliders[index].enabled = false;
                }
            }

            for (int index = 0; index < _renderers.Length; index++)
            {
                if (_renderers[index] != null)
                {
                    _renderers[index].enabled = false;
                }
            }
        }

        private void Restore()
        {
            for (int index = 0; index < _renderers.Length; index++)
            {
                if (_renderers[index] != null)
                {
                    _renderers[index].enabled =
                        _rendererEnabledStates[index];
                }
            }

            for (int index = 0; index < _colliders.Length; index++)
            {
                if (_colliders[index] != null)
                {
                    _colliders[index].enabled =
                        _colliderEnabledStates[index];
                }
            }

            _rigidbody.detectCollisions = _initialDetectCollisions;
            _rigidbody.useGravity = _initialUseGravity;
            _rigidbody.isKinematic = _initialIsKinematic;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            if (_grabInteractable != null)
            {
                _grabInteractable.enabled = _initialGrabEnabled;
            }

            _isConsumed = false;
        }
    }
}
