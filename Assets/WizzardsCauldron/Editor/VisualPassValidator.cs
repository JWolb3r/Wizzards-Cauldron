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
        private const string PerformanceUrpAssetPath =
            "Assets/Settings/Project Configuration/Performance URP Config.asset";
        private const string ExpectedSourceSha256 =
            "2A13B8401B8C493386575CBBB09E09BA43F1BCFBEEE3C7420D7EB106A638B0BC";

        private const int MaximumRealtimeLights = 4;
        private const int MaximumParticlesPerSystem = 48;
        private const int MaximumTotalParticles = 128;

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

            ValidateExplicitInteractionIntegrity(targetScene.GetRootGameObjects(), report);
        }

        private static ProtectedSceneSnapshot CaptureProtectedSceneSnapshot(Scene scene)
        {
            ProtectedSceneSnapshot snapshot = new ProtectedSceneSnapshot();
            Transform[] transforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();

            foreach (Transform transform in transforms)
            {
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
                   ";localScale=" + FormatVector3(transform.localScale) +
                   ";worldPosition=" + FormatVector3(transform.position) +
                   ";worldRotation=" + FormatQuaternion(transform.rotation) +
                   ";worldScale=" + FormatVector3(transform.lossyScale);
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

                if (property.propertyType == SerializedPropertyType.Generic)
                {
                    continue;
                }

                fields[property.propertyPath] = GetStableSerializedValue(property);
            }

            return fields;
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
                    differences.Add(label + " changed: " + key);
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
                              ValidateRoomResetSerializedArrays(resetCoordinators[0]);
            if (resetValid)
            {
                report.Ok(
                    "RoomResetCoordinator retains GameSession/CauldronIntake references, " +
                    "three potion entries, and four physics-reset entries.");
            }
            else
            {
                report.Error(
                    "RoomResetCoordinator serialized references/arrays are not intact.");
            }
        }

        private static bool ValidateRoomResetSerializedArrays(
            RoomResetCoordinator coordinator)
        {
            SerializedObject serializedObject = new SerializedObject(coordinator);
            SerializedProperty gameSession = serializedObject.FindProperty("_gameSession");
            SerializedProperty intake = serializedObject.FindProperty("_cauldronIntake");
            SerializedProperty potions = serializedObject.FindProperty("_potions");
            SerializedProperty physicsObjects =
                serializedObject.FindProperty("_physicsObjects");

            return gameSession != null && gameSession.objectReferenceValue != null &&
                   intake != null && intake.objectReferenceValue != null &&
                   ArrayHasNonNullReferences(potions, 3) &&
                   ArrayHasNonNullReferences(physicsObjects, 4);
        }

        private static bool ArrayHasNonNullReferences(
            SerializedProperty array,
            int expectedSize)
        {
            if (array == null || !array.isArray || array.arraySize != expectedSize)
            {
                return false;
            }

            for (int index = 0; index < array.arraySize; index++)
            {
                if (array.GetArrayElementAtIndex(index).objectReferenceValue == null)
                {
                    return false;
                }
            }

            return true;
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
            ValidateComponentCount<PotionController>(sceneRoots, 3, report);
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
                    typeof(T).Name + " count preserved: " + actualCount + ".");
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
