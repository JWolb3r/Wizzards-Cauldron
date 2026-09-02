using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WizzardsCauldron.Interactions
{
    /// <summary>
    /// Adds Meta Quest face buttons and applies the room locomotion layout to
    /// the existing XRI actions at runtime. The imported XRI sample asset stays
    /// unchanged, and its original grip/trigger bindings remain available.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class QuestFaceButtonBindings : MonoBehaviour
    {
        public const string LeftPrimaryButtonPath =
            "<XRController>{LeftHand}/primaryButton";
        public const string LeftSecondaryButtonPath =
            "<XRController>{LeftHand}/secondaryButton";
        public const string RightPrimaryButtonPath =
            "<XRController>{RightHand}/primaryButton";
        public const string RightSecondaryButtonPath =
            "<XRController>{RightHand}/secondaryButton";

        private const string LeftInteractionMap =
            "XRI Left Interaction";
        private const string RightInteractionMap =
            "XRI Right Interaction";
        private const string LeftLocomotionMap =
            "XRI Left Locomotion";
        private const string RightLocomotionMap =
            "XRI Right Locomotion";

        [SerializeField]
        private InputActionAsset _inputActions;

        public InputActionAsset InputActions => _inputActions;

        public void Configure(InputActionAsset inputActions)
        {
            _inputActions = inputActions;
        }

        private void Awake()
        {
            if (!TryGetConfigurationError(out string error))
            {
                Debug.LogError(
                    "[WC_QUEST_CONTROLS] " + error,
                    this);
                enabled = false;
                return;
            }

            ApplyBindings();
            Debug.Log(
                "[WC_QUEST_CONTROLS] Left stick moves, right stick turns, " +
                "jump and stick teleport are disabled. A/B (right) and " +
                "X/Y (left) select grabbable objects; B/Y also activate. " +
                "Head tracking plus physical room movement remain active.",
                this);
        }

        /// <summary>
        /// Applies the additive runtime bindings. Calling this more than once
        /// is safe and never duplicates a binding.
        /// </summary>
        public void ApplyBindings()
        {
            if (!TryGetConfigurationError(out string error))
            {
                throw new InvalidOperationException(error);
            }

            InputActionMap leftMap =
                _inputActions.FindActionMap(LeftInteractionMap, true);
            InputActionMap rightMap =
                _inputActions.FindActionMap(RightInteractionMap, true);
            InputActionMap leftLocomotion =
                _inputActions.FindActionMap(LeftLocomotionMap, true);
            InputActionMap rightLocomotion =
                _inputActions.FindActionMap(RightLocomotionMap, true);

            var enabledActions = new List<InputAction>();
            RememberEnabledActions(leftMap, enabledActions);
            RememberEnabledActions(rightMap, enabledActions);
            RememberEnabledActions(leftLocomotion, enabledActions);
            RememberEnabledActions(rightLocomotion, enabledActions);
            leftMap.Disable();
            rightMap.Disable();
            leftLocomotion.Disable();
            rightLocomotion.Disable();

            try
            {
                AddSelectBindings(
                    leftMap,
                    LeftPrimaryButtonPath,
                    LeftSecondaryButtonPath);
                AddSelectBindings(
                    rightMap,
                    RightPrimaryButtonPath,
                    RightSecondaryButtonPath);

                AddBindingIfMissing(
                    leftMap.FindAction("Activate", true),
                    LeftSecondaryButtonPath);
                AddBindingIfMissing(
                    leftMap.FindAction("Activate Value", true),
                    LeftSecondaryButtonPath);
                AddBindingIfMissing(
                    rightMap.FindAction("Activate", true),
                    RightSecondaryButtonPath);
                AddBindingIfMissing(
                    rightMap.FindAction("Activate Value", true),
                    RightSecondaryButtonPath);

                // The XRI Starter Assets let both sticks move and turn, and
                // bind the Quest A button to Jump. This room deliberately uses
                // one stick per role: left moves and right turns continuously.
                // Runtime overrides keep Unity's imported sample asset intact.
                DisableAllBindings(
                    leftLocomotion.FindAction("Teleport Mode", true));
                DisableAllBindings(
                    leftLocomotion.FindAction("Turn", true));
                DisableAllBindings(
                    leftLocomotion.FindAction("Snap Turn", true));

                DisableAllBindings(
                    rightLocomotion.FindAction("Teleport Mode", true));
                DisableAllBindings(
                    rightLocomotion.FindAction("Move", true));
                DisableAllBindings(
                    rightLocomotion.FindAction("Snap Turn", true));
                DisableAllBindings(
                    rightLocomotion.FindAction("Jump", true));
            }
            finally
            {
                for (int index = 0;
                     index < enabledActions.Count;
                     index++)
                {
                    enabledActions[index].Enable();
                }
            }
        }

        /// <summary>
        /// Verifies the assigned XRI asset without changing it.
        /// </summary>
        public bool TryGetConfigurationError(out string error)
        {
            if (_inputActions == null)
            {
                error = "No XRI Input Action Asset is assigned.";
                return false;
            }

            string[] requiredActions =
            {
                LeftInteractionMap + "/Select",
                LeftInteractionMap + "/Select Value",
                LeftInteractionMap + "/Activate",
                LeftInteractionMap + "/Activate Value",
                RightInteractionMap + "/Select",
                RightInteractionMap + "/Select Value",
                RightInteractionMap + "/Activate",
                RightInteractionMap + "/Activate Value",
                LeftLocomotionMap + "/Teleport Mode",
                LeftLocomotionMap + "/Turn",
                LeftLocomotionMap + "/Snap Turn",
                LeftLocomotionMap + "/Move",
                RightLocomotionMap + "/Teleport Mode",
                RightLocomotionMap + "/Turn",
                RightLocomotionMap + "/Snap Turn",
                RightLocomotionMap + "/Move",
                RightLocomotionMap + "/Jump",
                "XRI Head/Position",
                "XRI Head/Rotation"
            };

            for (int index = 0;
                 index < requiredActions.Length;
                 index++)
            {
                if (_inputActions.FindAction(
                    requiredActions[index],
                    false) == null)
                {
                    error = "The assigned XRI Input Action Asset is missing '" +
                        requiredActions[index] + "'.";
                    return false;
                }
            }

            if (!HasBinding(
                    LeftLocomotionMap + "/Move",
                    "<XRController>{LeftHand}/{Primary2DAxis}") ||
                !HasBinding(
                    RightLocomotionMap + "/Turn",
                    "<XRController>{RightHand}/{Primary2DAxis}"))
            {
                error = "The locomotion actions are missing the required " +
                    "left-move or right-turn Quest thumbstick binding.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void AddSelectBindings(
            InputActionMap map,
            string primaryButtonPath,
            string secondaryButtonPath)
        {
            InputAction select = map.FindAction("Select", true);
            InputAction selectValue = map.FindAction(
                "Select Value",
                true);

            AddBindingIfMissing(select, primaryButtonPath);
            AddBindingIfMissing(select, secondaryButtonPath);
            AddBindingIfMissing(selectValue, primaryButtonPath);
            AddBindingIfMissing(selectValue, secondaryButtonPath);
        }

        private static void AddBindingIfMissing(
            InputAction action,
            string path)
        {
            for (int index = 0;
                 index < action.bindings.Count;
                 index++)
            {
                if (string.Equals(
                    action.bindings[index].path,
                    path,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            action.AddBinding(path);
        }

        private static void DisableAllBindings(InputAction action)
        {
            for (int index = 0;
                 index < action.bindings.Count;
                 index++)
            {
                action.ApplyBindingOverride(index, string.Empty);
            }
        }

        private bool HasBinding(
            string actionPath,
            string bindingPath)
        {
            InputAction action = _inputActions.FindAction(
                actionPath,
                false);
            if (action == null)
            {
                return false;
            }

            for (int index = 0;
                 index < action.bindings.Count;
                 index++)
            {
                if (string.Equals(
                    action.bindings[index].path,
                    bindingPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RememberEnabledActions(
            InputActionMap map,
            ICollection<InputAction> enabledActions)
        {
            for (int index = 0;
                 index < map.actions.Count;
                 index++)
            {
                InputAction action = map.actions[index];
                if (action.enabled)
                {
                    enabledActions.Add(action);
                }
            }
        }
    }
}
