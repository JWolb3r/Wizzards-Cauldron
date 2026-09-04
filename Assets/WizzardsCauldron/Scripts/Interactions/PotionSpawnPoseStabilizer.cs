using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Interactions
{
    /// <summary>
    /// Keeps an authored bottle pose stable until the player first grabs it.
    /// The original physics constraints are restored for normal grabbing,
    /// throwing and cauldron interaction.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PotionController))]
    public sealed class PotionSpawnPoseStabilizer : MonoBehaviour
    {
        [SerializeField]
        private PotionController _potion;

        [SerializeField]
        private XRGrabInteractable _grabInteractable;

        private Rigidbody _rigidbody;
        private RigidbodyConstraints _authoredConstraints;
        private RigidbodyInterpolation _authoredInterpolation;
        private bool _releasedForGameplay;

        public void Configure(
            PotionController potion,
            XRGrabInteractable grabInteractable)
        {
            _potion = potion;
            _grabInteractable = grabInteractable;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _authoredConstraints = _rigidbody.constraints;
            _authoredInterpolation = _rigidbody.interpolation;

            if (_potion == null)
            {
                _potion = GetComponent<PotionController>();
            }

            if (_grabInteractable == null)
            {
                _grabInteractable =
                    GetComponent<XRGrabInteractable>();
            }
        }

        private void OnEnable()
        {
            _releasedForGameplay = false;

            if (_potion != null)
            {
                _potion.StateChanged += HandlePotionStateChanged;
            }

            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(
                    HandleSelectEntered);
            }

            if (_potion != null && _potion.IsAvailable &&
                (_grabInteractable == null ||
                 !_grabInteractable.isSelected))
            {
                StabilizeAtSpawn();
            }
        }

        private void LateUpdate()
        {
            if (!_releasedForGameplay &&
                _potion != null &&
                _potion.IsAvailable &&
                (_grabInteractable == null ||
                 !_grabInteractable.isSelected))
            {
                // XRGrabInteractable can restore its cached Rigidbody
                // constraints when it is re-enabled after a reset. Reapply
                // the display-pose lock before the next physics step.
                StabilizeAtSpawn();
            }
        }

        private void OnDisable()
        {
            if (_potion != null)
            {
                _potion.StateChanged -= HandlePotionStateChanged;
            }

            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(
                    HandleSelectEntered);
            }
        }

        private void HandlePotionStateChanged()
        {
            if (_potion.IsAvailable)
            {
                _releasedForGameplay = false;
                StabilizeAtSpawn();
            }
            else
            {
                RestoreAuthoredConstraints();
            }
        }

        private void HandleSelectEntered(
            SelectEnterEventArgs _)
        {
            _releasedForGameplay = true;
            RestoreAuthoredConstraints();
        }

        private void StabilizeAtSpawn()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.constraints =
                _authoredConstraints |
                RigidbodyConstraints.FreezePosition;
            _rigidbody.Sleep();
        }

        private void RestoreAuthoredConstraints()
        {
            _rigidbody.constraints = _authoredConstraints;
            _rigidbody.interpolation = _authoredInterpolation;
            _rigidbody.WakeUp();
        }
    }
}
