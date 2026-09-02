using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.Tests.EditMode
{
    public sealed class QuestFaceButtonBindingsTests
    {
        [Test]
        public void ApplyBindings_AddsAllFaceButtonsAndKeepsExistingBindings()
        {
            InputActionAsset actions = CreateInputActions();
            var gameObject = new GameObject("Quest controls test");

            try
            {
                QuestFaceButtonBindings bindings =
                    gameObject.AddComponent<QuestFaceButtonBindings>();
                bindings.Configure(actions);

                bindings.ApplyBindings();
                bindings.ApplyBindings();

                AssertBinding(
                    actions,
                    "XRI Left Interaction/Select",
                    QuestFaceButtonBindings.LeftPrimaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Left Interaction/Select",
                    QuestFaceButtonBindings.LeftSecondaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Left Interaction/Select Value",
                    QuestFaceButtonBindings.LeftPrimaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Left Interaction/Select Value",
                    QuestFaceButtonBindings.LeftSecondaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Right Interaction/Select",
                    QuestFaceButtonBindings.RightPrimaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Right Interaction/Select",
                    QuestFaceButtonBindings.RightSecondaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Right Interaction/Select Value",
                    QuestFaceButtonBindings.RightPrimaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Right Interaction/Select Value",
                    QuestFaceButtonBindings.RightSecondaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Left Interaction/Activate",
                    QuestFaceButtonBindings.LeftSecondaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Left Interaction/Activate Value",
                    QuestFaceButtonBindings.LeftSecondaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Right Interaction/Activate",
                    QuestFaceButtonBindings.RightSecondaryButtonPath);
                AssertBinding(
                    actions,
                    "XRI Right Interaction/Activate Value",
                    QuestFaceButtonBindings.RightSecondaryButtonPath);

                AssertBinding(
                    actions,
                    "XRI Left Interaction/Select",
                    "<XRController>{LeftHand}/{GripButton}");
                Assert.AreEqual(
                    1,
                    actions.FindAction(
                            "XRI Right Interaction/Select",
                            true)
                        .bindings.Count(binding =>
                            binding.path ==
                            QuestFaceButtonBindings.RightPrimaryButtonPath));

                AssertActiveBinding(
                    actions,
                    "XRI Left Locomotion/Move",
                    "<XRController>{LeftHand}/{Primary2DAxis}");
                AssertActiveBinding(
                    actions,
                    "XRI Right Locomotion/Turn",
                    "<XRController>{RightHand}/{Primary2DAxis}");

                AssertAllBindingsDisabled(
                    actions,
                    "XRI Left Locomotion/Teleport Mode");
                AssertAllBindingsDisabled(
                    actions,
                    "XRI Left Locomotion/Turn");
                AssertAllBindingsDisabled(
                    actions,
                    "XRI Left Locomotion/Snap Turn");
                AssertAllBindingsDisabled(
                    actions,
                    "XRI Right Locomotion/Teleport Mode");
                AssertAllBindingsDisabled(
                    actions,
                    "XRI Right Locomotion/Move");
                AssertAllBindingsDisabled(
                    actions,
                    "XRI Right Locomotion/Snap Turn");
                AssertAllBindingsDisabled(
                    actions,
                    "XRI Right Locomotion/Jump");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(actions);
            }
        }

        private static InputActionAsset CreateInputActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            AddInteractionMap(asset, "XRI Left Interaction", true);
            AddInteractionMap(asset, "XRI Right Interaction", false);

            AddLocomotionMap(asset, "Left", false);
            AddLocomotionMap(asset, "Right", true);

            InputActionMap head = asset.AddActionMap("XRI Head");
            head.AddAction(
                "Position",
                InputActionType.Value,
                expectedControlLayout: "Vector3");
            head.AddAction(
                "Rotation",
                InputActionType.Value,
                expectedControlLayout: "Quaternion");

            return asset;
        }

        private static void AddLocomotionMap(
            InputActionAsset asset,
            string hand,
            bool addJump)
        {
            InputActionMap map = asset.AddActionMap(
                "XRI " + hand + " Locomotion");
            string controllerPath =
                "<XRController>{" + hand + "Hand}/{Primary2DAxis}";

            AddVector2Action(map, "Teleport Mode", controllerPath);
            AddVector2Action(map, "Turn", controllerPath);
            AddVector2Action(map, "Snap Turn", controllerPath);
            AddVector2Action(map, "Move", controllerPath);

            if (addJump)
            {
                map.AddAction("Jump", InputActionType.Button)
                    .AddBinding(
                        "<XRController>{RightHand}/{PrimaryButton}");
            }
        }

        private static void AddVector2Action(
            InputActionMap map,
            string name,
            string bindingPath)
        {
            map.AddAction(
                    name,
                    InputActionType.Value,
                    expectedControlLayout: "Vector2")
                .AddBinding(bindingPath);
        }

        private static void AddInteractionMap(
            InputActionAsset asset,
            string mapName,
            bool addExistingGripBinding)
        {
            InputActionMap map = asset.AddActionMap(mapName);
            InputAction select = map.AddAction(
                "Select",
                InputActionType.Button);
            map.AddAction(
                "Select Value",
                InputActionType.Value,
                expectedControlLayout: "Axis");
            map.AddAction("Activate", InputActionType.Button);
            map.AddAction(
                "Activate Value",
                InputActionType.Value,
                expectedControlLayout: "Axis");

            if (addExistingGripBinding)
            {
                select.AddBinding(
                    "<XRController>{LeftHand}/{GripButton}");
            }
        }

        private static void AssertBinding(
            InputActionAsset asset,
            string actionPath,
            string bindingPath)
        {
            InputAction action = asset.FindAction(actionPath, true);
            Assert.IsTrue(
                action.bindings.Any(binding => binding.path == bindingPath),
                actionPath + " is missing " + bindingPath);
        }

        private static void AssertActiveBinding(
            InputActionAsset asset,
            string actionPath,
            string bindingPath)
        {
            InputAction action = asset.FindAction(actionPath, true);
            Assert.IsTrue(
                action.bindings.Any(binding =>
                    binding.path == bindingPath &&
                    binding.effectivePath == bindingPath),
                actionPath + " should keep " + bindingPath + " active.");
        }

        private static void AssertAllBindingsDisabled(
            InputActionAsset asset,
            string actionPath)
        {
            InputAction action = asset.FindAction(actionPath, true);
            Assert.IsNotEmpty(
                action.bindings,
                actionPath + " must have a source binding for this test.");
            Assert.IsTrue(
                action.bindings.All(binding =>
                    string.IsNullOrEmpty(binding.effectivePath)),
                actionPath + " should have no effective runtime bindings.");
        }
    }
}
