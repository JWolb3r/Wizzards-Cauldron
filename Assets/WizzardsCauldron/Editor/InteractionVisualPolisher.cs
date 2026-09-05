using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.Presentation;
using WizzardsCauldron.UI;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Targeted, repeatable polish for the two physical controls. This tool
    /// never runs the full visual builder. It only applies the explicitly
    /// approved enlarged finish-rune trigger in the copied visual scene.
    /// </summary>
    internal static class InteractionVisualPolisher
    {
        private const string FinishRuneName = "WC_FinishTargetRune";
        private const string ResetRuneName = "WC_ResetRuneButton";
        private const string ResetWandTriggerName = "WC_ResetWandTrigger";
        private const string GameplayAudioName = "WC_GameplayAudioFeedback";
        private const string GameplayExtensionRootName =
            "__WC_GAMEPLAY_EXTENSION__";

        private const string CyanMaterialPath =
            "Assets/WizzardsCauldron/Art/Materials/MAT_VP_CyanEmission.mat";
        private const string VioletMaterialPath =
            "Assets/WizzardsCauldron/Art/Materials/MAT_VP_VioletEmission.mat";
        private const string GoldMaterialPath =
            "Assets/WizzardsCauldron/Art/Materials/MAT_VP_Gold.mat";
        private const string IronMaterialPath =
            "Assets/WizzardsCauldron/Art/Materials/MAT_VP_BlackIron.mat";
        private const string ParticleMaterialPath =
            "Assets/WizzardsCauldron/Art/Materials/MAT_VP_PortalParticle.mat";
        private const string DiscMeshPath =
            "Assets/WizzardsCauldron/Art/Models/Generated/MSH_VP_Disc.asset";
        private const string RingMeshPath =
            "Assets/WizzardsCauldron/Art/Models/Generated/MSH_VP_Ring.asset";
        private const string RuneMeshPath =
            "Assets/WizzardsCauldron/Art/Models/Generated/MSH_VP_RuneRing.asset";

        [MenuItem(
            "Tools/Wizzards Cauldron/Polish Interaction Targets",
            priority = 202)]
        public static void PolishInteractionTargets()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            PolishInteractionTargetsBatch();
            EditorUtility.DisplayDialog(
                "Wizzards Cauldron",
                "Wand rune and reset rune were updated without rebuilding the visual scene.",
                "OK");
        }

        public static void PolishInteractionTargetsBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(
                VisualPassBuilder.VisualScenePath,
                OpenSceneMode.Single);

            PolishLoadedScene(scene);

            if (!EditorSceneManager.SaveScene(
                scene,
                VisualPassBuilder.VisualScenePath,
                false))
            {
                throw new InvalidOperationException(
                    "Could not save the polished visual scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[WC_INTERACTION_VISUALS] COMPLETE | fullVisualBuild=false | protectedSourceUnchanged=true | finishHitboxRadius=0.9");
        }

        public static void LogVisibleCyanRenderersBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(
                VisualPassBuilder.VisualScenePath,
                OpenSceneMode.Single);
            Material cyan = LoadRequired<Material>(CyanMaterialPath);

            Renderer[] renderers = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer =>
                    renderer.enabled &&
                    renderer.sharedMaterials.Contains(cyan))
                .ToArray();

            foreach (Renderer renderer in renderers)
            {
                Debug.Log(
                    "[WC_CYAN_RENDERER] " +
                    GetHierarchyPath(renderer.transform) +
                    " | type=" + renderer.GetType().Name +
                    " | center=" + renderer.bounds.center.ToString("F3") +
                    " | size=" + renderer.bounds.size.ToString("F3"));
            }

            Debug.Log("[WC_CYAN_RENDERER] count=" + renderers.Length);

            Renderer[] controlRenderers = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer =>
                {
                    string path = GetHierarchyPath(renderer.transform);
                    return path.IndexOf(
                               "Placeholders/ResetControl",
                               StringComparison.Ordinal) >= 0 ||
                           path.IndexOf(
                               "Placeholders/FinishTarget",
                               StringComparison.Ordinal) >= 0;
                })
                .ToArray();

            foreach (Renderer renderer in controlRenderers)
            {
                string materials = string.Join(
                    ",",
                    renderer.sharedMaterials.Select(material =>
                        material == null
                            ? "<null>"
                            : AssetDatabase.GetAssetPath(material)));
                Debug.Log(
                    "[WC_CONTROL_RENDERER] " +
                    GetHierarchyPath(renderer.transform) +
                    " | enabled=" + renderer.enabled +
                    " | center=" + renderer.bounds.center.ToString("F3") +
                    " | size=" + renderer.bounds.size.ToString("F3") +
                    " | materials=" + materials);
            }

            Renderer[] nearbyRenderers = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer =>
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        return false;
                    }

                    Vector3 center = renderer.bounds.center;
                    return center.x >= -2.5f && center.x <= 2.5f &&
                           center.y >= 0.55f && center.y <= 2.25f &&
                           center.z >= -1.5f && center.z <= 2.1f;
                })
                .ToArray();

            foreach (Renderer renderer in nearbyRenderers)
            {
                string materials = string.Join(
                    ",",
                    renderer.sharedMaterials.Select(material =>
                        material == null
                            ? "<null>"
                            : AssetDatabase.GetAssetPath(material)));
                Debug.Log(
                    "[WC_NEARBY_RENDERER] " +
                    GetHierarchyPath(renderer.transform) +
                    " | type=" + renderer.GetType().Name +
                    " | center=" + renderer.bounds.center.ToString("F3") +
                    " | size=" + renderer.bounds.size.ToString("F3") +
                    " | materials=" + materials);
            }

            Debug.Log("[WC_NEARBY_RENDERER] count=" + nearbyRenderers.Length);
        }

        public static void LogPotionPhysicsBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(
                VisualPassBuilder.VisualScenePath,
                OpenSceneMode.Single);
            Physics.SyncTransforms();

            Collider[] sceneColliders = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Collider>(true))
                .Where(collider =>
                    collider.enabled &&
                    collider.gameObject.activeInHierarchy &&
                    !collider.isTrigger)
                .ToArray();

            PotionController[] potions = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<PotionController>(true))
                .OrderBy(potion => potion.name)
                .ToArray();

            foreach (PotionController potion in potions)
            {
                Rigidbody body = potion.GetComponent<Rigidbody>();
                Collider[] potionColliders = potion
                    .GetComponentsInChildren<Collider>(true)
                    .Where(collider =>
                        collider.enabled &&
                        !collider.isTrigger)
                    .ToArray();
                string contacts = string.Join(
                    ",",
                    sceneColliders
                        .Where(other =>
                            !other.transform.IsChildOf(potion.transform) &&
                            potionColliders.Any(own =>
                                own.bounds.Intersects(other.bounds)))
                        .Select(other => GetHierarchyPath(other.transform))
                        .Distinct());
                string bounds = string.Join(
                    ";",
                    potionColliders.Select(collider =>
                        collider.GetType().Name +
                        " center=" + collider.bounds.center.ToString("F3") +
                        " size=" + collider.bounds.size.ToString("F3")));
                Debug.Log(
                    "[WC_POTION_PHYSICS] " + potion.name +
                    " | position=" + potion.transform.position.ToString("F3") +
                    " | rotation=" + potion.transform.rotation.eulerAngles.ToString("F2") +
                    " | mass=" + (body == null ? -1f : body.mass) +
                    " | collision=" +
                    (body == null ? "<none>" : body.collisionDetectionMode.ToString()) +
                    " | bounds=" + bounds +
                    " | touchingBounds=" +
                    (string.IsNullOrEmpty(contacts) ? "<none>" : contacts));
            }

            foreach (Collider collider in sceneColliders
                .Where(collider =>
                    collider.bounds.max.y >= 0.65f &&
                    collider.bounds.min.y <= 1.10f &&
                    collider.bounds.center.z >= 0.45f &&
                    collider.bounds.center.z <= 1.35f))
            {
                Debug.Log(
                    "[WC_SUPPORT_COLLIDER] " +
                    GetHierarchyPath(collider.transform) +
                    " | center=" + collider.bounds.center.ToString("F3") +
                    " | size=" + collider.bounds.size.ToString("F3") +
                    " | min=" + collider.bounds.min.ToString("F3") +
                    " | max=" + collider.bounds.max.ToString("F3"));
            }
        }

        internal static void PolishLoadedScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Interaction polish requires the loaded visual scene.");
            }

            Material cyan = LoadRequired<Material>(CyanMaterialPath);
            Material violet = LoadRequired<Material>(VioletMaterialPath);
            Material gold = LoadRequired<Material>(GoldMaterialPath);
            Material iron = LoadRequired<Material>(IronMaterialPath);
            Material particles = LoadRequired<Material>(ParticleMaterialPath);
            Mesh disc = LoadRequired<Mesh>(DiscMeshPath);
            Mesh ring = LoadRequired<Mesh>(RingMeshPath);
            Mesh runes = LoadRequired<Mesh>(RuneMeshPath);

            WandActivator wandActivator = FindUnique<WandActivator>(scene);
            ResetHoldControl resetControl = FindUnique<ResetHoldControl>(scene);
            GameSessionController session =
                FindUnique<GameSessionController>(scene);
            RoomResetCoordinator roomReset =
                FindUnique<RoomResetCoordinator>(scene);
            CauldronController cauldron =
                FindUnique<CauldronController>(scene);

            RemoveRedPotionAndRepairRoster(scene, roomReset);
            AlignCauldronLiquid(cauldron);
            StabilizePotionStartPoses(scene);
            EnsureAmbientMusic(scene);
            EnsureGameplayAudioFeedback(scene, session);
            UpdateInstructionText(scene);

            PolishWandTarget(
                wandActivator,
                session,
                disc,
                ring,
                runes,
                iron,
                gold,
                cyan,
                violet,
                particles);

            PolishResetButton(
                scene,
                resetControl,
                roomReset,
                ring,
                runes,
                gold,
                cyan,
                violet,
                particles);

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void StabilizePotionStartPoses(Scene scene)
        {
            PotionController[] potions = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<PotionController>(true))
                .ToArray();

            SetPotionStartPose(
                potions,
                "PotionGreen",
                new Vector3(-0.259f, 0.967f, 0.72f));
        }

        private static void RemoveRedPotionAndRepairRoster(
            Scene scene,
            RoomResetCoordinator roomReset)
        {
            PotionController[] existing = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<PotionController>(true))
                .ToArray();
            foreach (PotionController potion in existing)
            {
                bool isRed = string.Equals(
                    potion.name,
                    "PotionRed",
                    StringComparison.Ordinal) ||
                    potion.Definition != null && string.Equals(
                        potion.Definition.StableId,
                        "red",
                        StringComparison.OrdinalIgnoreCase);
                if (isRed)
                {
                    UnityEngine.Object.DestroyImmediate(
                        potion.gameObject);
                }
            }

            string[] orderedNames =
            {
                "PotionGreen",
                "PotionYellow",
                "Potion_Blue",
                "Potion_Violet",
                "Potion_Cyan",
                "Potion_Orange",
                "Potion_Magenta"
            };
            PotionController[] orderedPotions = orderedNames
                .Select(name => scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        PotionController>(true))
                    .Single(potion => string.Equals(
                        potion.name,
                        name,
                        StringComparison.Ordinal)))
                .ToArray();
            PhysicsResettable[] potionPhysics = orderedPotions
                .Select(potion =>
                    potion.GetComponent<PhysicsResettable>())
                .ToArray();
            PhysicsResettable wandPhysics = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<
                    PhysicsResettable>(true))
                .Single(resettable => string.Equals(
                    resettable.name,
                    "Wand",
                    StringComparison.Ordinal));

            SerializedObject resetSerialized =
                new SerializedObject(roomReset);
            SetObjectArray(
                resetSerialized,
                "_potions",
                orderedPotions);
            SetObjectArray(
                resetSerialized,
                "_physicsObjects",
                potionPhysics.Cast<UnityEngine.Object>()
                    .Concat(new UnityEngine.Object[] { wandPhysics })
                    .ToArray());
            resetSerialized.ApplyModifiedPropertiesWithoutUndo();

            PotionInspectionPanel inspectionPanel =
                FindUnique<PotionInspectionPanel>(scene);
            PotionInspectionSource[] inspectionSources = orderedPotions
                .Select(potion =>
                    potion.GetComponent<PotionInspectionSource>())
                .ToArray();
            SerializedObject inspectionSerialized =
                new SerializedObject(inspectionPanel);
            SetObjectArray(
                inspectionSerialized,
                "_sources",
                inspectionSources);
            inspectionSerialized.ApplyModifiedPropertiesWithoutUndo();

            PuzzleDefinition expandedPuzzle =
                LoadRequired<PuzzleDefinition>(
                    VisualPassGameplayExtension.ExpandedPuzzlePath);
            SerializedObject puzzleSerialized =
                new SerializedObject(expandedPuzzle);
            SetObjectArray(
                puzzleSerialized,
                "_potions",
                orderedPotions
                    .Select(potion => potion.Definition)
                    .ToArray());
            puzzleSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(expandedPuzzle);

            Debug.Log(
                "[WC_INTERACTION_VISUALS] PotionRed removed from the visual " +
                "scene, reset roster, inspection roster and expanded puzzle.");
        }

        private static void SetPotionStartPose(
            PotionController[] potions,
            string potionName,
            Vector3 localPosition)
        {
            PotionController potion = potions.Single(candidate =>
                string.Equals(
                    candidate.name,
                    potionName,
                    StringComparison.Ordinal));
            potion.transform.localPosition = localPosition;
            potion.transform.localRotation = Quaternion.identity;
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                potion.transform);
        }

        private static void EnsureAmbientMusic(Scene scene)
        {
            GameObject visualRoot = scene.GetRootGameObjects()
                .Single(root => string.Equals(
                    root.name,
                    "__WC_VISUAL_PASS__",
                    StringComparison.Ordinal));
            DestroyDirectChild(
                visualRoot.transform,
                "WC_AstralAmbientMusic");

            GameObject musicObject = new GameObject(
                "WC_AstralAmbientMusic");
            musicObject.transform.SetParent(visualRoot.transform, false);
            AudioSource source = musicObject.AddComponent<AudioSource>();
            source.playOnAwake = true;
            source.loop = true;
            source.volume = 0.34f;
            source.mute = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 160;
            musicObject.AddComponent<AstralAmbientMusic>();
        }

        private static void EnsureGameplayAudioFeedback(
            Scene scene,
            GameSessionController session)
        {
            GameObject visualRoot = scene.GetRootGameObjects()
                .Single(root => string.Equals(
                    root.name,
                    "__WC_VISUAL_PASS__",
                    StringComparison.Ordinal));
            DestroyDirectChild(visualRoot.transform, GameplayAudioName);

            CauldronIntake intake = FindUnique<CauldronIntake>(scene);
            ResetHoldControl reset = FindUnique<ResetHoldControl>(scene);
            WandActivator finish = FindUnique<WandActivator>(scene);

            GameObject audioRoot = new GameObject(GameplayAudioName);
            audioRoot.transform.SetParent(visualRoot.transform, false);

            AudioSource potionSource = CreateSpatialAudioSource(
                audioRoot.transform,
                "PotionIntoCauldronSfx",
                intake.transform.position,
                0.72f);
            AudioSource resetSource = CreateSpatialAudioSource(
                audioRoot.transform,
                "ResetRuneSfx",
                reset.transform.position,
                0.66f);
            AudioSource resultSource = CreateSpatialAudioSource(
                audioRoot.transform,
                "SolutionResultSfx",
                finish.transform.position,
                0.74f);

            AlchemyAudioFeedback feedback =
                audioRoot.AddComponent<AlchemyAudioFeedback>();
            SerializedObject serialized = new SerializedObject(feedback);
            SetReference(serialized, "_cauldronIntake", intake);
            SetReference(serialized, "_gameSession", session);
            SetReference(serialized, "_potionSource", potionSource);
            SetReference(serialized, "_resetSource", resetSource);
            SetReference(serialized, "_resultSource", resultSource);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioSource CreateSpatialAudioSource(
            Transform parent,
            string name,
            Vector3 worldPosition,
            float volume)
        {
            GameObject sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(parent, false);
            sourceObject.transform.position = worldPosition;

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = volume;
            source.spatialBlend = 0.82f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.35f;
            source.maxDistance = 5.5f;
            source.priority = 96;
            return source;
        }

        private static void UpdateInstructionText(Scene scene)
        {
            TMP_Text instructionText = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<TMP_Text>(true))
                .FirstOrDefault(text =>
                    text.text != null &&
                    text.text.IndexOf(
                        "Your challenge is to brew",
                        StringComparison.Ordinal) >= 0);
            if (instructionText == null)
            {
                throw new InvalidOperationException(
                    "Could not find the visual-scene instruction text.");
            }

            instructionText.text =
                "BREW THE STRONGEST RESTORATIVE MIXTURE\n\n" +
                "1. Touch a difficulty plaque with the wand tip.\n" +
                "2. Point at potions to inspect Health and Fill. Grab them with " +
                "A/B (right) or X/Y (left), then drop them into the cauldron.\n" +
                "3. Each bottle works once and disappears. Overfilling is allowed, " +
                "but the mixture will fail.\n" +
                "4. Touch the glowing SUBMIT SOLUTION rune with the wand tip to " +
                "see your score.\n\n" +
                "HINT: Hold the wand; press B (right) or Y (left).\n" +
                "RESET: Touch the wall RESET rune with the wand tip.\n" +
                "MOVE: Left stick walks; right stick turns.";

            instructionText.fontSize = 28f;
            instructionText.lineSpacing = 2f;
            instructionText.rectTransform.anchoredPosition =
                new Vector2(0f, -30f);
            instructionText.rectTransform.sizeDelta =
                new Vector2(-140f, -180f);
        }

        private static void AlignCauldronLiquid(
            CauldronController cauldron)
        {
            Transform liquid = cauldron.transform.Find("LiquidVisual");
            Transform visualPivot = cauldron.transform.Find(
                "WC_VisualPivot_AlexCauldron");
            Renderer alexRenderer = visualPivot == null
                ? null
                : visualPivot.GetComponentsInChildren<Renderer>(true)
                    .FirstOrDefault(renderer => renderer.enabled);

            if (liquid == null || alexRenderer == null)
            {
                throw new InvalidOperationException(
                    "Could not align the cauldron liquid with Alex's visible cauldron.");
            }

            // The gameplay root intentionally stays untouched. Alex's mesh is
            // offset by its replaceable VisualPivot, so the original liquid
            // must follow the visible mesh rather than the protected root.
            Vector3 worldPosition = liquid.position;
            worldPosition.x = alexRenderer.bounds.center.x;
            worldPosition.z = alexRenderer.bounds.center.z;
            liquid.position = worldPosition;

            float worldDiameter = Mathf.Min(
                alexRenderer.bounds.size.x,
                alexRenderer.bounds.size.z) * 0.70f;
            Vector3 parentScale = liquid.parent.lossyScale;
            Vector3 localScale = liquid.localScale;
            localScale.x = worldDiameter /
                (2f * Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)));
            localScale.z = worldDiameter /
                (2f * Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
            liquid.localScale = localScale;
        }

        private static void PolishWandTarget(
            WandActivator activator,
            GameSessionController session,
            Mesh disc,
            Mesh ring,
            Mesh runes,
            Material iron,
            Material gold,
            Material cyan,
            Material violet,
            Material particleMaterial)
        {
            Transform target = activator.transform;
            SphereCollider finishTrigger =
                target.GetComponent<SphereCollider>();
            if (finishTrigger == null)
            {
                throw new InvalidOperationException(
                    "The finish target requires its SphereCollider.");
            }

            // Explicitly approved target-scene interaction adjustment: the
            // wand-tip hit area grows from roughly 15 cm to 27 cm while the
            // protected functional source scene remains unchanged.
            finishTrigger.isTrigger = true;
            finishTrigger.radius = 0.9f;

            Renderer primitiveRenderer = target.GetComponent<Renderer>();
            if (primitiveRenderer != null)
            {
                primitiveRenderer.enabled = false;
            }

            DestroyDirectChild(target, FinishRuneName);

            GameObject visual = new GameObject(FinishRuneName);
            visual.transform.SetParent(target, false);

            Transform backdrop = CreateDoubleSidedMesh(
                visual.transform,
                "DarkRuneBackdrop",
                disc,
                iron,
                1.02f,
                0.035f);

            Transform outer = CreateDoubleSidedMesh(
                visual.transform,
                "OuterCyanRunes",
                runes,
                cyan,
                1.38f,
                0.015f);

            Transform inner = CreateDoubleSidedMesh(
                visual.transform,
                "InnerGoldRing",
                ring,
                gold,
                1.22f,
                0f);

            CreateRuneGlyph(
                visual.transform,
                "CentralWandGlyph",
                cyan,
                gold,
                0.045f);

            CreateFinishLabel(visual.transform);

            // A subtle violet halo separates the gameplay rune from the
            // cyan cauldron liquid without adding another large room ring.
            CreateDoubleSidedMesh(
                visual.transform,
                "VioletHalo",
                ring,
                violet,
                1.5f,
                0.025f);

            GameObject lightObject = new GameObject("ActivationGlow");
            lightObject.transform.SetParent(visual.transform, false);
            Light activationLight = lightObject.AddComponent<Light>();
            activationLight.type = LightType.Point;
            activationLight.color = new Color(0.16f, 0.82f, 1f, 1f);
            activationLight.intensity = 0.025f;
            activationLight.range = 0.62f;
            activationLight.shadows = LightShadows.None;
            activationLight.renderMode = LightRenderMode.ForcePixel;

            ParticleSystem activationParticles = CreateFinishParticles(
                target,
                particleMaterial);

            WandFinishRuneFeedback feedback =
                target.GetComponent<WandFinishRuneFeedback>();
            if (feedback == null)
            {
                feedback = target.gameObject.AddComponent<
                    WandFinishRuneFeedback>();
            }

            SerializedObject serialized = new SerializedObject(feedback);
            SetReference(serialized, "_gameSession", session);
            SetReference(serialized, "_pulseRoot", visual.transform);
            SetReference(serialized, "_outerRing", outer);
            SetReference(serialized, "_innerRing", inner);
            SetReference(
                serialized,
                "_activationParticles",
                activationParticles);
            SetReference(
                serialized,
                "_activationLight",
                activationLight);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _ = backdrop;
        }

        private static void PolishResetButton(
            Scene scene,
            ResetHoldControl resetControl,
            RoomResetCoordinator roomReset,
            Mesh ring,
            Mesh runes,
            Material gold,
            Material cyan,
            Material violet,
            Material particleMaterial)
        {
            Transform resetRoot = resetControl.transform;
            DestroyDirectChild(resetRoot, ResetRuneName);

            // Hide both the old primitive and the elongated Alex visual. Their
            // colliders, interactable and status canvas remain untouched.
            foreach (Renderer renderer in resetRoot
                .GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            Collider interactionVolume = resetRoot
                .GetComponentsInChildren<Collider>(true)
                .FirstOrDefault(collider =>
                    string.Equals(
                        collider.gameObject.name,
                        "InteractionVolume",
                        StringComparison.Ordinal));

            Vector3 center = interactionVolume != null
                ? interactionVolume.bounds.center
                : resetRoot.position;

            GameObject visual = new GameObject(ResetRuneName);
            visual.transform.position = center;
            visual.transform.SetParent(resetRoot, true);

            Camera sceneCamera = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(camera => camera.CompareTag("MainCamera"));
            Vector3 initialFacingDirection = sceneCamera != null
                ? sceneCamera.transform.position - center
                : Vector3.left;
            visual.transform.rotation = Quaternion.LookRotation(
                initialFacingDirection.normalized,
                Vector3.up);

            Transform outerRune = CreateDoubleSidedMesh(
                visual.transform,
                "OuterVioletResetRunes",
                runes,
                violet,
                0.24f,
                0.004f);

            Transform innerRune = CreateDoubleSidedMesh(
                visual.transform,
                "InnerGoldResetRing",
                ring,
                gold,
                0.19f,
                0f);

            CreateDoubleSidedMesh(
                visual.transform,
                "CyanResetSigils",
                runes,
                cyan,
                0.12f,
                0.008f);

            CreateRuneGlyph(
                visual.transform,
                "ResetGlyph",
                gold,
                cyan,
                0.012f,
                0.085f);

            ParticleSystem activationParticles = CreateRuneParticles(
                visual.transform,
                "ResetRuneSparkBurst",
                particleMaterial,
                new Color(0.64f, 0.22f, 1f, 1f),
                new Color(0.1f, 0.9f, 1f, 1f),
                20);

            Transform extensionRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => string.Equals(
                    root.name,
                    GameplayExtensionRootName,
                    StringComparison.Ordinal))
                ?.transform;
            if (extensionRoot == null)
            {
                throw new InvalidOperationException(
                    "The authorized gameplay-extension root is missing.");
            }

            DestroyDirectChild(extensionRoot, ResetWandTriggerName);
            GameObject triggerObject = new GameObject(ResetWandTriggerName);
            triggerObject.transform.position = center;
            triggerObject.transform.SetParent(extensionRoot, true);
            SphereCollider wandTrigger =
                triggerObject.AddComponent<SphereCollider>();
            wandTrigger.isTrigger = true;
            wandTrigger.radius = 0.145f;

            TMP_Text statusText = resetRoot
                .GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault();
            if (statusText != null)
            {
                statusText.text = "Touch with wand to reset";
            }

            Behaviour legacyInteractable = resetRoot
                .GetComponents<Behaviour>()
                .FirstOrDefault(component =>
                    component != null &&
                    component != resetControl &&
                    component.GetType().Name.IndexOf(
                        "Interactable",
                        StringComparison.OrdinalIgnoreCase) >= 0);

            WandResetActivator activator =
                triggerObject.AddComponent<WandResetActivator>();
            SerializedObject serialized = new SerializedObject(activator);
            SetReference(serialized, "_roomReset", roomReset);
            SetReference(serialized, "_wandTrigger", wandTrigger);
            SetReference(
                serialized,
                "_legacyHoldControl",
                resetControl);
            SetReference(
                serialized,
                "_legacyInteractable",
                legacyInteractable);
            SetReference(serialized, "_statusText", statusText);
            SetReference(
                serialized,
                "_runeFacingRoot",
                visual.transform);
            SetReference(serialized, "_pulseRoot", visual.transform);
            SetReference(serialized, "_outerRune", outerRune);
            SetReference(serialized, "_innerRune", innerRune);
            SetReference(
                serialized,
                "_activationParticles",
                activationParticles);
            SetReference(
                serialized,
                "_activationLight",
                null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateDoubleSidedMesh(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            float scale,
            float localZ)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            CreateMesh(
                root.transform,
                "Front",
                mesh,
                material,
                new Vector3(scale, scale, 1f),
                new Vector3(0f, 0f, localZ),
                Quaternion.Euler(0f, 180f, 0f));

            CreateMesh(
                root.transform,
                "Back",
                mesh,
                material,
                new Vector3(scale, scale, 1f),
                new Vector3(0f, 0f, -localZ),
                Quaternion.identity);

            return root.transform;
        }

        private static void CreateFinishLabel(Transform parent)
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                throw new InvalidOperationException(
                    "TextMesh Pro has no default font for the finish-rune label.");
            }

            GameObject labelObject = new GameObject("SubmitSolutionLabel");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition =
                new Vector3(0f, -1.48f, 0.06f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one * 0.25f;

            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.font = font;
            label.text = "SUBMIT SOLUTION";
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 1.35f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.72f;
            label.fontSizeMax = 1.35f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.76f, 0.24f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(6f, 1.1f);
            label.raycastTarget = false;
        }

        private static void CreateRuneGlyph(
            Transform parent,
            string name,
            Material primary,
            Material secondary,
            float localZ,
            float size = 0.42f)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 0f, localZ);

            for (int index = 0; index < 4; index++)
            {
                GameObject beam = GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
                beam.name = "RuneStroke_" + (index + 1);
                beam.transform.SetParent(root.transform, false);
                beam.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    index * 45f);
                beam.transform.localScale = new Vector3(
                    size * 0.12f,
                    size,
                    size * 0.035f);

                Collider collider = beam.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                MeshRenderer renderer = beam.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = index % 2 == 0
                    ? primary
                    : secondary;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static ParticleSystem CreateFinishParticles(
            Transform target,
            Material material)
        {
            return CreateRuneParticles(
                target,
                "WandActivationSparkBurst",
                material,
                new Color(0.1f, 0.9f, 1f, 1f),
                new Color(1f, 0.58f, 0.12f, 1f),
                24);
        }

        private static ParticleSystem CreateRuneParticles(
            Transform target,
            string name,
            Material material,
            Color firstColor,
            Color secondColor,
            short burstCount)
        {
            GameObject particlesObject = new GameObject(name);
            particlesObject.transform.position = target.position;
            particlesObject.transform.rotation = Quaternion.identity;
            particlesObject.transform.SetParent(target, true);

            ParticleSystem particles =
                particlesObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.04f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                firstColor,
                secondColor);
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, burstCount)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.075f;

            ParticleSystem.ColorOverLifetimeModule color =
                particles.colorOverLifetime;
            color.enabled = true;
            Gradient alphaFade = new Gradient();
            alphaFade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = alphaFade;

            ParticleSystemRenderer particleRenderer =
                particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment =
                ParticleSystemRenderSpace.View;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            return particles;
        }

        private static GameObject CreateMesh(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            Vector3 localScale,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;

            MeshFilter filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;
            return visual;
        }

        private static void DestroyDirectChild(
            Transform parent,
            string name)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (string.Equals(
                    child.name,
                    name,
                    StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static T FindUnique<T>(Scene scene)
            where T : Component
        {
            T[] matches = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<T>(true))
                .ToArray();

            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected one " + typeof(T).Name +
                    " in the visual scene, found " + matches.Length + ".");
            }

            return matches[0];
        }

        private static T LoadRequired<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "Required interaction visual asset is missing: " + path);
            }

            return asset;
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized field " + propertyName + ".");
            }

            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object[] values)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                throw new InvalidOperationException(
                    "Missing serialized array field " + propertyName + ".");
            }

            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index)
                    .objectReferenceValue = values[index];
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
