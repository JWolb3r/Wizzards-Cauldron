using UnityEngine;

namespace WizzardsCauldron.Interactions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsResettable : MonoBehaviour
    {
        [Header("Optional Interaction")]
        [Tooltip(
            "Assign the XR Grab Interactable so a held " +
            "object is released before its pose is restored.")]
        [SerializeField]
        private Behaviour _interactionBehaviour;

        private Rigidbody _rigidbody;
        private Transform _initialParent;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;
        private bool _initialIsKinematic;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            _initialParent = transform.parent;
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;
            _initialIsKinematic = _rigidbody.isKinematic;
        }

        public void RestoreInitialPose()
        {
            bool interactionWasEnabled =
                _interactionBehaviour != null &&
                _interactionBehaviour.enabled;

            if (interactionWasEnabled)
            {
                _interactionBehaviour.enabled = false;
            }

            _rigidbody.isKinematic = true;

            transform.SetParent(_initialParent, false);
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialLocalScale;

            _rigidbody.position = transform.position;
            _rigidbody.rotation = transform.rotation;

            Physics.SyncTransforms();

            _rigidbody.isKinematic = _initialIsKinematic;

            if (!_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.Sleep();
            }

            if (interactionWasEnabled)
            {
                _interactionBehaviour.enabled = true;
            }
        }
    }
}