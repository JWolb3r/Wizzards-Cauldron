using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace WizzardsCauldron.Interactions
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class WandHintInput : MonoBehaviour
    {
        private const string GrabAssistObjectName =
            "WC_WandGrabAssist";
        private const float GrabAssistRadius = 0.075f;
        private const float GrabAssistHeight = 0.55f;

        [SerializeField]
        private QuestCampaignController _campaign;

        [SerializeField]
        private XRGrabInteractable _wandInteractable;

        [SerializeField, Min(0f)]
        private float _minimumHeldTime = 0.15f;

        private float _selectedAt;
        private bool _subscribed;
        private CapsuleCollider _grabAssistCollider;

        public void Configure(
            QuestCampaignController campaign,
            XRGrabInteractable wandInteractable)
        {
            _campaign = campaign;
            _wandInteractable = wandInteractable;
        }

        private void Awake()
        {
            ResolveInteractable();

            if (Application.isPlaying)
            {
                EnsureGrabAssistCollider();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _subscribed)
            {
                return;
            }

            ResolveInteractable();
            EnsureGrabAssistCollider();
            if (_campaign == null ||
                _wandInteractable == null)
            {
                Debug.LogError(
                    "WandHintInput is missing the campaign or wand " +
                    "interactable reference.",
                    this);
                return;
            }

            _wandInteractable.selectEntered.AddListener(
                HandleSelectEntered);
            _wandInteractable.activated.AddListener(
                HandleActivated);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed || _wandInteractable == null)
            {
                return;
            }

            _wandInteractable.selectEntered.RemoveListener(
                HandleSelectEntered);
            _wandInteractable.activated.RemoveListener(
                HandleActivated);
            _subscribed = false;
        }

        private void OnValidate()
        {
            _minimumHeldTime = Mathf.Max(
                0f,
                _minimumHeldTime);
            ResolveInteractable();
        }

        private void HandleSelectEntered(
            SelectEnterEventArgs _)
        {
            _selectedAt = Time.unscaledTime;
        }

        private void HandleActivated(
            ActivateEventArgs args)
        {
            if (!_wandInteractable.isSelected ||
                Time.unscaledTime - _selectedAt <
                _minimumHeldTime)
            {
                return;
            }

            _campaign.TryUseHint(out _);
        }

        private void ResolveInteractable()
        {
            if (_wandInteractable == null)
            {
                _wandInteractable =
                    GetComponent<XRGrabInteractable>();
            }
        }

        private void EnsureGrabAssistCollider()
        {
            if (_wandInteractable == null)
            {
                return;
            }

            Transform assistTransform = transform.Find(
                GrabAssistObjectName);
            if (assistTransform == null)
            {
                var assistObject = new GameObject(
                    GrabAssistObjectName);
                assistObject.layer = gameObject.layer;
                assistTransform = assistObject.transform;
                assistTransform.SetParent(transform, false);
                assistTransform.localPosition = Vector3.zero;
                assistTransform.localRotation = Quaternion.identity;
                assistTransform.localScale = Vector3.one;
            }

            _grabAssistCollider =
                assistTransform.GetComponent<CapsuleCollider>();
            if (_grabAssistCollider == null)
            {
                _grabAssistCollider =
                    assistTransform.gameObject.AddComponent<
                        CapsuleCollider>();
            }

            _grabAssistCollider.isTrigger = true;
            _grabAssistCollider.direction = 1;
            _grabAssistCollider.center = Vector3.zero;
            _grabAssistCollider.radius = GrabAssistRadius;
            _grabAssistCollider.height = GrabAssistHeight;

            if (_wandInteractable.colliders.Contains(
                _grabAssistCollider))
            {
                return;
            }

            XRInteractionManager manager =
                _wandInteractable.interactionManager;
            bool wasRegistered =
                manager != null &&
                manager.IsRegistered(
                    (IXRInteractable)_wandInteractable);

            if (wasRegistered)
            {
                manager.UnregisterInteractable(
                    (IXRInteractable)_wandInteractable);
            }

            _wandInteractable.colliders.Add(
                _grabAssistCollider);

            if (wasRegistered)
            {
                manager.RegisterInteractable(
                    (IXRInteractable)_wandInteractable);
            }
        }
    }
}
