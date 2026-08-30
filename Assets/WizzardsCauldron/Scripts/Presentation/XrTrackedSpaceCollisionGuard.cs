using System;
using UnityEngine;

namespace WizzardsCauldron.Presentation
{
    /// <summary>
    /// Keeps tracked head movement inside the structural room colliders.
    /// The XR locomotion system already moves its CharacterController through
    /// physics, but room-scale tracking and the device simulator can move the
    /// camera inside the rig without moving that controller. This guard shifts
    /// only the rig root back by the blocked tracked-space displacement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XrTrackedSpaceCollisionGuard : MonoBehaviour
    {
        private const int MaximumCastHits = 32;

        [SerializeField]
        private Transform _rigRoot;

        [SerializeField]
        private Transform _head;

        [SerializeField]
        private Collider[] _blockingColliders = Array.Empty<Collider>();

        [SerializeField, Min(0.08f)]
        private float _bodyRadius = 0.18f;

        [SerializeField, Min(0.001f)]
        private float _collisionSkin = 0.015f;

        private readonly RaycastHit[] _castHits =
            new RaycastHit[MaximumCastHits];

        private Vector3 _lastHeadPosition;
        private bool _hasLastHeadPosition;

        public Transform RigRoot => _rigRoot;
        public Transform Head => _head;
        public Collider[] BlockingColliders => _blockingColliders;

        public void Configure(
            Transform rigRoot,
            Transform head,
            Collider[] blockingColliders)
        {
            _rigRoot = rigRoot;
            _head = head;
            _blockingColliders = blockingColliders != null
                ? (Collider[])blockingColliders.Clone()
                : Array.Empty<Collider>();

            ResetTrackingState();
        }

        private void OnEnable()
        {
            ResetTrackingState();
        }

        private void LateUpdate()
        {
            if (_rigRoot == null || _head == null)
            {
                return;
            }

            Vector3 currentHeadPosition = _head.position;
            if (!_hasLastHeadPosition)
            {
                _lastHeadPosition = currentHeadPosition;
                _hasLastHeadPosition = true;
                return;
            }

            Vector3 horizontalDisplacement =
                currentHeadPosition - _lastHeadPosition;
            horizontalDisplacement.y = 0f;

            float distance = horizontalDisplacement.magnitude;
            if (distance <= 0.0001f)
            {
                _lastHeadPosition = currentHeadPosition;
                return;
            }

            Vector3 direction = horizontalDisplacement / distance;
            float allowedDistance = FindAllowedDistance(
                _lastHeadPosition,
                direction,
                distance);

            if (allowedDistance < distance)
            {
                Vector3 correction = direction *
                    (allowedDistance - distance);

                _rigRoot.position += correction;
                Physics.SyncTransforms();
            }

            _lastHeadPosition = _head.position;
        }

        private float FindAllowedDistance(
            Vector3 previousHeadPosition,
            Vector3 direction,
            float requestedDistance)
        {
            float floorY = _rigRoot.position.y;
            float bottomY = floorY + _bodyRadius + _collisionSkin;
            float topY = Mathf.Max(
                bottomY,
                previousHeadPosition.y - _bodyRadius);

            Vector3 bottom = new Vector3(
                previousHeadPosition.x,
                bottomY,
                previousHeadPosition.z);
            Vector3 top = new Vector3(
                previousHeadPosition.x,
                topY,
                previousHeadPosition.z);

            int hitCount = Physics.CapsuleCastNonAlloc(
                bottom,
                top,
                _bodyRadius,
                direction,
                _castHits,
                requestedDistance + _collisionSkin,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = requestedDistance;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _castHits[index];
                if (!IsBlockingCollider(hit.collider))
                {
                    continue;
                }

                nearestDistance = Mathf.Min(
                    nearestDistance,
                    Mathf.Max(0f, hit.distance - _collisionSkin));
            }

            return nearestDistance;
        }

        private bool IsBlockingCollider(Collider candidate)
        {
            if (candidate == null || _blockingColliders == null)
            {
                return false;
            }

            for (int index = 0;
                 index < _blockingColliders.Length;
                 index++)
            {
                if (_blockingColliders[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetTrackingState()
        {
            _hasLastHeadPosition = _head != null;
            if (_hasLastHeadPosition)
            {
                _lastHeadPosition = _head.position;
            }
        }

        private void OnValidate()
        {
            _bodyRadius = Mathf.Max(0.08f, _bodyRadius);
            _collisionSkin = Mathf.Max(0.001f, _collisionSkin);
        }
    }
}
