using UnityEngine;

namespace WizzardsCauldron.Presentation
{
    [DisallowMultipleComponent]
    public sealed class InitialViewOrientation : MonoBehaviour
    {
        [Header("XR References")]
        [SerializeField]
        private Transform _originRoot;

        [SerializeField]
        private Camera _xrCamera;

        [Header("Starting View")]
        [SerializeField]
        private Transform _lookTarget;

        private bool _hasAligned;

        private void LateUpdate()
        {
            if (_hasAligned)
            {
                return;
            }

            _hasAligned = true;

            if (_originRoot == null ||
                _xrCamera == null ||
                _lookTarget == null)
            {
                Debug.LogError(
                    "InitialViewOrientation is missing one or more " +
                    "required Inspector references.",
                    this);

                enabled = false;
                return;
            }

            Vector3 up = _originRoot.up;

            Vector3 currentForward =
                Vector3.ProjectOnPlane(
                    _xrCamera.transform.forward,
                    up);

            Vector3 desiredForward =
                Vector3.ProjectOnPlane(
                    _lookTarget.position -
                    _xrCamera.transform.position,
                    up);

            if (currentForward.sqrMagnitude < 0.0001f ||
                desiredForward.sqrMagnitude < 0.0001f)
            {
                Debug.LogError(
                    "InitialViewOrientation could not calculate " +
                    "a valid horizontal view direction.",
                    this);

                enabled = false;
                return;
            }

            float yaw = Vector3.SignedAngle(
                currentForward.normalized,
                desiredForward.normalized,
                up);

            _originRoot.RotateAround(
                _xrCamera.transform.position,
                up,
                yaw);

            enabled = false;
        }
    }
}