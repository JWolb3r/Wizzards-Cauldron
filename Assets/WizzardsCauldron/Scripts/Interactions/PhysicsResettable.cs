using System.Collections;
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
        private Coroutine _reenableInteractionRoutine;

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
            bool interactionShouldBeEnabled =
                _interactionBehaviour != null &&
                (_interactionBehaviour.enabled ||
                 _reenableInteractionRoutine != null);

            if (_reenableInteractionRoutine != null)
            {
                StopCoroutine(_reenableInteractionRoutine);
                _reenableInteractionRoutine = null;
            }

            if (interactionShouldBeEnabled)
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

            if (interactionShouldBeEnabled)
            {
                // XR needs one complete frame with the grab component disabled
                // so a held object is actually released before it becomes
                // grabbable again at its restored start pose.
                _reenableInteractionRoutine = StartCoroutine(
                    ReenableInteractionNextFrame());
            }
        }

        private IEnumerator ReenableInteractionNextFrame()
        {
            yield return null;

            if (_interactionBehaviour != null)
            {
                _interactionBehaviour.enabled = true;
            }

            _reenableInteractionRoutine = null;
        }
    }
}
