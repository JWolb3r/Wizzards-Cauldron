using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.UI;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Project-owned data and scene references for the optional expanded
    /// visual-scene puzzle. The protected source scene and shared potion
    /// prefab remain untouched.
    /// </summary>
    internal sealed class VisualPassGameplayExtensionData
    {
        private readonly IReadOnlyList<PotionDefinition>
            _potionDefinitions;
        private readonly IReadOnlyList<PotionDefinition>
            _newPotionDefinitions;

        internal VisualPassGameplayExtensionData(
            PuzzleDefinition expandedPuzzle,
            GameObject potionBottlePrefab,
            PotionDefinition[] potionDefinitions,
            PotionDefinition[] newPotionDefinitions)
        {
            ExpandedPuzzle = expandedPuzzle;
            PotionBottlePrefab = potionBottlePrefab;
            _potionDefinitions = Array.AsReadOnly(
                (PotionDefinition[])potionDefinitions.Clone());
            _newPotionDefinitions = Array.AsReadOnly(
                (PotionDefinition[])newPotionDefinitions.Clone());
            ScenePotions = Array.AsReadOnly(
                Array.Empty<PotionController>());
            PotionPhysicsObjects = Array.AsReadOnly(
                Array.Empty<PhysicsResettable>());
            WorkbenchColliders = Array.AsReadOnly(
                Array.Empty<BoxCollider>());
        }

        internal PuzzleDefinition ExpandedPuzzle { get; }
        internal GameObject PotionBottlePrefab { get; }

        internal IReadOnlyList<PotionDefinition>
            PotionDefinitions => _potionDefinitions;

        internal IReadOnlyList<PotionDefinition>
            NewPotionDefinitions => _newPotionDefinitions;

        internal GameObject GameplayRoot { get; private set; }

        internal IReadOnlyList<PotionController>
            ScenePotions { get; private set; }

        internal IReadOnlyList<PhysicsResettable>
            PotionPhysicsObjects { get; private set; }

        internal PhysicsResettable WandPhysicsObject { get; private set; }

        internal IReadOnlyList<BoxCollider>
            WorkbenchColliders { get; private set; }

        internal PotionDefinition FindDefinition(string stableId)
        {
            for (int index = 0;
                 index < _potionDefinitions.Count;
                 index++)
            {
                PotionDefinition definition =
                    _potionDefinitions[index];

                if (string.Equals(
                    definition.StableId,
                    stableId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return definition;
                }
            }

            throw new InvalidOperationException(
                "The expanded visual puzzle has no potion with stable ID '" +
                stableId + "'.");
        }

        internal void SetComposedSceneState(
            GameObject gameplayRoot,
            PotionController[] scenePotions,
            PhysicsResettable[] potionPhysicsObjects,
            PhysicsResettable wandPhysicsObject,
            BoxCollider[] workbenchColliders)
        {
            GameplayRoot = gameplayRoot;
            ScenePotions = Array.AsReadOnly(
                (PotionController[])scenePotions.Clone());
            PotionPhysicsObjects = Array.AsReadOnly(
                (PhysicsResettable[])potionPhysicsObjects.Clone());
            WandPhysicsObject = wandPhysicsObject;
            WorkbenchColliders = Array.AsReadOnly(
                (BoxCollider[])workbenchColliders.Clone());
        }
    }

    /// <summary>
    /// Creates the target-scene-only expanded potion puzzle. Rebuilding first
    /// removes the previous generated extension root and recreates it from the
    /// unchanged shared potion prefab.
    /// </summary>
    internal static class VisualPassGameplayExtension
    {
        internal const string GameplayRootName =
            "__WC_GAMEPLAY_EXTENSION__";

        internal const string ExpandedPuzzlePath =
            "Assets/WizzardsCauldron/Data/Puzzles/" +
            "SO_PuzzleVisualExpanded.asset";

        internal const string MainWorkbenchBodyColliderName =
            "MainWorkbenchBodyCollider";
        internal const string CentralWorktopSurfaceColliderName =
            "CentralWorktopSurfaceCollider";
        internal const string ExpandedPotionWorktopColliderName =
            "ExpandedPotionWorktopCollider";
        internal const string PotionExtensionBodyColliderName =
            "PotionExtensionBodyCollider";
        internal const string RightWorktopSurfaceColliderName =
            "RightWorktopSurfaceCollider";
        internal const string WorkbenchBridgeSurfaceColliderName =
            "WorkbenchBridgeSurfaceCollider";
        internal const string FrontRoomBoundaryColliderName =
            "FrontRoomBoundaryCollider";

        private const string PotionDataFolder =
            "Assets/WizzardsCauldron/Data/Potions";
        private const string PuzzleDataFolder =
            "Assets/WizzardsCauldron/Data/Puzzles";
        private const string PotionBottlePrefabPath =
            "Assets/WizzardsCauldron/Prefabs/PF_PotionBottle.prefab";

        private static readonly PotionBuildSpec[] NewPotionSpecs =
        {
            new PotionBuildSpec(
                "blue",
                "Blue Potion",
                2,
                1,
                new Color(0.08f, 0.35f, 1f, 1f),
                new Vector3(-1.68f, 1.185f, 1.02f)),
            new PotionBuildSpec(
                "violet",
                "Violet Potion",
                5,
                3,
                new Color(0.55f, 0.12f, 0.85f, 1f),
                new Vector3(-1.49f, 1.185f, 1.02f)),
            new PotionBuildSpec(
                "cyan",
                "Cyan Potion",
                7,
                4,
                new Color(0.05f, 0.85f, 0.9f, 1f),
                new Vector3(-1.30f, 1.185f, 1.02f)),
            new PotionBuildSpec(
                "orange",
                "Orange Potion",
                4,
                2,
                new Color(1f, 0.32f, 0.04f, 1f),
                new Vector3(-0.58f, 0.985f, 0.64f)),
            new PotionBuildSpec(
                "magenta",
                "Magenta Potion",
                9,
                5,
                new Color(0.95f, 0.07f, 0.48f, 1f),
                new Vector3(-0.30f, 0.985f, 0.64f))
        };

        /// <summary>
        /// Creates or normalizes only project-owned potion and puzzle assets.
        /// Existing red, green and yellow definitions are loaded read-only and
        /// remain the definitions used by the protected source scene.
        /// </summary>
        internal static VisualPassGameplayExtensionData BuildOrUpdateData()
        {
            EnsureFolder(PotionDataFolder);
            EnsureFolder(PuzzleDataFolder);

            PotionDefinition red = LoadRequiredAsset<PotionDefinition>(
                PotionDataFolder + "/SO_PotionRed.asset");
            PotionDefinition green = LoadRequiredAsset<PotionDefinition>(
                PotionDataFolder + "/SO_PotionGreen.asset");
            PotionDefinition yellow = LoadRequiredAsset<PotionDefinition>(
                PotionDataFolder + "/SO_PotionYellow.asset");

            var newDefinitions = new PotionDefinition[
                NewPotionSpecs.Length];

            for (int index = 0;
                 index < NewPotionSpecs.Length;
                 index++)
            {
                PotionBuildSpec spec = NewPotionSpecs[index];
                string path = PotionDataFolder + "/SO_Potion" +
                    ToTitleCase(spec.StableId) + ".asset";

                newDefinitions[index] = UpsertPotionDefinition(
                    path,
                    spec);
            }

            var allDefinitions = new PotionDefinition[8]
            {
                red,
                green,
                yellow,
                newDefinitions[0],
                newDefinitions[1],
                newDefinitions[2],
                newDefinitions[3],
                newDefinitions[4]
            };

            PuzzleDefinition expandedPuzzle = UpsertExpandedPuzzle(
                allDefinitions);
            GameObject potionPrefab =
                LoadRequiredAsset<GameObject>(PotionBottlePrefabPath);
            ValidatePotionPrefab(potionPrefab);

            for (int index = 0;
                 index < newDefinitions.Length;
                 index++)
            {
                AssetDatabase.SaveAssetIfDirty(newDefinitions[index]);
            }
            AssetDatabase.SaveAssetIfDirty(expandedPuzzle);

            Debug.Log(
                "[WC_GAMEPLAY_EXTENSION] Data ready: 8 unique potions, " +
                "capacity 8, expected optimum blue + orange + magenta " +
                "(health 15, fill 8).");

            return new VisualPassGameplayExtensionData(
                expandedPuzzle,
                potionPrefab,
                allDefinitions,
                newDefinitions);
        }

        /// <summary>
        /// Rebuilds the expanded gameplay objects only in the dedicated visual
        /// scene. No source scene, Runtime script or shared prefab is saved.
        /// </summary>
        internal static VisualPassGameplayExtensionData Compose(
            Scene scene,
            VisualPassGameplayExtensionData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ValidateTargetScene(scene);

            GameObject[] previousRoots = FindGeneratedRoots(scene);
            PotionController[] originalPotions =
                FindOriginalPotions(scene, previousRoots, data);
            PotionController[] legacyExtensionPotions =
                FindLegacyExtensionPotions(scene, previousRoots, data);
            GameSessionController gameSession =
                FindUniqueInScene<GameSessionController>(scene);
            RoomResetCoordinator roomReset =
                FindUniqueInScene<RoomResetCoordinator>(scene);
            PotionInspectionPanel inspectionPanel =
                FindUniqueInScene<PotionInspectionPanel>(scene);
            PhysicsResettable wandReset = FindExistingWandReset(scene);

            for (int index = 0;
                 index < previousRoots.Length;
                 index++)
            {
                UnityEngine.Object.DestroyImmediate(
                    previousRoots[index]);
            }

            var gameplayRoot = new GameObject(GameplayRootName);
            SceneManager.MoveGameObjectToScene(gameplayRoot, scene);
            gameplayRoot.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            gameplayRoot.transform.localScale = Vector3.one;

            var newPotions = new PotionController[
                NewPotionSpecs.Length];
            for (int index = 0;
                 index < NewPotionSpecs.Length;
                 index++)
            {
                PotionBuildSpec spec = NewPotionSpecs[index];
                PotionDefinition definition =
                    data.FindDefinition(spec.StableId);

                PotionController legacyPotion =
                    FindLegacyPotion(
                        legacyExtensionPotions,
                        definition,
                        spec.StableId);
                newPotions[index] = legacyPotion != null
                    ? AdoptPotionInstance(
                        gameplayRoot.transform,
                        legacyPotion,
                        definition,
                        spec)
                    : CreatePotionInstance(
                        scene,
                        gameplayRoot.transform,
                        data.PotionBottlePrefab,
                        definition,
                        spec);
            }

            PotionController[] allPotions = CombinePotions(
                originalPotions,
                newPotions);
            PhysicsResettable[] potionPhysics =
                ResolvePotionPhysics(allPotions);
            PotionInspectionSource[] inspectionSources =
                ResolvePotionInspectionSources(allPotions);

            AssignObjectReference(
                gameSession,
                "_puzzleDefinition",
                data.ExpandedPuzzle);
            AssignObjectArray(
                roomReset,
                "_potions",
                allPotions);
            AssignObjectArray(
                inspectionPanel,
                "_sources",
                inspectionSources);

            var resetPhysics = new PhysicsResettable[9];
            for (int index = 0;
                 index < potionPhysics.Length;
                 index++)
            {
                resetPhysics[index] = potionPhysics[index];
            }
            resetPhysics[resetPhysics.Length - 1] = wandReset;

            AssignObjectArray(
                roomReset,
                "_physicsObjects",
                resetPhysics);

            BoxCollider[] tableColliders =
            {
                CreateStaticBoxCollider(
                    gameplayRoot.transform,
                    MainWorkbenchBodyColliderName,
                    new Vector3(0.05f, 0.47f, 0.92f),
                    new Vector3(2.36f, 0.74f, 0.84f)),
                CreateStaticBoxCollider(
                    gameplayRoot.transform,
                    CentralWorktopSurfaceColliderName,
                    new Vector3(-0.38f, 0.765f, 1.01f),
                    new Vector3(0.66f, 0.16f, 0.64f)),
                CreateStaticBoxCollider(
                    gameplayRoot.transform,
                    ExpandedPotionWorktopColliderName,
                    new Vector3(-0.36f, 0.765f, 0.64f),
                    new Vector3(0.94f, 0.16f, 0.26f)),
                CreateStaticBoxCollider(
                    gameplayRoot.transform,
                    PotionExtensionBodyColliderName,
                    new Vector3(-1.49f, 0.55f, 1.02f),
                    new Vector3(0.64f, 0.99f, 0.44f)),
                CreateStaticBoxCollider(
                    gameplayRoot.transform,
                    RightWorktopSurfaceColliderName,
                    new Vector3(0.84f, 0.765f, 1.01f),
                    new Vector3(0.78f, 0.16f, 0.64f)),
                CreateStaticBoxCollider(
                    gameplayRoot.transform,
                    WorkbenchBridgeSurfaceColliderName,
                    new Vector3(0.20f, 0.765f, 0.745f),
                    new Vector3(0.54f, 0.16f, 0.11f)),
                CreateStaticBoxCollider(
                    gameplayRoot.transform,
                    FrontRoomBoundaryColliderName,
                    new Vector3(0f, 1.50f, -1.02f),
                    new Vector3(4.10f, 3.00f, 0.10f))
            };

            data.SetComposedSceneState(
                gameplayRoot,
                allPotions,
                potionPhysics,
                wandReset,
                tableColliders);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                "[WC_GAMEPLAY_EXTENSION] Scene extension rebuilt: 5 new " +
                "interactive potions, 8 resettable potions, the existing " +
                "wand reset, six static workbench colliders and one " +
                "front-room boundary.");

            return data;
        }

        private static PotionDefinition UpsertPotionDefinition(
            string path,
            PotionBuildSpec spec)
        {
            PotionDefinition definition =
                AssetDatabase.LoadAssetAtPath<PotionDefinition>(path);

            if (definition == null)
            {
                UnityEngine.Object existing =
                    AssetDatabase.LoadMainAssetAtPath(path);
                if (existing != null)
                {
                    throw new InvalidOperationException(
                        "Cannot create PotionDefinition because another " +
                        "asset already occupies " + path + ".");
                }

                definition = ScriptableObject.CreateInstance<
                    PotionDefinition>();
                definition.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(definition, path);
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.Update();
            RequireProperty(serialized, "_stableId").stringValue =
                spec.StableId;
            RequireProperty(serialized, "_displayName").stringValue =
                spec.DisplayName;
            RequireProperty(serialized, "_healthValue").intValue =
                spec.HealthValue;
            RequireProperty(serialized, "_fillValue").intValue =
                spec.FillValue;
            RequireProperty(serialized, "_presentationColor").colorValue =
                spec.PresentationColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            return definition;
        }

        private static PuzzleDefinition UpsertExpandedPuzzle(
            PotionDefinition[] definitions)
        {
            PuzzleDefinition puzzle =
                AssetDatabase.LoadAssetAtPath<PuzzleDefinition>(
                    ExpandedPuzzlePath);

            if (puzzle == null)
            {
                UnityEngine.Object existing =
                    AssetDatabase.LoadMainAssetAtPath(
                        ExpandedPuzzlePath);
                if (existing != null)
                {
                    throw new InvalidOperationException(
                        "Cannot create expanded PuzzleDefinition because " +
                        "another asset already occupies " +
                        ExpandedPuzzlePath + ".");
                }

                puzzle = ScriptableObject.CreateInstance<
                    PuzzleDefinition>();
                puzzle.name = Path.GetFileNameWithoutExtension(
                    ExpandedPuzzlePath);
                AssetDatabase.CreateAsset(
                    puzzle,
                    ExpandedPuzzlePath);
            }

            SerializedObject serialized = new SerializedObject(puzzle);
            serialized.Update();
            RequireProperty(
                serialized,
                "_cauldronCapacity").intValue = 8;
            SerializedProperty potions = RequireProperty(
                serialized,
                "_potions");
            potions.arraySize = definitions.Length;
            for (int index = 0;
                 index < definitions.Length;
                 index++)
            {
                potions.GetArrayElementAtIndex(index)
                    .objectReferenceValue = definitions[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(puzzle);

            if (!puzzle.TryValidate(out string error))
            {
                throw new InvalidOperationException(
                    "Expanded visual puzzle is invalid: " + error);
            }

            return puzzle;
        }

        private static PotionController CreatePotionInstance(
            Scene scene,
            Transform parent,
            GameObject prefab,
            PotionDefinition definition,
            PotionBuildSpec spec)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Unity could not instantiate " +
                    PotionBottlePrefabPath + ".");
            }

            instance.name = "Potion_" + ToTitleCase(spec.StableId);
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(
                spec.Position,
                Quaternion.identity);
            instance.transform.localScale = Vector3.one;

            PotionController potion =
                RequireComponent<PotionController>(instance);
            Rigidbody rigidbody = RequireComponent<Rigidbody>(instance);
            RequireComponent<PhysicsResettable>(instance);
            StabilizeAddedPotion(rigidbody);

            if (instance.GetComponentInChildren<Collider>(true) == null)
            {
                throw new InvalidOperationException(
                    instance.name + " has no Collider from the shared " +
                    "potion prefab.");
            }

            AssignObjectReference(
                potion,
                "_definition",
                definition);

            PrefabUtility.RecordPrefabInstancePropertyModifications(
                instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                potion);

            return potion;
        }

        private static PotionController[] FindOriginalPotions(
            Scene scene,
            GameObject[] generatedRoots,
            VisualPassGameplayExtensionData data)
        {
            var candidates = new List<PotionController>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (ContainsReference(generatedRoots, root))
                {
                    continue;
                }

                candidates.AddRange(
                    root.GetComponentsInChildren<
                        PotionController>(true));
            }

            var ordered = new PotionController[3];
            for (int definitionIndex = 0;
                 definitionIndex < 3;
                 definitionIndex++)
            {
                PotionDefinition expected =
                    data.PotionDefinitions[definitionIndex];

                for (int candidateIndex = 0;
                     candidateIndex < candidates.Count;
                     candidateIndex++)
                {
                    PotionController candidate =
                        candidates[candidateIndex];
                    if (candidate.Definition == expected)
                    {
                        if (ordered[definitionIndex] != null)
                        {
                            throw new InvalidOperationException(
                                "More than one original potion references " +
                                expected.name + ".");
                        }

                        ordered[definitionIndex] = candidate;
                    }
                }

                if (ordered[definitionIndex] == null)
                {
                    throw new InvalidOperationException(
                        "The visual scene is missing the protected original " +
                        "potion that references " + expected.name + ".");
                }
            }

            return ordered;
        }

        private static PotionController[] FindLegacyExtensionPotions(
            Scene scene,
            GameObject[] generatedRoots,
            VisualPassGameplayExtensionData data)
        {
            var candidates = new List<PotionController>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (ContainsReference(generatedRoots, root))
                {
                    continue;
                }

                candidates.AddRange(
                    root.GetComponentsInChildren<PotionController>(true));
            }

            var legacy = new List<PotionController>();
            for (int candidateIndex = 0;
                 candidateIndex < candidates.Count;
                 candidateIndex++)
            {
                PotionController candidate = candidates[candidateIndex];
                if (candidate == null || candidate.Definition == null ||
                    !ContainsDefinition(
                        data.NewPotionDefinitions,
                        candidate.Definition))
                {
                    continue;
                }

                legacy.Add(candidate);
            }

            return legacy.ToArray();
        }

        private static PotionController FindLegacyPotion(
            PotionController[] legacyPotions,
            PotionDefinition definition,
            string stableId)
        {
            PotionController match = null;
            for (int index = 0;
                 index < legacyPotions.Length;
                 index++)
            {
                PotionController candidate = legacyPotions[index];
                if (candidate == null || candidate.Definition == null ||
                    !string.Equals(
                        candidate.Definition.StableId,
                        stableId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "More than one legacy target-scene potion uses " +
                        stableId + ". Resolve the duplicate before rebuilding.");
                }

                match = candidate;
            }

            return match;
        }

        private static bool ContainsDefinition(
            IReadOnlyList<PotionDefinition> definitions,
            PotionDefinition candidate)
        {
            for (int index = 0;
                 index < definitions.Count;
                 index++)
            {
                if (definitions[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static PotionController AdoptPotionInstance(
            Transform parent,
            PotionController potion,
            PotionDefinition definition,
            PotionBuildSpec spec)
        {
            if (potion == null)
            {
                throw new ArgumentNullException(nameof(potion));
            }

            potion.gameObject.name = "Potion_" + ToTitleCase(spec.StableId);
            potion.transform.SetParent(parent, false);
            potion.transform.SetPositionAndRotation(
                spec.Position,
                Quaternion.identity);
            potion.transform.localScale = Vector3.one;

            Rigidbody rigidbody = RequireComponent<Rigidbody>(potion.gameObject);
            RequireComponent<PhysicsResettable>(potion.gameObject);
            RequireComponent<PotionInspectionSource>(potion.gameObject);
            StabilizeAddedPotion(rigidbody);
            if (potion.GetComponentInChildren<Collider>(true) == null)
            {
                throw new InvalidOperationException(
                    potion.name + " has no Collider from its legacy " +
                    "target-scene instance.");
            }

            AssignObjectReference(potion, "_definition", definition);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                potion.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(potion);

            return potion;
        }

        private static void StabilizeAddedPotion(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                throw new ArgumentNullException(nameof(rigidbody));
            }

            // These five bottles are presentation stock placed on narrow
            // display surfaces.  Keep their upright presentation while still
            // leaving them dynamic and throwable when grabbed; only angular
            // motion is constrained, so the existing XR interaction and
            // potion gameplay remain intact.
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private static PhysicsResettable FindExistingWandReset(
            Scene scene)
        {
            WandTip wandTip = FindUniqueInScene<WandTip>(scene);
            PhysicsResettable reset =
                wandTip.GetComponentInParent<PhysicsResettable>();

            if (reset == null)
            {
                throw new InvalidOperationException(
                    "The protected WandTip has no PhysicsResettable parent.");
            }

            return reset;
        }

        private static PotionController[] CombinePotions(
            PotionController[] original,
            PotionController[] added)
        {
            var result = new PotionController[
                original.Length + added.Length];
            Array.Copy(original, 0, result, 0, original.Length);
            Array.Copy(
                added,
                0,
                result,
                original.Length,
                added.Length);
            return result;
        }

        private static PhysicsResettable[] ResolvePotionPhysics(
            PotionController[] potions)
        {
            var result = new PhysicsResettable[potions.Length];

            for (int index = 0;
                 index < potions.Length;
                 index++)
            {
                result[index] = RequireComponent<PhysicsResettable>(
                    potions[index].gameObject);
            }

            return result;
        }

        private static PotionInspectionSource[] ResolvePotionInspectionSources(
            PotionController[] potions)
        {
            var result = new PotionInspectionSource[potions.Length];

            for (int index = 0;
                 index < potions.Length;
                 index++)
            {
                PotionController potion = potions[index];
                PotionInspectionSource source =
                    RequireComponent<PotionInspectionSource>(
                        potion.gameObject);

                if (source.Potion != potion)
                {
                    throw new InvalidOperationException(
                        GetHierarchyPath(potion.transform) +
                        " has a PotionInspectionSource that does not " +
                        "reference its own PotionController.");
                }

                result[index] = source;
            }

            return result;
        }

        private static BoxCollider CreateStaticBoxCollider(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size)
        {
            var colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.SetPositionAndRotation(
                position,
                Quaternion.identity);
            colliderObject.transform.localScale = Vector3.one;
            colliderObject.isStatic = true;

            BoxCollider collider =
                colliderObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;
            collider.isTrigger = false;
            collider.enabled = true;

            return collider;
        }

        private static void AssignObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            RequireProperty(
                serialized,
                propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    target);
            }
        }

        private static void AssignObjectArray<T>(
            UnityEngine.Object target,
            string propertyName,
            T[] values)
            where T : UnityEngine.Object
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = RequireProperty(
                serialized,
                propertyName);
            property.arraySize = values.Length;

            for (int index = 0;
                 index < values.Length;
                 index++)
            {
                property.GetArrayElementAtIndex(index)
                    .objectReferenceValue = values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    target);
            }
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    serialized.targetObject.GetType().Name +
                    " has no serialized property '" + propertyName + "'.");
            }

            return property;
        }

        private static T FindUniqueInScene<T>(Scene scene)
            where T : Component
        {
            var matches = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                matches.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            if (matches.Count != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + typeof(T).Name +
                    " in the visual scene, found " + matches.Count + ".");
            }

            return matches[0];
        }

        private static GameObject[] FindGeneratedRoots(Scene scene)
        {
            var matches = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == GameplayRootName)
                {
                    matches.Add(roots[index]);
                }
            }

            return matches.ToArray();
        }

        private static bool ContainsReference(
            GameObject[] objects,
            GameObject candidate)
        {
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidatePotionPrefab(GameObject prefab)
        {
            RequireComponent<PotionController>(prefab);
            RequireComponent<Rigidbody>(prefab);
            RequireComponent<PhysicsResettable>(prefab);
            RequireComponent<PotionInspectionSource>(prefab);

            if (prefab.GetComponentInChildren<Collider>(true) == null)
            {
                throw new InvalidOperationException(
                    PotionBottlePrefabPath + " has no Collider.");
            }
        }

        private static T RequireComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    gameObject.name + " has no " + typeof(T).Name + ".");
            }

            return component;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            var parts = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static T LoadRequiredAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException(
                    "Required asset is missing.",
                    path);
            }

            return asset;
        }

        private static void ValidateTargetScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Gameplay extension requires a valid loaded scene.");
            }

            if (!string.Equals(
                scene.path,
                VisualPassBuilder.VisualScenePath,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Gameplay extension can only modify " +
                    VisualPassBuilder.VisualScenePath + ", not " +
                    scene.path + ".");
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)
                ?.Replace('\\', '/');
            string name = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) ||
                !AssetDatabase.IsValidFolder(parent))
            {
                throw new DirectoryNotFoundException(
                    "Cannot create project data folder because its parent " +
                    "is missing: " + folderPath);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static string ToTitleCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return char.ToUpperInvariant(value[0]) +
                value.Substring(1);
        }

        private sealed class PotionBuildSpec
        {
            internal PotionBuildSpec(
                string stableId,
                string displayName,
                int healthValue,
                int fillValue,
                Color presentationColor,
                Vector3 position)
            {
                StableId = stableId;
                DisplayName = displayName;
                HealthValue = healthValue;
                FillValue = fillValue;
                PresentationColor = presentationColor;
                Position = position;
            }

            internal string StableId { get; }
            internal string DisplayName { get; }
            internal int HealthValue { get; }
            internal int FillValue { get; }
            internal Color PresentationColor { get; }
            internal Vector3 Position { get; }
        }
    }
}
