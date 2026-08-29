using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.Presentation;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Read-only validation for the generated visual pass. The validator never
    /// saves a scene or changes build settings; scenes opened by this utility are
    /// always closed with their in-memory changes discarded.
    /// </summary>
    public static class VisualPassValidator
    {
        private const string LogPrefix = "[WC_VISUAL_VALIDATION]";
        private const string SourceScenePath =
            "Assets/WizzardsCauldron/Scenes/SCN_InteractionTest.unity";
        private const string TargetScenePath =
            "Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string GeneratedRootName = "__WC_VISUAL_PASS__";
        private const string GameplayExtensionRootName =
            "__WC_GAMEPLAY_EXTENSION__";
        private const string ExpandedPuzzlePath =
            "Assets/WizzardsCauldron/Data/Puzzles/SO_PuzzleVisualExpanded.asset";
        private const string PerformanceUrpAssetPath =
            "Assets/Settings/Project Configuration/Performance URP Config.asset";
        private const string ExpectedSourceSha256 =
            "2A13B8401B8C493386575CBBB09E09BA43F1BCFBEEE3C7420D7EB106A638B0BC";

        private const int MaximumRealtimeLights = 4;
        private const int MaximumParticlesPerSystem = 48;
        private const int MaximumTotalParticles = 128;
        private const int ExpectedPotionCount = 8;
        private const int ExpectedPhysicsResettableCount = 9;

        private static readonly PotionExpectation[] ExpectedNewPotions =
        {
            new PotionExpectation("blue", 2, 1),
            new PotionExpectation("violet", 5, 3),
            new PotionExpectation("cyan", 7, 4),
            new PotionExpectation("orange", 4, 2),
            new PotionExpectation("magenta", 9, 5)
        };

        private static readonly AuthorizedLayoutExpectation[]
            AuthorizedVisualLayout =
        {
            new AuthorizedLayoutExpectation(
                "Placeholders/Cauldron",
                new Vector3(1.36f, 0.845f, 1.05f),
                Quaternion.identity,
                new Vector3(0.38f, 0.35f, 0.38f),
                0),
            new AuthorizedLayoutExpectation(
                "Placeholders/FinishTarget",
                new Vector3(0.949f, 1.2f, 1.05f),
                Quaternion.identity,
                new Vector3(0.15f, 0.15f, 0.15f),
                0),
            new AuthorizedLayoutExpectation(
                "Placeholders/PotionShelf",
                new Vector3(-0.65f, 1f, 1f),
                Quaternion.identity,
                new Vector3(1.065f, 0.08f, 0.4f),
                0),
            new AuthorizedLayoutExpectation(
                "Placeholders/ResetControl",
                new Vector3(1.9185f, 1.2f, 0.7f),
                Quaternion.identity,
                Vector3.one,
                0),
            new AuthorizedLayoutExpectation(
                "Placeholders/Wand",
                new Vector3(0.286f, 0.78f, 0.859f),
                new Quaternion(0f, 0f, 0.7071068f, 0.7071068f),
                Vector3.one,
                0),
            new AuthorizedLayoutExpectation(
                "Placeholders/WandTable",
                new Vector3(0.05f, 0.712f, 0.95f),
                Quaternion.identity,
                new Vector3(1.42f, 0.07f, 0.3f),
                0),
            new AuthorizedLayoutExpectation(
                "XR Origin (XR Rig)",
                new Vector3(-1.12f, 0f, -0.276f),
                Quaternion.identity,
                Vector3.one,
                2)
        };

        private static readonly HashSet<string> KnownGameplayComponentTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "WizzardsCauldron.Core.CauldronController",
                "WizzardsCauldron.Core.GameSessionController",
                "WizzardsCauldron.Core.PotionController",
                "WizzardsCauldron.Interactions.CauldronIntake",
                "WizzardsCauldron.Interactions.PhysicsResettable",
                "WizzardsCauldron.Interactions.PotionInspectionSource",
                "WizzardsCauldron.Interactions.ResetHoldControl",
                "WizzardsCauldron.Interactions.RoomResetCoordinator",
                "WizzardsCauldron.Interactions.WandActivator",
                "WizzardsCauldron.Interactions.WandTip",
                "WizzardsCauldron.Presentation.CauldronLiquidDisplay",
                "WizzardsCauldron.UI.CauldronStatusPanel",
                "WizzardsCauldron.UI.PotionInspectionPanel",
                "WizzardsCauldron.UI.ResultPanel"
            };

        private static readonly WrapperExpectation[] ExpectedAlexWrappers =
        {
            new WrapperExpectation("Cauldron", "cauldron", "kessel"),
            new WrapperExpectation("Wand", "wand", "zauberstab"),
            new WrapperExpectation("Potion/Bottle", "potion", "bottle", "flasche"),
            new WrapperExpectation(
                "Shelf/Table",
                "shelf",
                "table",
                "furniture",
                "regal",
                "tisch"),
            new WrapperExpectation("Reset", "reset", "button", "knopf")
        };

        [MenuItem("Tools/Wizzards Cauldron/Validate Visual Pass", priority = 101)]
        public static void ValidateVisualPass()
        {
            ValidationReport report = RunValidation();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Wizzards Cauldron - Visual Pass",
                    report.BuildDialogSummary(),
                    "OK");
            }
        }

        /// <summary>
        /// Entry point for Unity's -executeMethod option. Warnings are reported
        /// but do not fail the process; validation errors throw after the full
        /// summary has been written to the log.
        /// </summary>
        public static void ValidateVisualPassBatch()
        {
            ValidationReport report = RunValidation();
            if (report.ErrorCount > 0)
            {
                throw new InvalidOperationException(
                    LogPrefix + " Visual-pass validation failed with " +
                    report.ErrorCount + " error(s). See the preceding validation log.");
            }
        }

        internal static void AssertProtectedStateMatchesForBuild(
            Scene sourceScene,
            Scene targetScene)
        {
            if (!sourceScene.IsValid() || !sourceScene.isLoaded ||
                !targetScene.IsValid() || !targetScene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Visual build preflight requires loaded source and target scenes.");
            }

            AssertAuthorizedVisualLayout(targetScene);

            ProtectedSceneSnapshot source =
                CaptureProtectedSceneSnapshot(sourceScene);
            ProtectedSceneSnapshot target =
                CaptureProtectedSceneSnapshot(targetScene);
            List<string> differences = new List<string>();
            CompareStringDictionaries(
                "protected object",
                source.ObjectStates,
                target.ObjectStates,
                differences);
            CompareComponentDictionaries(
                source.ComponentStates,
                target.ComponentStates,
                differences);

            if (differences.Count > 0)
            {
                Debug.LogError(
                    "[WC_VISUAL_PASS] Protected preflight differences: " +
                    string.Join(" | ", differences.Take(50).ToArray()));
                throw new InvalidOperationException(
                    "Protected target-scene state differs from the functional " +
                    "source in " + differences.Count + " place(s). Visual build " +
                    "stopped before generating or saving content. First difference: " +
                    differences[0]);
            }

            Debug.Log(
                "[WC_VISUAL_PASS] Protected-state preflight passed: " +
                source.ComponentStates.Count + " serialized component(s), " +
                source.ObjectStates.Count + " protected pose(s).");
        }

        private static ValidationReport RunValidation()
        {
            ValidationReport report = new ValidationReport();
            Scene sourceScene = default(Scene);
            Scene targetScene = default(Scene);
            Scene previouslyActiveScene = SceneManager.GetActiveScene();
            bool sourceOpenedByValidator = false;
            bool targetOpenedByValidator = false;

            try
            {
                ValidateProtectedSourceScene(report);
                ValidateBuildSettings(report);

                bool sourceExists = ValidateSceneAsset(
                    SourceScenePath,
                    "Protected source scene",
                    report);
                bool targetExists = ValidateSceneAsset(
                    TargetScenePath,
                    "Visual target scene",
                    report);

                if (!sourceExists || !targetExists)
                {
                    return FinishReport(report);
                }

                sourceScene = OpenSceneReadOnly(
                    SourceScenePath,
                    "protected source",
                    report,
                    out sourceOpenedByValidator);
                targetScene = OpenSceneReadOnly(
                    TargetScenePath,
                    "visual target",
                    report,
                    out targetOpenedByValidator);

                ValidateScene(targetScene, report);
                ValidateProtectedSceneState(sourceScene, targetScene, report);
            }
            catch (Exception exception)
            {
                report.Error(
                    "Validation aborted unexpectedly: " +
                    exception.GetType().Name + ": " + exception.Message);
                Debug.LogException(exception);
            }
            finally
            {
                if (previouslyActiveScene.IsValid() &&
                    previouslyActiveScene.isLoaded &&
                    SceneManager.GetActiveScene() != previouslyActiveScene)
                {
                    SceneManager.SetActiveScene(previouslyActiveScene);
                }

                if (targetOpenedByValidator &&
                    targetScene.IsValid() &&
                    targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                if (sourceOpenedByValidator &&
                    sourceScene.IsValid() &&
                    sourceScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }

            return FinishReport(report);
        }

        private static ValidationReport FinishReport(ValidationReport report)
        {
            report.LogSummary();
            return report;
        }

        private static void ValidateProtectedSourceScene(ValidationReport report)
        {
            string fullPath = GetProjectAbsolutePath(SourceScenePath);
            if (!File.Exists(fullPath))
            {
                report.Error("Protected source scene is missing: " + SourceScenePath);
                return;
            }

            string actualHash;
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(fullPath))
            {
                actualHash = BitConverter.ToString(sha256.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }

            if (string.Equals(
                    actualHash,
                    ExpectedSourceSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.Ok(
                    "Protected source scene SHA256 is unchanged: " + actualHash);
            }
            else
            {
                report.Error(
                    "Protected source scene SHA256 changed. Expected " +
                    ExpectedSourceSha256 + ", found " + actualHash + ".");
            }
        }

        private static bool ValidateSceneAsset(
            string scenePath,
            string label,
            ValidationReport report)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null || !File.Exists(GetProjectAbsolutePath(scenePath)))
            {
                report.Error(label + " is missing: " + scenePath);
                return false;
            }

            report.Ok(label + " exists: " + scenePath);
            return true;
        }

        private static Scene OpenSceneReadOnly(
            string scenePath,
            string label,
            ValidationReport report,
            out bool openedByValidator)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            openedByValidator = !scene.IsValid() || !scene.isLoaded;

            if (openedByValidator)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }
            else if (scene.isDirty)
            {
                report.Warn(
                    "The " + label + " scene was already open and dirty; " +
                    "its in-memory state was inspected without saving it.");
            }

            return scene;
        }

        private static void ValidateScene(Scene scene, ValidationReport report)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.Error("Visual target scene could not be loaded for validation.");
                return;
            }

            GameObject[] sceneRoots = scene.GetRootGameObjects();
            GameObject[] generatedRoots = sceneRoots
                .Where(root => string.Equals(
                    root.name,
                    GeneratedRootName,
                    StringComparison.Ordinal))
                .ToArray();

            GameObject generatedRoot = null;
            if (generatedRoots.Length != 1)
            {
                report.Error(
                    "Expected exactly one root named " + GeneratedRootName +
                    ", found " + generatedRoots.Length + ".");
            }
            else
            {
                generatedRoot = generatedRoots[0];
                report.Ok("Generated visual root exists exactly once.");
                ValidateGeneratedRoot(generatedRoot, report);
            }

            GameObject[] gameplayExtensionRoots = sceneRoots
                .Where(root => string.Equals(
                    root.name,
                    GameplayExtensionRootName,
                    StringComparison.Ordinal))
                .ToArray();
            if (gameplayExtensionRoots.Length != 1)
            {
                report.Error(
                    "Expected exactly one authorized gameplay-extension root named " +
                    GameplayExtensionRootName + ", found " +
                    gameplayExtensionRoots.Length + ".");
            }
            else
            {
                report.Ok("Authorized gameplay-extension root exists exactly once.");
                ValidateGameplayExtension(
                    gameplayExtensionRoots[0],
                    sceneRoots,
                    report);
            }

            Component[] sceneComponents = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .ToArray();
            ValidateNamedComponentCount(
                sceneComponents,
                "VisualFeedbackController",
                1,
                report);
            ValidateNamedComponentCount(
                sceneComponents,
                "AstralPortalAnimator",
                1,
                report);

            ValidateControllerCounts(sceneRoots, report);
            Transform[] alexWrapperRoots = ValidateAlexWrapperNames(sceneRoots, report);
            ValidateAlexWrapperSubtrees(alexWrapperRoots, generatedRoot, report);
            ValidateMissingScripts(sceneRoots, report);
            ValidateRealtimeLights(sceneRoots, report);
            ValidateQuestRenderFallback(sceneRoots, report);
            ValidateParticles(sceneRoots, report);
        }

        private static void ValidateProtectedSceneState(
            Scene sourceScene,
            Scene targetScene,
            ValidationReport report)
        {
            if (!sourceScene.IsValid() || !sourceScene.isLoaded ||
                !targetScene.IsValid() || !targetScene.isLoaded)
            {
                report.Error(
                    "Source/target protected-state comparison requires both scenes loaded.");
                return;
            }

            ProtectedSceneSnapshot source = CaptureProtectedSceneSnapshot(sourceScene);
            ProtectedSceneSnapshot target = CaptureProtectedSceneSnapshot(targetScene);
            List<string> differences = new List<string>();

            CompareStringDictionaries(
                "protected object",
                source.ObjectStates,
                target.ObjectStates,
                differences);
            CompareComponentDictionaries(
                source.ComponentStates,
                target.ComponentStates,
                differences);

            if (differences.Count == 0)
            {
                report.Ok(
                    "Protected source/target state is identical across " +
                    source.ComponentStates.Count + " serialized component(s) and " +
                    source.ObjectStates.Count + " protected GameObject pose(s). " +
                    "Coverage: " + source.ColliderCount + " Collider, " +
                    source.RigidbodyCount + " Rigidbody, " + source.XrCount +
                    " XR, " + source.GameplayCount + " gameplay/controller component(s).");
            }
            else
            {
                const int maximumListedDifferences = 30;
                report.Error(
                    "Protected gameplay/collider/XR state differs from the source in " +
                    differences.Count + " place(s): " +
                    string.Join(
                        " | ",
                        differences.Take(maximumListedDifferences).ToArray()) +
                    (differences.Count > maximumListedDifferences
                        ? " | ... and " +
                          (differences.Count - maximumListedDifferences) + " more"
                        : string.Empty));
            }

            ValidateAuthorizedVisualLayout(targetScene, report);
            ValidateExplicitInteractionIntegrity(targetScene.GetRootGameObjects(), report);
        }

        private static void AssertAuthorizedVisualLayout(Scene targetScene)
        {
            List<string> issues = GetAuthorizedVisualLayoutIssues(targetScene);
            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    "The dedicated visual scene no longer matches its approved " +
                    "target-only gameplay layout: " +
                    string.Join(" | ", issues.ToArray()));
            }

            Debug.Log(
                "[WC_VISUAL_PASS] Approved visual-scene layout passed: " +
                AuthorizedVisualLayout.Length +
                " exact target-only poses and cauldron liquid offset 0.6.");
        }

        private static void ValidateAuthorizedVisualLayout(
            Scene targetScene,
            ValidationReport report)
        {
            List<string> issues = GetAuthorizedVisualLayoutIssues(targetScene);
            if (issues.Count == 0)
            {
                report.Ok(
                    "Approved target-only layout is exact: cauldron, finish target, " +
                    "potion shelf, reset, wand, worktable and XR start pose.");
            }
            else
            {
                report.Error(
                    "Approved target-only layout differs in " + issues.Count +
                    " place(s): " + string.Join(" | ", issues.ToArray()));
            }
        }

        private static List<string> GetAuthorizedVisualLayoutIssues(
            Scene targetScene)
        {
            List<string> issues = new List<string>();
            Transform[] transforms = targetScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();

            foreach (AuthorizedLayoutExpectation expectation in
                     AuthorizedVisualLayout)
            {
                Transform[] matches = transforms
                    .Where(transform => string.Equals(
                        GetHierarchyPath(transform),
                        expectation.HierarchyPath,
                        StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length != 1)
                {
                    issues.Add(
                        expectation.HierarchyPath + " expected once, found " +
                        matches.Length);
                    continue;
                }

                Transform transform = matches[0];
                bool poseMatches =
                    (transform.localPosition - expectation.LocalPosition)
                        .sqrMagnitude <= 0.00000001f &&
                    Mathf.Abs(Quaternion.Dot(
                        transform.localRotation,
                        expectation.LocalRotation)) >= 0.999999f &&
                    (transform.localScale - expectation.LocalScale)
                        .sqrMagnitude <= 0.00000001f;
                GameObject gameObject = transform.gameObject;
                if (!poseMatches ||
                    !gameObject.activeSelf ||
                    gameObject.layer != expectation.Layer ||
                    !string.Equals(
                        gameObject.tag,
                        "Untagged",
                        StringComparison.Ordinal))
                {
                    issues.Add(
                        expectation.HierarchyPath +
                        " no longer matches the exact approved pose/state");
                }
            }

            CauldronLiquidDisplay[] liquidDisplays = targetScene
                .GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<CauldronLiquidDisplay>(true))
                .ToArray();
            if (liquidDisplays.Length != 1)
            {
                issues.Add(
                    "expected one CauldronLiquidDisplay, found " +
                    liquidDisplays.Length);
            }
            else
            {
                SerializedObject serialized =
                    new SerializedObject(liquidDisplays[0]);
                SerializedProperty bottom =
                    serialized.FindProperty("_bottomLocalY");
                if (bottom == null ||
                    Mathf.Abs(bottom.floatValue - 0.6f) > 0.000001f)
                {
                    issues.Add(
                        "CauldronLiquidDisplay._bottomLocalY must remain 0.6 " +
                        "for the approved Alex-cauldron placement");
                }
            }

            return issues;
        }

        private static bool IsAuthorizedVisualLayoutPath(string path)
        {
            return AuthorizedVisualLayout.Any(expectation => string.Equals(
                expectation.HierarchyPath,
                path,
                StringComparison.Ordinal));
        }

        private static ProtectedSceneSnapshot CaptureProtectedSceneSnapshot(Scene scene)
        {
            ProtectedSceneSnapshot snapshot = new ProtectedSceneSnapshot();
            Transform[] transforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();

            foreach (Transform transform in transforms)
            {
                if (IsUnderGeneratedVisualRoot(transform) ||
                    IsUnderAuthorizedGameplayExtension(transform))
                {
                    continue;
                }

                Component[] components = transform.gameObject.GetComponents<Component>();
                Dictionary<Type, int> typeOrdinals = new Dictionary<Type, int>();
                bool protectedObject = false;

                foreach (Component component in components)
                {
                    if (component == null || component is Transform)
                    {
                        continue;
                    }

                    Type type = component.GetType();
                    int ordinal;
                    typeOrdinals.TryGetValue(type, out ordinal);
                    typeOrdinals[type] = ordinal + 1;

                    bool isCollider = component is Collider || component is Collider2D;
                    bool isRigidbody = component is Rigidbody || component is Rigidbody2D;
                    bool isXr = IsXrComponent(type);
                    bool isGameplay = IsKnownGameplayComponent(type);
                    if (!isCollider && !isRigidbody && !isXr && !isGameplay)
                    {
                        continue;
                    }

                    protectedObject = true;
                    if (isCollider)
                    {
                        snapshot.ColliderCount++;
                    }
                    if (isRigidbody)
                    {
                        snapshot.RigidbodyCount++;
                    }
                    if (isXr)
                    {
                        snapshot.XrCount++;
                    }
                    if (isGameplay)
                    {
                        snapshot.GameplayCount++;
                    }

                    string componentKey = GetHierarchyPath(transform) + " :: " +
                        (type.FullName ?? type.Name) + "[" + ordinal + "]";
                    snapshot.ComponentStates[componentKey] =
                        CaptureSerializedComponentState(component);
                }

                if (protectedObject)
                {
                    snapshot.ObjectStates[GetHierarchyPath(transform)] =
                        CaptureProtectedObjectState(transform);
                }
            }

            return snapshot;
        }

        private static string CaptureProtectedObjectState(Transform transform)
        {
            GameObject gameObject = transform.gameObject;
            return "active=" + gameObject.activeSelf +
                   ";layer=" + gameObject.layer +
                   ";tag=" + gameObject.tag +
                   ";parent=" +
                   (transform.parent == null
                       ? "<root>"
                       : GetHierarchyPath(transform.parent)) +
                   ";localPosition=" + FormatVector3(transform.localPosition) +
                   ";localRotation=" + FormatQuaternion(transform.localRotation) +
                   ";localScale=" + FormatVector3(transform.localScale);
        }

        private static SortedDictionary<string, string> CaptureSerializedComponentState(
            Component component)
        {
            SortedDictionary<string, string> fields =
                new SortedDictionary<string, string>(StringComparer.Ordinal);
            SerializedObject serializedObject = new SerializedObject(component);
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren =
                    property.propertyType == SerializedPropertyType.Generic ||
                    property.isArray;

                if (IsUnitySerializationPlumbing(property.propertyPath))
                {
                    continue;
                }

                if (IsAuthorizedGameplayExtensionField(
                    component,
                    property.propertyPath))
                {
                    continue;
                }

                if (property.propertyType == SerializedPropertyType.Generic)
                {
                    continue;
                }

                fields[property.propertyPath] = GetStableSerializedValue(property);
            }

            return fields;
        }

        private static bool IsUnderAuthorizedGameplayExtension(
            Transform transform)
        {
            return IsUnderSceneRootNamed(
                transform,
                GameplayExtensionRootName);
        }

        private static bool IsUnderGeneratedVisualRoot(
            Transform transform)
        {
            return IsUnderSceneRootNamed(
                transform,
                GeneratedRootName);
        }

        private static bool IsUnderSceneRootNamed(
            Transform transform,
            string rootName)
        {
            if (transform == null)
            {
                return false;
            }

            Transform sceneRoot = transform.root;
            return sceneRoot != null &&
                   sceneRoot.parent == null &&
                   sceneRoot.gameObject.scene == transform.gameObject.scene &&
                   string.Equals(
                       sceneRoot.name,
                       rootName,
                       StringComparison.Ordinal);
        }

        private static bool IsAuthorizedGameplayExtensionField(
            Component component,
            string propertyPath)
        {
            if (component is GameSessionController)
            {
                return string.Equals(
                    propertyPath,
                    "_puzzleDefinition",
                    StringComparison.Ordinal);
            }

            if (component is CauldronLiquidDisplay)
            {
                return string.Equals(
                    propertyPath,
                    "_bottomLocalY",
                    StringComparison.Ordinal);
            }

            if (!(component is RoomResetCoordinator))
            {
                return false;
            }

            return propertyPath.StartsWith(
                       "_potions.Array.",
                       StringComparison.Ordinal) ||
                   propertyPath.StartsWith(
                       "_physicsObjects.Array.",
                       StringComparison.Ordinal);
        }

        private static bool IsUnitySerializationPlumbing(string propertyPath)
        {
            return string.Equals(propertyPath, "m_GameObject", StringComparison.Ordinal) ||
                   string.Equals(propertyPath, "m_PrefabInstance", StringComparison.Ordinal) ||
                   string.Equals(propertyPath, "m_PrefabAsset", StringComparison.Ordinal) ||
                   string.Equals(
                       propertyPath,
                       "m_CorrespondingSourceObject",
                       StringComparison.Ordinal);
        }

        private static string GetStableSerializedValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Enum:
                    return property.longValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.RenderingLayerMask:
                    return property.ulongValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return property.boolValue ? "true" : "false";
                case SerializedPropertyType.Float:
                    return property.doubleValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.String:
                    return property.stringValue ?? string.Empty;
                case SerializedPropertyType.Color:
                    return FormatColor(property.colorValue);
                case SerializedPropertyType.ObjectReference:
                    return GetStableObjectReference(property.objectReferenceValue);
                case SerializedPropertyType.ExposedReference:
                    return GetStableObjectReference(property.exposedReferenceValue);
                case SerializedPropertyType.Vector2:
                    return FormatVector2(property.vector2Value);
                case SerializedPropertyType.Vector3:
                    return FormatVector3(property.vector3Value);
                case SerializedPropertyType.Vector4:
                    return FormatVector4(property.vector4Value);
                case SerializedPropertyType.Quaternion:
                    return FormatQuaternion(property.quaternionValue);
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue.ToString();
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue.ToString();
                case SerializedPropertyType.Rect:
                    return FormatRect(property.rectValue);
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue.ToString();
                case SerializedPropertyType.Bounds:
                    return FormatBounds(property.boundsValue);
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue.ToString();
                case SerializedPropertyType.AnimationCurve:
                    return FormatAnimationCurve(property.animationCurveValue);
                case SerializedPropertyType.Gradient:
                    return FormatGradient(property.gradientValue);
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceFullTypename ?? "<null>";
                case SerializedPropertyType.Hash128:
                    return property.hash128Value.ToString();
                default:
                    return property.propertyType.ToString();
            }
        }

        private static string GetStableObjectReference(UnityEngine.Object referencedObject)
        {
            if (referencedObject == null)
            {
                return "<null>";
            }

            if (EditorUtility.IsPersistent(referencedObject))
            {
                string assetPath = AssetDatabase.GetAssetPath(referencedObject);
                string guid;
                long localId;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        referencedObject,
                        out guid,
                        out localId))
                {
                    return "asset:" + guid + ":" + localId + ":" + assetPath;
                }

                return "asset:" + assetPath + ":" + referencedObject.name;
            }

            Component component = referencedObject as Component;
            if (component != null)
            {
                return "scene:" + GetHierarchyPath(component.transform) + "::" +
                       (component.GetType().FullName ?? component.GetType().Name) + "[" +
                       GetComponentTypeOrdinal(component) + "]";
            }

            GameObject gameObject = referencedObject as GameObject;
            if (gameObject != null)
            {
                return "scene:" + GetHierarchyPath(gameObject.transform);
            }

            return (referencedObject.GetType().FullName ?? referencedObject.GetType().Name) +
                   ":" + referencedObject.name;
        }

        private static int GetComponentTypeOrdinal(Component component)
        {
            Component[] sameType = component.gameObject.GetComponents(component.GetType());
            for (int index = 0; index < sameType.Length; index++)
            {
                if (sameType[index] == component)
                {
                    return index;
                }
            }

            return -1;
        }

        private static void CompareStringDictionaries(
            string label,
            IDictionary<string, string> source,
            IDictionary<string, string> target,
            ICollection<string> differences)
        {
            foreach (string key in source.Keys.Union(target.Keys).OrderBy(key => key))
            {
                if (string.Equals(
                        label,
                        "protected object",
                        StringComparison.Ordinal) &&
                    IsAuthorizedVisualLayoutPath(key))
                {
                    continue;
                }

                string sourceValue;
                string targetValue;
                bool sourceHas = source.TryGetValue(key, out sourceValue);
                bool targetHas = target.TryGetValue(key, out targetValue);

                if (!sourceHas)
                {
                    differences.Add("extra " + label + " in target: " + key);
                }
                else if (!targetHas)
                {
                    differences.Add("missing " + label + " in target: " + key);
                }
                else if (!string.Equals(sourceValue, targetValue, StringComparison.Ordinal))
                {
                    differences.Add(
                        label + " changed: " + key +
                        " (source=" + sourceValue +
                        ", target=" + targetValue + ")");
                }
            }
        }

        private static void CompareComponentDictionaries(
            IDictionary<string, SortedDictionary<string, string>> source,
            IDictionary<string, SortedDictionary<string, string>> target,
            ICollection<string> differences)
        {
            foreach (string key in source.Keys.Union(target.Keys).OrderBy(key => key))
            {
                SortedDictionary<string, string> sourceFields;
                SortedDictionary<string, string> targetFields;
                bool sourceHas = source.TryGetValue(key, out sourceFields);
                bool targetHas = target.TryGetValue(key, out targetFields);

                if (!sourceHas)
                {
                    differences.Add("extra protected component in target: " + key);
                    continue;
                }
                if (!targetHas)
                {
                    differences.Add("missing protected component in target: " + key);
                    continue;
                }

                foreach (string field in sourceFields.Keys.Union(targetFields.Keys))
                {
                    string sourceValue;
                    string targetValue;
                    if (!sourceFields.TryGetValue(field, out sourceValue))
                    {
                        differences.Add(key + " gained serialized field " + field);
                    }
                    else if (!targetFields.TryGetValue(field, out targetValue))
                    {
                        differences.Add(key + " lost serialized field " + field);
                    }
                    else if (!string.Equals(sourceValue, targetValue, StringComparison.Ordinal))
                    {
                        differences.Add(
                            key + " changed " + field + " (source=" + sourceValue +
                            ", target=" + targetValue + ")");
                    }
                }
            }
        }

        private static void ValidateExplicitInteractionIntegrity(
            GameObject[] targetRoots,
            ValidationReport report)
        {
            WandTip[] wandTips = targetRoots
                .SelectMany(root => root.GetComponentsInChildren<WandTip>(true))
                .ToArray();
            bool wandTipValid = wandTips.Length == 1 &&
                                wandTips[0].GetComponent<Collider>() != null &&
                                wandTips[0].GetComponent<Collider>().isTrigger;
            if (wandTipValid)
            {
                report.Ok("WandTip remains unique and retains its trigger Collider.");
            }
            else
            {
                report.Error(
                    "WandTip integrity failed: expected one WandTip with a trigger Collider.");
            }

            WandActivator[] activators = targetRoots
                .SelectMany(root => root.GetComponentsInChildren<WandActivator>(true))
                .ToArray();
            Collider finishTrigger = null;
            if (activators.Length == 1)
            {
                SerializedObject serializedActivator = new SerializedObject(activators[0]);
                SerializedProperty finishProperty =
                    serializedActivator.FindProperty("_finishTrigger");
                finishTrigger = finishProperty != null
                    ? finishProperty.objectReferenceValue as Collider
                    : null;
            }

            if (activators.Length == 1 && finishTrigger != null && finishTrigger.isTrigger)
            {
                report.Ok(
                    "WandActivator remains unique and references an enabled trigger target at " +
                    GetHierarchyPath(finishTrigger.transform) + ".");
            }
            else
            {
                report.Error(
                    "WandActivator/FinishTarget integrity failed: expected one activator " +
                    "with a non-null trigger Collider reference.");
            }

            RoomResetCoordinator[] resetCoordinators = targetRoots
                .SelectMany(root =>
                    root.GetComponentsInChildren<RoomResetCoordinator>(true))
                .ToArray();
            bool resetValid = resetCoordinators.Length == 1 &&
                              ValidateRoomResetSerializedArrays(
                                  resetCoordinators[0],
                                  targetRoots);
            if (resetValid)
            {
                report.Ok(
                    "RoomResetCoordinator retains GameSession/CauldronIntake references, " +
                    "eight unique potion entries, and nine unique physics-reset entries.");
            }
            else
            {
                report.Error(
                    "RoomResetCoordinator serialized references/arrays are not intact.");
            }
        }

        private static bool ValidateRoomResetSerializedArrays(
            RoomResetCoordinator coordinator,
            GameObject[] targetRoots)
        {
            SerializedObject serializedObject = new SerializedObject(coordinator);
            SerializedProperty gameSession = serializedObject.FindProperty("_gameSession");
            SerializedProperty intake = serializedObject.FindProperty("_cauldronIntake");
            SerializedProperty potions = serializedObject.FindProperty("_potions");
            SerializedProperty physicsObjects =
                serializedObject.FindProperty("_physicsObjects");

            GameSessionController[] sceneSessions = targetRoots
                .SelectMany(root =>
                    root.GetComponentsInChildren<GameSessionController>(true))
                .ToArray();
            CauldronIntake[] sceneIntakes = targetRoots
                .SelectMany(root =>
                    root.GetComponentsInChildren<CauldronIntake>(true))
                .ToArray();
            PotionController[] scenePotions = targetRoots
                .SelectMany(root =>
                    root.GetComponentsInChildren<PotionController>(true))
                .ToArray();
            PhysicsResettable[] scenePhysicsObjects = targetRoots
                .SelectMany(root =>
                    root.GetComponentsInChildren<PhysicsResettable>(true))
                .ToArray();

            return sceneSessions.Length == 1 &&
                   sceneIntakes.Length == 1 &&
                   gameSession != null &&
                   gameSession.objectReferenceValue == sceneSessions[0] &&
                   intake != null &&
                   intake.objectReferenceValue == sceneIntakes[0] &&
                   ArrayReferencesExactlyMatch(
                       potions,
                       scenePotions.Cast<UnityEngine.Object>()) &&
                   ArrayReferencesExactlyMatch(
                       physicsObjects,
                       scenePhysicsObjects.Cast<UnityEngine.Object>());
        }

        private static bool ArrayReferencesExactlyMatch(
            SerializedProperty array,
            IEnumerable<UnityEngine.Object> expectedReferences)
        {
            UnityEngine.Object[] expected = expectedReferences.ToArray();
            if (array == null || !array.isArray ||
                array.arraySize != expected.Length)
            {
                return false;
            }

            HashSet<UnityEngine.Object> actual =
                new HashSet<UnityEngine.Object>();
            for (int index = 0; index < array.arraySize; index++)
            {
                UnityEngine.Object item = array
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue;
                if (item == null || !actual.Add(item))
                {
                    return false;
                }
            }

            return actual.SetEquals(expected);
        }

        private static void ValidateGameplayExtension(
            GameObject extensionRoot,
            GameObject[] sceneRoots,
            ValidationReport report)
        {
            List<string> issues = new List<string>();

            Component[] rootComponents = extensionRoot.GetComponents<Component>();
            if (rootComponents.Any(component =>
                component != null && !(component is Transform)))
            {
                issues.Add("extension scene root must contain only its Transform");
            }

            PotionController[] extensionPotions = extensionRoot
                .GetComponentsInChildren<PotionController>(true);
            PhysicsResettable[] extensionResettables = extensionRoot
                .GetComponentsInChildren<PhysicsResettable>(true);
            Rigidbody[] extensionRigidbodies = extensionRoot
                .GetComponentsInChildren<Rigidbody>(true);
            Component[] extensionComponents = extensionRoot
                .GetComponentsInChildren<Component>(true);
            Component[] extensionGrabInteractables = extensionComponents
                .Where(component =>
                    component != null &&
                    string.Equals(
                        component.GetType().Name,
                        "XRGrabInteractable",
                        StringComparison.Ordinal))
                .ToArray();

            if (extensionPotions.Length != ExpectedNewPotions.Length)
            {
                issues.Add(
                    "expected " + ExpectedNewPotions.Length +
                    " extension PotionController components, found " +
                    extensionPotions.Length);
            }
            if (extensionResettables.Length != ExpectedNewPotions.Length)
            {
                issues.Add(
                    "expected " + ExpectedNewPotions.Length +
                    " extension PhysicsResettable components, found " +
                    extensionResettables.Length);
            }
            if (extensionRigidbodies.Length != ExpectedNewPotions.Length)
            {
                issues.Add(
                    "expected " + ExpectedNewPotions.Length +
                    " extension Rigidbody components, found " +
                    extensionRigidbodies.Length);
            }
            if (extensionGrabInteractables.Length != ExpectedNewPotions.Length)
            {
                issues.Add(
                    "expected " + ExpectedNewPotions.Length +
                    " extension XRGrabInteractable components, found " +
                    extensionGrabInteractables.Length);
            }

            HashSet<string> extensionIds = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (PotionController potion in extensionPotions)
            {
                PotionDefinition definition = potion.Definition;
                if (definition == null)
                {
                    issues.Add(GetHierarchyPath(potion.transform) +
                               " has no PotionDefinition");
                    continue;
                }

                if (!extensionIds.Add(definition.StableId))
                {
                    issues.Add("duplicate extension potion ID " +
                               definition.StableId);
                }

                PotionExpectation expectation = ExpectedNewPotions
                    .FirstOrDefault(item => string.Equals(
                        item.StableId,
                        definition.StableId,
                        StringComparison.OrdinalIgnoreCase));
                if (expectation == null)
                {
                    issues.Add("unexpected extension potion ID " +
                               definition.StableId);
                }
                else
                {
                    if (definition.HealthValue != expectation.HealthValue ||
                        definition.FillValue != expectation.FillValue)
                    {
                        issues.Add(
                            definition.StableId + " values differ: expected health/fill " +
                            expectation.HealthValue + "/" + expectation.FillValue +
                            ", found " + definition.HealthValue + "/" +
                            definition.FillValue);
                    }

                    string assetPath = AssetDatabase.GetAssetPath(definition);
                    if (!string.Equals(
                        assetPath,
                        expectation.AssetPath,
                        StringComparison.Ordinal))
                    {
                        issues.Add(
                            definition.StableId + " uses unexpected definition asset " +
                            assetPath);
                    }
                }

                if (potion.GetComponent<Rigidbody>() == null ||
                    potion.GetComponent<PhysicsResettable>() == null)
                {
                    issues.Add(GetHierarchyPath(potion.transform) +
                               " lost its Rigidbody or PhysicsResettable");
                }

                bool hasGrab = potion.GetComponents<Component>()
                    .Any(component =>
                        component != null &&
                        string.Equals(
                            component.GetType().Name,
                            "XRGrabInteractable",
                            StringComparison.Ordinal));
                if (!hasGrab)
                {
                    issues.Add(GetHierarchyPath(potion.transform) +
                               " has no XRGrabInteractable");
                }

                bool hasSolidCollider = potion
                    .GetComponentsInChildren<Collider>(true)
                    .Any(collider => collider.enabled && !collider.isTrigger);
                if (!hasSolidCollider)
                {
                    issues.Add(GetHierarchyPath(potion.transform) +
                               " has no enabled solid bottle Collider");
                }
            }

            ValidateWorkbenchCollider(
                extensionRoot,
                "MainWorkbenchBodyCollider",
                new Vector3(0.05f, 0.47f, 0.7f),
                new Vector3(2.36f, 0.74f, 0.14f),
                issues);
            ValidateWorkbenchCollider(
                extensionRoot,
                "CentralWorktopSurfaceCollider",
                new Vector3(-0.38f, 0.8f, 1.01f),
                new Vector3(0.64f, 0.09f, 0.62f),
                issues);
            ValidateWorkbenchCollider(
                extensionRoot,
                "ExpandedPotionWorktopCollider",
                new Vector3(-0.36f, 0.8f, 0.64f),
                new Vector3(0.92f, 0.09f, 0.24f),
                issues);
            ValidateWorkbenchCollider(
                extensionRoot,
                "PotionExtensionBodyCollider",
                new Vector3(-1.49f, 0.55f, 1.02f),
                new Vector3(0.62f, 0.99f, 0.42f),
                issues);

            BoxCollider[] tableColliders = extensionRoot
                .GetComponentsInChildren<BoxCollider>(true);
            if (tableColliders.Length != 4)
            {
                issues.Add(
                    "expected exactly four table BoxCollider components, found " +
                    tableColliders.Length);
            }

            ValidateExpandedPuzzle(sceneRoots, issues);

            if (issues.Count == 0)
            {
                report.Ok(
                    "Gameplay extension is complete: five defined, grabbable, " +
                    "resettable potions; capacity-eight puzzle; and four " +
                    "non-trigger table collision volumes.");
            }
            else
            {
                report.Error(
                    "Gameplay-extension validation found " + issues.Count +
                    " issue(s): " + string.Join(" | ", issues.ToArray()));
            }
        }

        private static void ValidateWorkbenchCollider(
            GameObject extensionRoot,
            string objectName,
            Vector3 expectedCenter,
            Vector3 expectedSize,
            ICollection<string> issues)
        {
            Transform[] matches = extensionRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(transform => string.Equals(
                    transform.name,
                    objectName,
                    StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                issues.Add(
                    "expected exactly one " + objectName + ", found " +
                    matches.Length);
                return;
            }

            GameObject colliderObject = matches[0].gameObject;
            BoxCollider collider = colliderObject.GetComponent<BoxCollider>();
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                issues.Add(objectName +
                           " must have one enabled, non-trigger BoxCollider");
                return;
            }

            if (colliderObject.GetComponent<Rigidbody>() != null ||
                colliderObject.GetComponent<Renderer>() != null)
            {
                issues.Add(objectName +
                           " must be a renderer-free static collision object");
            }
            if (!colliderObject.isStatic)
            {
                issues.Add(objectName + " is not marked static");
            }

            Bounds bounds = collider.bounds;
            if ((bounds.center - expectedCenter).sqrMagnitude > 0.0004f ||
                (bounds.size - expectedSize).sqrMagnitude > 0.0004f)
            {
                issues.Add(
                    objectName + " bounds differ from the visible table geometry " +
                    "(center=" + FormatVector3(bounds.center) +
                    ", size=" + FormatVector3(bounds.size) + ")");
            }
        }

        private static void ValidateExpandedPuzzle(
            GameObject[] sceneRoots,
            ICollection<string> issues)
        {
            PuzzleDefinition expectedPuzzle = AssetDatabase.LoadAssetAtPath<
                PuzzleDefinition>(ExpandedPuzzlePath);
            if (expectedPuzzle == null)
            {
                issues.Add("expanded puzzle asset is missing at " +
                           ExpandedPuzzlePath);
                return;
            }

            GameSessionController[] sessions = sceneRoots
                .SelectMany(root =>
                    root.GetComponentsInChildren<GameSessionController>(true))
                .ToArray();
            if (sessions.Length != 1)
            {
                issues.Add("expected one GameSessionController while validating puzzle");
                return;
            }

            SerializedObject serializedSession = new SerializedObject(sessions[0]);
            SerializedProperty puzzleProperty =
                serializedSession.FindProperty("_puzzleDefinition");
            if (puzzleProperty == null ||
                puzzleProperty.objectReferenceValue != expectedPuzzle)
            {
                issues.Add(
                    "GameSessionController does not reference " +
                    ExpandedPuzzlePath);
            }

            if (!expectedPuzzle.TryValidate(out string puzzleError))
            {
                issues.Add("expanded puzzle is invalid: " + puzzleError);
                return;
            }
            if (expectedPuzzle.CauldronCapacity != 8 ||
                expectedPuzzle.Potions.Count != ExpectedPotionCount)
            {
                issues.Add(
                    "expanded puzzle must contain eight potions at capacity 8");
                return;
            }

            PotionController[] scenePotions = sceneRoots
                .SelectMany(root =>
                    root.GetComponentsInChildren<PotionController>(true))
                .ToArray();
            HashSet<PotionDefinition> sceneDefinitions = new HashSet<PotionDefinition>(
                scenePotions.Select(potion => potion.Definition));
            HashSet<PotionDefinition> puzzleDefinitions = new HashSet<PotionDefinition>(
                expectedPuzzle.Potions);
            if (sceneDefinitions.Contains(null) ||
                sceneDefinitions.Count != ExpectedPotionCount ||
                !sceneDefinitions.SetEquals(puzzleDefinitions))
            {
                issues.Add(
                    "expanded puzzle definitions do not exactly match the eight " +
                    "PotionController instances in the visual scene");
            }

            KnapsackItem[] items = expectedPuzzle.Potions
                .Select(potion => new KnapsackItem(
                    potion.StableId,
                    potion.HealthValue,
                    potion.FillValue))
                .ToArray();
            PuzzleSolution solution = PuzzleSolver.Solve(8, items);
            string[] expectedIds = { "blue", "orange", "magenta" };
            if (solution.MaximumHealth != 15 ||
                solution.UsedCapacity != 8 ||
                !solution.SelectedPotionIds.SequenceEqual(expectedIds))
            {
                issues.Add(
                    "expanded puzzle optimum must be blue + orange + magenta " +
                    "for 15 health at 8 fill");
            }
        }

        private static void ValidateGeneratedRoot(
            GameObject generatedRoot,
            ValidationReport report)
        {
            Component[] components = generatedRoot.GetComponentsInChildren<Component>(true);
            List<string> forbiddenComponents = new List<string>();

            foreach (Component component in components)
            {
                if (component == null || component is Transform)
                {
                    continue;
                }

                Type type = component.GetType();
                if (component is Collider ||
                    component is Collider2D ||
                    component is Rigidbody ||
                    component is Rigidbody2D ||
                    IsXrComponent(type) ||
                    IsKnownGameplayComponent(type))
                {
                    forbiddenComponents.Add(
                        GetHierarchyPath(component.transform) + " :: " + type.FullName);
                }
            }

            if (forbiddenComponents.Count == 0)
            {
                report.Ok(
                    "Generated visual root contains no Collider, Rigidbody, XR, " +
                    "or known gameplay component.");
            }
            else
            {
                report.Error(
                    "Generated visual root contains " + forbiddenComponents.Count +
                    " forbidden component(s): " +
                    string.Join(" | ", forbiddenComponents.ToArray()));
            }

            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            Transform[] portalObjects = transforms
                .Where(transform =>
                    NormalizeName(transform.name).Contains("portal"))
                .ToArray();

            if (portalObjects.Length == 0)
            {
                report.Error("No portal object was found below the generated visual root.");
            }
            else
            {
                report.Ok(
                    "Portal object found: " + GetHierarchyPath(portalObjects[0]) +
                    (portalObjects.Length > 1
                        ? " (" + portalObjects.Length + " portal-named objects total)."
                        : "."));
            }

            ValidateGeneratedRenderers(generatedRoot, report);
        }

        private static bool IsXrComponent(Type type)
        {
            string typeNamespace = type.Namespace ?? string.Empty;
            return typeNamespace.StartsWith("UnityEngine.XR", StringComparison.Ordinal) ||
                   typeNamespace.StartsWith("Unity.XR", StringComparison.Ordinal) ||
                   typeNamespace.StartsWith(
                       "UnityEngine.InputSystem.XR",
                       StringComparison.Ordinal) ||
                   type.Name.StartsWith("XR", StringComparison.Ordinal);
        }

        private static bool IsKnownGameplayComponent(Type type)
        {
            return !string.IsNullOrEmpty(type.FullName) &&
                   KnownGameplayComponentTypes.Contains(type.FullName);
        }

        private static void ValidateNamedComponentCount(
            IEnumerable<Component> components,
            string expectedTypeName,
            int expectedCount,
            ValidationReport report)
        {
            Component[] matches = components
                .Where(component =>
                    component != null &&
                    string.Equals(
                        component.GetType().Name,
                        expectedTypeName,
                        StringComparison.Ordinal))
                .ToArray();

            if (matches.Length == expectedCount)
            {
                report.Ok(
                    "Exactly " + expectedCount + " " + expectedTypeName +
                    " component found at " + GetHierarchyPath(matches[0].transform) + ".");
            }
            else
            {
                report.Error(
                    "Expected exactly " + expectedCount + " " + expectedTypeName +
                    " component, found " + matches.Length + ".");
            }
        }

        private static void ValidateGeneratedRenderers(
            GameObject generatedRoot,
            ValidationReport report)
        {
            Renderer[] renderers = generatedRoot.GetComponentsInChildren<Renderer>(true);
            ValidateRendererCollection(renderers, "Generated visual root", report);
        }

        private static void ValidateRendererCollection(
            Renderer[] renderers,
            string label,
            ValidationReport report)
        {
            if (renderers.Length == 0)
            {
                report.Error(label + " contains no Renderer components.");
                return;
            }

            List<string> issues = new List<string>();
            HashSet<Material> checkedMaterials = new HashSet<Material>();

            foreach (Renderer renderer in renderers)
            {
                string hierarchyPath = GetHierarchyPath(renderer.transform);

                MeshRenderer meshRenderer = renderer as MeshRenderer;
                if (meshRenderer != null)
                {
                    MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        issues.Add(hierarchyPath + " has a MeshRenderer without a mesh.");
                    }
                }

                SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;
                if (skinnedRenderer != null && skinnedRenderer.sharedMesh == null)
                {
                    issues.Add(hierarchyPath + " has a SkinnedMeshRenderer without a mesh.");
                }

                ParticleSystemRenderer particleRenderer =
                    renderer as ParticleSystemRenderer;
                if (particleRenderer != null &&
                    particleRenderer.renderMode == ParticleSystemRenderMode.Mesh &&
                    particleRenderer.mesh == null)
                {
                    issues.Add(
                        hierarchyPath + " uses particle Mesh render mode without a mesh.");
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    issues.Add(hierarchyPath + " has no material.");
                    continue;
                }

                for (int materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                    {
                        issues.Add(
                            hierarchyPath + " has an empty material slot " +
                            materialIndex + ".");
                        continue;
                    }

                    if (!checkedMaterials.Add(material))
                    {
                        continue;
                    }

                    Shader shader = material.shader;
                    if (shader == null)
                    {
                        issues.Add("Material " + material.name + " has no shader.");
                    }
                    else if (!IsUrpMaterial(material))
                    {
                        issues.Add(
                            "Material " + material.name + " uses non-URP/legacy shader " +
                            shader.name + ".");
                    }
                }
            }

            if (issues.Count == 0)
            {
                report.Ok(
                    label + ": " + renderers.Length +
                    " Renderer component(s) have valid " +
                    "meshes/materials and URP-compatible shaders.");
            }
            else
            {
                report.Error(
                    label + " renderer/material validation found " + issues.Count +
                    " issue(s): " + string.Join(" | ", issues.ToArray()));
            }
        }

        private static bool IsUrpMaterial(Material material)
        {
            Shader shader = material.shader;
            string shaderName = shader.name ?? string.Empty;
            if (shaderName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase) ||
                shaderName.StartsWith("Particles/", StringComparison.OrdinalIgnoreCase) ||
                shaderName.StartsWith("Mobile/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shaderName, "Standard", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    shaderName,
                    "Standard (Specular setup)",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string renderPipelineTag = material.GetTag(
                "RenderPipeline",
                false,
                string.Empty);

            return renderPipelineTag.IndexOf(
                       "Universal",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   shaderName.StartsWith(
                       "Universal Render Pipeline/",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateControllerCounts(
            GameObject[] sceneRoots,
            ValidationReport report)
        {
            ValidateComponentCount<PotionController>(
                sceneRoots,
                ExpectedPotionCount,
                report);
            ValidateComponentCount<PhysicsResettable>(
                sceneRoots,
                ExpectedPhysicsResettableCount,
                report);
            ValidateComponentCount<CauldronController>(sceneRoots, 1, report);
            ValidateComponentCount<CauldronIntake>(sceneRoots, 1, report);
            ValidateComponentCount<GameSessionController>(sceneRoots, 1, report);
            ValidateComponentCount<RoomResetCoordinator>(sceneRoots, 1, report);
            ValidateComponentCount<WandActivator>(sceneRoots, 1, report);
            ValidateComponentCount<WandTip>(sceneRoots, 1, report);
        }

        private static void ValidateComponentCount<T>(
            IEnumerable<GameObject> sceneRoots,
            int expectedCount,
            ValidationReport report)
            where T : Component
        {
            int actualCount = sceneRoots.Sum(
                root => root.GetComponentsInChildren<T>(true).Length);

            if (actualCount == expectedCount)
            {
                report.Ok(
                    typeof(T).Name + " count matches expected: " + actualCount + ".");
            }
            else
            {
                report.Error(
                    typeof(T).Name + " count changed. Expected " + expectedCount +
                    ", found " + actualCount + ".");
            }
        }

        private static Transform[] ValidateAlexWrapperNames(
            GameObject[] sceneRoots,
            ValidationReport report)
        {
            Transform[] allTransforms = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();
            Transform[] wrapperRoots = allTransforms
                .Where(transform => IsVisualWrapperMarker(transform.name))
                .Where(transform =>
                    transform.parent == null ||
                    !IsVisualWrapperMarker(transform.parent.name))
                .ToArray();

            int foundCategoryCount = 0;
            foreach (WrapperExpectation expectation in ExpectedAlexWrappers)
            {
                Transform[] matches = wrapperRoots
                    .Where(transform => expectation.Matches(GetHierarchyPath(transform)))
                    .ToArray();

                if (matches.Length == 0)
                {
                    report.Error(
                        "Alex visual wrapper/pivot name not found for " +
                        expectation.Label + ".");
                    continue;
                }

                foundCategoryCount++;
                report.Ok(
                    "Alex visual wrapper/pivot for " + expectation.Label +
                    " found (" + matches.Length + " scene instance(s)); first: " +
                    GetHierarchyPath(matches[0]) + ".");
            }

            if (foundCategoryCount == ExpectedAlexWrappers.Length)
            {
                report.Ok("All five stable Alex visual-wrapper categories are present.");
            }

            return wrapperRoots;
        }

        private static bool IsVisualWrapperMarker(string objectName)
        {
            string normalizedName = NormalizeName(objectName);
            return normalizedName.Contains("visualpivot") ||
                   normalizedName.Contains("visualwrapper") ||
                   normalizedName.Contains("alexvisual");
        }

        private static void ValidateAlexWrapperSubtrees(
            Transform[] wrapperRoots,
            GameObject generatedRoot,
            ValidationReport report)
        {
            if (wrapperRoots.Length == 0)
            {
                report.Error("No Alex visual-wrapper/pivot subtree could be inspected.");
                return;
            }

            List<string> forbidden = new List<string>();
            List<string> unattached = new List<string>();
            List<string> emptyRendererRoots = new List<string>();
            HashSet<Renderer> renderers = new HashSet<Renderer>();

            foreach (Transform wrapperRoot in wrapperRoots)
            {
                bool belowGeneratedRoot = generatedRoot != null &&
                                          wrapperRoot.IsChildOf(generatedRoot.transform);
                bool belowGameplayRoot = HasGameplayAncestor(wrapperRoot.parent);
                bool belowNeutralPlaceholderHost =
                    HasNamedAncestor(wrapperRoot.parent, "Placeholders");
                if (!belowGeneratedRoot &&
                    !belowGameplayRoot &&
                    !belowNeutralPlaceholderHost)
                {
                    unattached.Add(GetHierarchyPath(wrapperRoot));
                }

                Component[] components =
                    wrapperRoot.GetComponentsInChildren<Component>(true);
                foreach (Component component in components)
                {
                    if (component == null || component is Transform)
                    {
                        continue;
                    }

                    Type type = component.GetType();
                    if (component is Collider || component is Collider2D ||
                        component is Rigidbody || component is Rigidbody2D ||
                        IsXrComponent(type) || IsKnownGameplayComponent(type))
                    {
                        forbidden.Add(
                            GetHierarchyPath(component.transform) + " :: " +
                            (type.FullName ?? type.Name));
                    }
                }

                Renderer[] subtreeRenderers =
                    wrapperRoot.GetComponentsInChildren<Renderer>(true);
                if (subtreeRenderers.Length == 0)
                {
                    emptyRendererRoots.Add(GetHierarchyPath(wrapperRoot));
                }
                foreach (Renderer renderer in subtreeRenderers)
                {
                    renderers.Add(renderer);
                }
            }

            if (unattached.Count == 0)
            {
                report.Ok(
                    "Every Alex wrapper/pivot is attached below a gameplay root " +
                    "or an approved project-owned visual host.");
            }
            else
            {
                report.Error(
                    "Alex wrapper/pivot subtrees outside approved attachment points: " +
                    string.Join(" | ", unattached.ToArray()));
            }

            if (forbidden.Count == 0)
            {
                report.Ok(
                    "Alex wrapper/pivot subtrees contain no Collider, Rigidbody, XR, " +
                    "or known gameplay components.");
            }
            else
            {
                report.Error(
                    "Forbidden components inside Alex wrapper/pivot subtrees: " +
                    string.Join(" | ", forbidden.ToArray()));
            }

            if (emptyRendererRoots.Count > 0)
            {
                report.Error(
                    "Alex wrapper/pivot subtrees without a Renderer: " +
                    string.Join(" | ", emptyRendererRoots.ToArray()));
            }

            ValidateRendererCollection(
                renderers.ToArray(),
                "Alex wrapper/pivot subtrees",
                report);
        }

        private static bool HasGameplayAncestor(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                Component[] components = current.GetComponents<Component>();
                if (components.Any(component =>
                        component != null &&
                        IsKnownGameplayComponent(component.GetType())))
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        private static bool HasNamedAncestor(
            Transform transform,
            string objectName)
        {
            Transform current = transform;
            while (current != null)
            {
                if (string.Equals(
                    current.name,
                    objectName,
                    StringComparison.Ordinal))
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        private static void ValidateMissingScripts(
            GameObject[] sceneRoots,
            ValidationReport report)
        {
            List<string> missingScriptLocations = new List<string>();
            int gameObjectCount = 0;

            foreach (GameObject root in sceneRoots)
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                gameObjectCount += transforms.Length;

                foreach (Transform transform in transforms)
                {
                    Component[] components = transform.gameObject.GetComponents<Component>();
                    int missingCount = components.Count(component => component == null);
                    for (int index = 0; index < missingCount; index++)
                    {
                        missingScriptLocations.Add(GetHierarchyPath(transform));
                    }
                }
            }

            if (missingScriptLocations.Count == 0)
            {
                report.Ok(
                    "No missing script components in target scene (" +
                    gameObjectCount + " GameObjects inspected)." );
            }
            else
            {
                report.Error(
                    missingScriptLocations.Count +
                    " missing script component(s) in target scene: " +
                    string.Join(" | ", missingScriptLocations.ToArray()));
            }
        }

        private static void ValidateRealtimeLights(
            GameObject[] sceneRoots,
            ValidationReport report)
        {
            Light[] realtimeLights = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .Where(light =>
                    light != null &&
                    light.enabled &&
                    light.gameObject.activeInHierarchy &&
                    light.lightmapBakeType != LightmapBakeType.Baked)
                .ToArray();

            if (realtimeLights.Length <= MaximumRealtimeLights)
            {
                report.Ok(
                    "Realtime/mixed light count is within budget: " +
                    realtimeLights.Length + "/" + MaximumRealtimeLights + ".");
            }
            else
            {
                report.Error(
                    "Realtime/mixed light budget exceeded: " + realtimeLights.Length +
                    "/" + MaximumRealtimeLights + ".");
            }

            Light[] shadowedSmallLights = realtimeLights
                .Where(light =>
                    light.type != LightType.Directional &&
                    light.shadows != LightShadows.None)
                .ToArray();

            if (shadowedSmallLights.Length == 0)
            {
                report.Ok("All enabled non-directional realtime lights have shadows disabled.");
            }
            else
            {
                report.Error(
                    "Small realtime lights with shadows enabled: " +
                    string.Join(
                        " | ",
                        shadowedSmallLights
                            .Select(light => GetHierarchyPath(light.transform))
                            .ToArray()));
            }
        }

        private static void ValidateQuestRenderFallback(
            GameObject[] sceneRoots,
            ValidationReport report)
        {
            UnityEngine.Object pipelineAsset =
                AssetDatabase.LoadMainAssetAtPath(PerformanceUrpAssetPath);
            if (pipelineAsset == null)
            {
                report.Error(
                    "Quest Performance URP asset is missing: " +
                    PerformanceUrpAssetPath + ".");
                return;
            }

            SerializedObject pipeline = new SerializedObject(pipelineAsset);
            SerializedProperty additionalLights =
                pipeline.FindProperty("m_AdditionalLightsRenderingMode");
            SerializedProperty supportsHdr =
                pipeline.FindProperty("m_SupportsHDR");
            if (additionalLights == null || supportsHdr == null)
            {
                report.Error(
                    "Could not inspect Quest Performance URP light/HDR settings.");
                return;
            }

            Light directional = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .FirstOrDefault(light =>
                    light.enabled &&
                    light.gameObject.activeInHierarchy &&
                    light.type == LightType.Directional);

            if (additionalLights.intValue == 0)
            {
                bool warmReadableFallback =
                    directional != null &&
                    directional.intensity >= 0.5f &&
                    directional.color.r > directional.color.b * 1.2f;
                if (warmReadableFallback)
                {
                    report.Ok(
                        "Quest Performance disables additional lights; the warm " +
                        "directional/ambient base remains independently readable.");
                }
                else
                {
                    report.Error(
                        "Quest Performance disables additional lights, but no " +
                        "sufficient warm directional fallback is configured.");
                }
            }
            else
            {
                report.Ok(
                    "Quest Performance renders additional lights in mode " +
                    additionalLights.intValue + ".");
            }

            Volume[] volumes = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<Volume>(true))
                .Where(volume =>
                    volume.enabled &&
                    volume.gameObject.activeInHierarchy &&
                    volume.sharedProfile != null)
                .ToArray();
            Bloom bloom = null;
            foreach (Volume volume in volumes)
            {
                if (volume.sharedProfile.TryGet(out bloom) && bloom != null)
                {
                    break;
                }
            }

            if (!supportsHdr.boolValue)
            {
                if (bloom != null &&
                    bloom.active &&
                    bloom.threshold.overrideState &&
                    bloom.threshold.value <= 1f)
                {
                    report.Ok(
                        "Quest Performance is LDR; Bloom uses an LDR-safe " +
                        "threshold of " +
                        bloom.threshold.value.ToString(
                            "0.###",
                            CultureInfo.InvariantCulture) + ".");
                }
                else
                {
                    report.Error(
                        "Quest Performance is LDR, but Bloom is missing, inactive, " +
                        "or configured above the usable LDR threshold.");
                }
            }
            else
            {
                report.Ok("Quest Performance HDR support is enabled.");
            }
        }

        private static void ValidateParticles(
            GameObject[] sceneRoots,
            ValidationReport report)
        {
            ParticleSystem[] particleSystems = sceneRoots
                .SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true))
                .ToArray();

            int totalMaximumParticles = 0;
            List<string> perSystemWarnings = new List<string>();
            List<string> colliderIssues = new List<string>();

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                int maximumParticles = particleSystem.main.maxParticles;
                totalMaximumParticles += maximumParticles;

                if (maximumParticles > MaximumParticlesPerSystem)
                {
                    perSystemWarnings.Add(
                        GetHierarchyPath(particleSystem.transform) + "=" +
                        maximumParticles);
                }

                if (particleSystem.GetComponent<Collider>() != null)
                {
                    colliderIssues.Add(GetHierarchyPath(particleSystem.transform));
                }

                if (particleSystem.collision.enabled)
                {
                    report.Warn(
                        "Particle collision module is enabled at " +
                        GetHierarchyPath(particleSystem.transform) + ".");
                }
            }

            if (perSystemWarnings.Count == 0)
            {
                report.Ok(
                    "Every particle system stays at or below " +
                    MaximumParticlesPerSystem + " maxParticles.");
            }
            else
            {
                report.Warn(
                    "Particle systems above the conservative per-system budget (" +
                    MaximumParticlesPerSystem + "): " +
                    string.Join(" | ", perSystemWarnings.ToArray()));
            }

            if (totalMaximumParticles <= MaximumTotalParticles)
            {
                report.Ok(
                    "Total configured maxParticles is within budget: " +
                    totalMaximumParticles + "/" + MaximumTotalParticles + ".");
            }
            else
            {
                report.Warn(
                    "Total configured maxParticles exceeds conservative budget: " +
                    totalMaximumParticles + "/" + MaximumTotalParticles + ".");
            }

            if (colliderIssues.Count == 0)
            {
                report.Ok("No ParticleSystem GameObject has a Collider component.");
            }
            else
            {
                report.Error(
                    "ParticleSystem GameObjects with Collider components: " +
                    string.Join(" | ", colliderIssues.ToArray()));
            }
        }

        private static void ValidateBuildSettings(ValidationReport report)
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();

            bool exactlyTargetEnabled =
                enabledScenes.Length == 1 &&
                PathsEqual(enabledScenes[0].path, TargetScenePath);
            bool sampleEnabled = enabledScenes.Any(
                scene => PathsEqual(scene.path, SampleScenePath));

            if (exactlyTargetEnabled && !sampleEnabled)
            {
                report.Ok(
                    "Build Settings contain exactly the enabled visual target scene; " +
                    "SampleScene is not enabled.");
            }
            else
            {
                string configured = enabledScenes.Length == 0
                    ? "<none>"
                    : string.Join(
                        ", ",
                        enabledScenes.Select(scene => scene.path).ToArray());

                report.Error(
                    "Build Settings must enable exactly " + TargetScenePath +
                    " and must not enable SampleScene. Enabled: " + configured + ".");
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                (left ?? string.Empty).Replace('\\', '/'),
                (right ?? string.Empty).Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetProjectAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<missing transform>";
            }

            StringBuilder path = new StringBuilder(transform.name);
            Transform parent = transform.parent;
            while (parent != null)
            {
                path.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return path.ToString();
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder normalized = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    normalized.Append(char.ToLowerInvariant(character));
                }
            }

            return normalized.ToString();
        }

        private static string FormatVector2(Vector2 value)
        {
            return "(" + FormatFloat(value.x) + "," +
                   FormatFloat(value.y) + ")";
        }

        private static string FormatVector3(Vector3 value)
        {
            return "(" + FormatFloat(value.x) + "," +
                   FormatFloat(value.y) + "," +
                   FormatFloat(value.z) + ")";
        }

        private static string FormatVector4(Vector4 value)
        {
            return "(" + FormatFloat(value.x) + "," +
                   FormatFloat(value.y) + "," +
                   FormatFloat(value.z) + "," +
                   FormatFloat(value.w) + ")";
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return "(" + FormatFloat(value.x) + "," +
                   FormatFloat(value.y) + "," +
                   FormatFloat(value.z) + "," +
                   FormatFloat(value.w) + ")";
        }

        private static string FormatColor(Color value)
        {
            return "(" + FormatFloat(value.r) + "," +
                   FormatFloat(value.g) + "," +
                   FormatFloat(value.b) + "," +
                   FormatFloat(value.a) + ")";
        }

        private static string FormatRect(Rect value)
        {
            return "position=" + FormatVector2(value.position) +
                   ";size=" + FormatVector2(value.size);
        }

        private static string FormatBounds(Bounds value)
        {
            return "center=" + FormatVector3(value.center) +
                   ";size=" + FormatVector3(value.size);
        }

        private static string FormatAnimationCurve(AnimationCurve curve)
        {
            if (curve == null)
            {
                return "<null>";
            }

            IEnumerable<string> keys = curve.keys.Select(key =>
                FormatFloat(key.time) + "," +
                FormatFloat(key.value) + "," +
                FormatFloat(key.inTangent) + "," +
                FormatFloat(key.outTangent) + "," +
                FormatFloat(key.inWeight) + "," +
                FormatFloat(key.outWeight) + "," +
                ((int)key.weightedMode).ToString(
                    CultureInfo.InvariantCulture));
            return "pre=" + (int)curve.preWrapMode +
                   ";post=" + (int)curve.postWrapMode +
                   ";keys=[" + string.Join("|", keys.ToArray()) + "]";
        }

        private static string FormatGradient(Gradient gradient)
        {
            if (gradient == null)
            {
                return "<null>";
            }

            string colors = string.Join(
                "|",
                gradient.colorKeys.Select(key =>
                    FormatFloat(key.time) + ":" +
                    FormatColor(key.color)).ToArray());
            string alphas = string.Join(
                "|",
                gradient.alphaKeys.Select(key =>
                    FormatFloat(key.time) + ":" +
                    FormatFloat(key.alpha)).ToArray());
            return "mode=" + (int)gradient.mode +
                   ";colors=[" + colors + "]" +
                   ";alphas=[" + alphas + "]";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private sealed class PotionExpectation
        {
            public PotionExpectation(
                string stableId,
                int healthValue,
                int fillValue)
            {
                StableId = stableId;
                HealthValue = healthValue;
                FillValue = fillValue;
                AssetPath =
                    "Assets/WizzardsCauldron/Data/Potions/SO_Potion" +
                    char.ToUpperInvariant(stableId[0]) +
                    stableId.Substring(1) + ".asset";
            }

            public string StableId { get; private set; }
            public int HealthValue { get; private set; }
            public int FillValue { get; private set; }
            public string AssetPath { get; private set; }
        }

        private sealed class AuthorizedLayoutExpectation
        {
            public AuthorizedLayoutExpectation(
                string hierarchyPath,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                int layer)
            {
                HierarchyPath = hierarchyPath;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                Layer = layer;
            }

            public string HierarchyPath { get; private set; }
            public Vector3 LocalPosition { get; private set; }
            public Quaternion LocalRotation { get; private set; }
            public Vector3 LocalScale { get; private set; }
            public int Layer { get; private set; }
        }

        private sealed class ProtectedSceneSnapshot
        {
            public readonly SortedDictionary<string, string> ObjectStates =
                new SortedDictionary<string, string>(StringComparer.Ordinal);
            public readonly SortedDictionary<
                string,
                SortedDictionary<string, string>> ComponentStates =
                new SortedDictionary<
                    string,
                    SortedDictionary<string, string>>(StringComparer.Ordinal);

            public int ColliderCount;
            public int RigidbodyCount;
            public int XrCount;
            public int GameplayCount;
        }

        private sealed class WrapperExpectation
        {
            private readonly string[] categoryTokens;

            public WrapperExpectation(string label, params string[] categoryTokens)
            {
                Label = label;
                this.categoryTokens = categoryTokens;
            }

            public string Label { get; private set; }

            public bool Matches(string hierarchyPath)
            {
                string normalizedPath = NormalizeName(hierarchyPath);
                bool hasVisualMarker =
                    normalizedPath.Contains("visualpivot") ||
                    normalizedPath.Contains("visualwrapper") ||
                    normalizedPath.Contains("alexvisual");

                return hasVisualMarker &&
                       categoryTokens.Any(token =>
                           normalizedPath.Contains(NormalizeName(token)));
            }
        }

        private sealed class ValidationReport
        {
            public int OkCount { get; private set; }
            public int WarningCount { get; private set; }
            public int ErrorCount { get; private set; }

            public void Ok(string message)
            {
                OkCount++;
                Debug.Log(LogPrefix + " OK " + message);
            }

            public void Warn(string message)
            {
                WarningCount++;
                Debug.LogWarning(LogPrefix + " WARN " + message);
            }

            public void Error(string message)
            {
                ErrorCount++;
                Debug.LogError(LogPrefix + " ERROR " + message);
            }

            public void LogSummary()
            {
                string summary = BuildSummaryLine();
                if (ErrorCount > 0)
                {
                    Debug.LogError(summary);
                }
                else if (WarningCount > 0)
                {
                    Debug.LogWarning(summary);
                }
                else
                {
                    Debug.Log(summary);
                }
            }

            public string BuildDialogSummary()
            {
                string status = ErrorCount == 0
                    ? "Visual-pass validation completed."
                    : "Visual-pass validation found blocking errors.";

                return status + "\n\n" + BuildSummaryLine() +
                       "\n\nDetails are in the Unity Console.";
            }

            private string BuildSummaryLine()
            {
                return LogPrefix + " SUMMARY " + OkCount + " OK, " +
                       WarningCount + " WARN, " + ErrorCount + " ERROR. " +
                       "No scene, asset, or Build Setting was saved or changed.";
            }
        }
    }
}
