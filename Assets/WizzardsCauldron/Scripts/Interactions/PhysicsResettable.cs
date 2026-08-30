using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        private object[] _selectingInteractors = Array.Empty<object>();

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
            object[] selectingInteractors =
                CaptureSelectingInteractors();
            if (selectingInteractors.Length > 0)
            {
                _selectingInteractors = selectingInteractors;
            }

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

            // Disabling an XR Grab Interactable cancels the current grab, but
            // a controller whose select input is still held can immediately
            // grab it again. Keep the restored object unavailable until the
            // controller that held it has actually released its input.
            while (AnyCapturedInteractorStillSelecting())
            {
                yield return null;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.Sleep();

            if (_interactionBehaviour != null)
            {
                _interactionBehaviour.enabled = true;
            }

            _selectingInteractors = Array.Empty<object>();
            _reenableInteractionRoutine = null;
        }

        private object[] CaptureSelectingInteractors()
        {
            if (_interactionBehaviour == null)
            {
                return Array.Empty<object>();
            }

            PropertyInfo property = _interactionBehaviour.GetType()
                .GetProperty(
                    "interactorsSelecting",
                    BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetValue(_interactionBehaviour) is not
                IEnumerable interactors)
            {
                return Array.Empty<object>();
            }

            List<object> captured = new List<object>();
            foreach (object interactor in interactors)
            {
                if (interactor != null)
                {
                    captured.Add(interactor);
                }
            }

            return captured.ToArray();
        }

        private bool AnyCapturedInteractorStillSelecting()
        {
            for (int index = 0;
                 index < _selectingInteractors.Length;
                 index++)
            {
                object interactor = _selectingInteractors[index];
                if (interactor == null ||
                    interactor is UnityEngine.Object unityObject &&
                    unityObject == null)
                {
                    continue;
                }

                PropertyInfo property = interactor.GetType().GetProperty(
                    "isSelectActive",
                    BindingFlags.Instance | BindingFlags.Public);
                if (property?.GetValue(interactor) is bool isActive &&
                    isActive)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
