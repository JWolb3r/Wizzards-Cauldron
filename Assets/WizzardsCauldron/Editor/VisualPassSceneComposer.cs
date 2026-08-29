using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.Presentation;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Composes only the dedicated visual scene. Shared gameplay prefabs are
    /// never opened or saved; their target-scene instances receive visual-only
    /// overrides and collider-free wrapper children.
    /// </summary>
    internal static class VisualPassSceneComposer
    {
        private const string CauldronPivotName =
            "WC_VisualPivot_AlexCauldron";
        private const string WandPivotName =
            "WC_VisualPivot_AlexWand";
        private const string PotionPivotPrefix =
            "WC_VisualPivot_AlexPotion_";
        private const string FurniturePivotName =
            "WC_VisualPivot_AlexFurniture";
        private const string ResetPivotName =
            "WC_VisualPivot_AlexReset";
        private const string CauldronRuneName =
            "WC_CauldronRuneRing";
        private const string FinishVisualName =
            "WC_FinishTargetRune";

        private const string BookNecromancyPath =
            "Assets/GreyratsLab/Stylized magic books/Prefab/Single material/SM_Nec book.prefab";
        private const string BookFirePath =
            "Assets/GreyratsLab/Stylized magic books/Prefab/Single material/SM_Fire book.prefab";
        private const string BookIcePath =
            "Assets/GreyratsLab/Stylized magic books/Prefab/Single material/SM_Ice book.prefab";
        private const string BlueGemPath =
            "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Gemstone_13_Blue.prefab";
        private const string GoldCrystalPath =
            "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Stone_Crystal_02_Gold.prefab";
        private const string LanternPath =
            "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Lantern_02_Yellow_Light_Iron.prefab";
        private const string ChestPath =
            "Assets/StylizedDarkCastle/Prefabs/CastleInterior/Chest.prefab";

        internal static void Compose(
            Scene scene,
            VisualPassAssets assets,
            VisualPassGameplayExtensionData gameplayData)
        {
            if (assets == null)
            {
                throw new ArgumentNullException(nameof(assets));
            }
            if (gameplayData == null)
            {
                throw new ArgumentNullException(nameof(gameplayData));
            }

            VisualPassGameplayExtension.Compose(
                scene,
                gameplayData);

            SceneReferences references =
                FindAndValidateGameplayReferences(scene);

            ClearPreviousGeneratedObjects(scene, references);

            GameObject generatedRoot = new GameObject(
                VisualPassBuilder.GeneratedRootName);
            SceneManager.MoveGameObjectToScene(generatedRoot, scene);

            Transform architecture = CreateGroup(
                generatedRoot.transform,
                "Architecture");
            Transform portalGroup = CreateGroup(
                generatedRoot.transform,
                "AstralPortal");
            Transform decoration = CreateGroup(
                generatedRoot.transform,
                "Decoration");
            Transform lighting = CreateGroup(
                generatedRoot.transform,
                "LightingAndPost");
            Transform feedback = CreateGroup(
                generatedRoot.transform,
                "VisualFeedback");

            StyleExistingRoom(references, assets);
            BuildArchitecture(architecture, assets);

            InteractiveVisuals interactiveVisuals =
                IntegrateAlexVisuals(references, assets);

            BuildAstralPortal(portalGroup, assets);
            BuildDecoration(decoration, assets);

            LightingVisuals lightingVisuals =
                BuildLightingAndPost(
                    lighting,
                    references,
                    assets);

            BuildVisualFeedback(
                feedback,
                references,
                interactiveVisuals.CauldronRuneRenderer,
                lightingVisuals.CauldronAccent,
                assets);

            StyleWorldSpaceUi(references, assets);
            StyleFinishTarget(references, assets);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                "[WC_VISUAL_PASS] Scene composition complete: " +
                "architecture, Alex wrappers, portal, decoration, " +
                "lighting, post-processing and event VFX prepared.");
        }

        private static SceneReferences FindAndValidateGameplayReferences(
            Scene scene)
        {
            SceneReferences result = new SceneReferences
            {
                Environment = FindUniqueByName(scene, "Environment"),
                Placeholders = FindUniqueByName(scene, "Placeholders"),
                Floor = FindUniqueByName(scene, "Floor"),
                WallLeft = FindUniqueByName(scene, "Wall_Left"),
                WallRight = FindUniqueByName(scene, "Wall_Right"),
                WallBack = FindUniqueByName(scene, "Wall_Back"),
                PotionShelf = FindUniqueByName(scene, "PotionShelf"),
                WandTable = FindUniqueByName(scene, "WandTable"),
                Cauldron = FindUniqueInScene<CauldronController>(scene),
                CauldronIntake = FindUniqueInScene<CauldronIntake>(scene),
                GameSession = FindUniqueInScene<GameSessionController>(scene),
                RoomReset = FindUniqueInScene<RoomResetCoordinator>(scene),
                WandActivator = FindUniqueInScene<WandActivator>(scene),
                ResetHoldControl = FindUniqueInScene<ResetHoldControl>(scene),
                LiquidDisplay = FindUniqueInScene<CauldronLiquidDisplay>(scene),
                Potions = FindInScene<PotionController>(scene)
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .ToArray()
            };

            if (result.Potions.Length != 8)
            {
                throw new InvalidOperationException(
                    "Expected exactly eight PotionController " +
                    "instances, found " + result.Potions.Length + ".");
            }

            WandTip wandTip = FindUniqueInScene<WandTip>(scene);
            result.WandRoot = wandTip.GetComponentInParent<
                PhysicsResettable>();
            if (result.WandRoot == null)
            {
                throw new InvalidOperationException(
                    "The existing WandTip has no PhysicsResettable root.");
            }

            result.ResetRoot = result.ResetHoldControl.gameObject;
            result.FinishTarget = result.WandActivator.gameObject;

            Transform liquid = FindDirectChild(
                result.Cauldron.transform,
                "LiquidVisual");
            if (liquid == null)
            {
                throw new InvalidOperationException(
                    "The protected LiquidVisual child is missing.");
            }

            result.LiquidVisual = liquid.gameObject;

            BoxCollider intakeCollider =
                result.CauldronIntake.GetComponent<BoxCollider>();
            Collider finishCollider =
                result.FinishTarget.GetComponent<Collider>();

            if (intakeCollider == null || !intakeCollider.isTrigger)
            {
                throw new InvalidOperationException(
                    "The protected cauldron intake trigger is invalid.");
            }

            if (finishCollider == null || !finishCollider.isTrigger)
            {
                throw new InvalidOperationException(
                    "The protected wand finish trigger is invalid.");
            }

            return result;
        }

        private static void ClearPreviousGeneratedObjects(
            Scene scene,
            SceneReferences references)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == VisualPassBuilder.GeneratedRootName)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            DestroyDirectChildrenNamed(
                references.Cauldron.transform,
                CauldronPivotName,
                CauldronRuneName);
            DestroyDirectChildrenNamed(
                references.WandRoot.transform,
                WandPivotName);
            DestroyDirectChildrenNamed(
                references.ResetRoot.transform,
                ResetPivotName);
            DestroyDirectChildrenNamed(
                references.Placeholders.transform,
                FurniturePivotName);
            DestroyDirectChildrenNamed(
                references.FinishTarget.transform,
                FinishVisualName);

            foreach (PotionController potion in references.Potions)
            {
                DestroyDirectChildrenWithPrefix(
                    potion.transform,
                    PotionPivotPrefix);
            }
        }

        private static void StyleExistingRoom(
            SceneReferences references,
            VisualPassAssets assets)
        {
            StyleRenderer(
                references.Floor.GetComponent<Renderer>(),
                assets.FloorStone,
                true);
            StyleRenderer(
                references.WallLeft.GetComponent<Renderer>(),
                assets.DarkStone,
                true);
            StyleRenderer(
                references.WallRight.GetComponent<Renderer>(),
                assets.DarkStone,
                true);
            StyleRenderer(
                references.WallBack.GetComponent<Renderer>(),
                assets.DarkStone,
                true);
        }

        private static void BuildArchitecture(
            Transform parent,
            VisualPassAssets assets)
        {
            CreateCube(
                parent,
                "BackCornice",
                new Vector3(0f, 2.93f, 2.86f),
                new Vector3(3.82f, 0.16f, 0.18f),
                assets.StoneTrim,
                true);
            CreateCube(
                parent,
                "LeftCornice",
                new Vector3(-1.86f, 2.93f, 1f),
                new Vector3(0.18f, 0.16f, 3.82f),
                assets.StoneTrim,
                true);
            CreateCube(
                parent,
                "RightCornice",
                new Vector3(1.86f, 2.93f, 1f),
                new Vector3(0.18f, 0.16f, 3.82f),
                assets.StoneTrim,
                true);

            Vector3[] columnPositions =
            {
                new Vector3(-1.84f, 1.5f, 2.84f),
                new Vector3(1.84f, 1.5f, 2.84f)
            };

            for (int index = 0; index < columnPositions.Length; index++)
            {
                CreateCube(
                    parent,
                    "ObservatoryColumn_" + (index + 1),
                    columnPositions[index],
                    new Vector3(0.22f, 2.82f, 0.22f),
                    assets.StoneTrim,
                    true);
            }

            CreateCube(
                parent,
                "RearCeilingBeam",
                new Vector3(0f, 2.78f, 2.15f),
                new Vector3(3.52f, 0.13f, 0.17f),
                assets.WarmWood,
                true);

            CreateCube(
                parent,
                "BackWallBaseTrim",
                new Vector3(0f, 0.18f, 2.84f),
                new Vector3(3.76f, 0.16f, 0.16f),
                assets.StoneTrim,
                true);
            CreateCube(
                parent,
                "LeftWallBaseTrim",
                new Vector3(-1.84f, 0.18f, 1f),
                new Vector3(0.16f, 0.16f, 3.76f),
                assets.StoneTrim,
                true);
            CreateCube(
                parent,
                "RightWallBaseTrim",
                new Vector3(1.84f, 0.18f, 1f),
                new Vector3(0.16f, 0.16f, 3.76f),
                assets.StoneTrim,
                true);

            BuildCofferedCeiling(parent, assets);

            CreateMeshVisual(
                parent,
                "FloorAlchemyRing",
                assets.RuneRingMesh,
                assets.CyanEmission,
                new Vector3(0.2f, 0.108f, 1.05f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.62f, 0.62f, 0.62f),
                false,
                false);
        }

        private static void BuildCofferedCeiling(
            Transform parent,
            VisualPassAssets assets)
        {
            GameObject ceiling = CreateCube(
                parent,
                "CofferedCeilingStone",
                new Vector3(0f, 3.04f, 1f),
                new Vector3(4.08f, 0.1f, 4.08f),
                assets.DarkStone,
                true);
            DisableShadowsRecursively(ceiling);

            float[] crossBeamPositions =
            {
                -0.45f,
                0.45f,
                1.55f,
                2.45f
            };
            for (int index = 0;
                 index < crossBeamPositions.Length;
                 index++)
            {
                GameObject beam = CreateCube(
                    parent,
                    "CeilingCrossBeam_" + (index + 1),
                    new Vector3(
                        0f,
                        2.925f,
                        crossBeamPositions[index]),
                    new Vector3(3.82f, 0.13f, 0.12f),
                    assets.WarmWood,
                    true);
                DisableShadowsRecursively(beam);
            }

            float[] lengthBeamPositions = { -1.25f, 1.25f };
            for (int index = 0;
                 index < lengthBeamPositions.Length;
                 index++)
            {
                GameObject beam = CreateCube(
                    parent,
                    "CeilingLengthBeam_" + (index + 1),
                    new Vector3(
                        lengthBeamPositions[index],
                        2.925f,
                        1f),
                    new Vector3(0.12f, 0.13f, 3.82f),
                    assets.WarmWood,
                    true);
                DisableShadowsRecursively(beam);
            }

            CreateMeshVisual(
                parent,
                "CeilingAstralRune",
                assets.RuneRingMesh,
                assets.VioletEmission,
                new Vector3(0f, 2.984f, 1f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.34f, 0.34f, 0.34f),
                true,
                false);
        }

        private static InteractiveVisuals IntegrateAlexVisuals(
            SceneReferences references,
            VisualPassAssets assets)
        {
            Renderer cauldronPlaceholder =
                references.Cauldron.GetComponent<Renderer>();
            if (cauldronPlaceholder != null)
            {
                cauldronPlaceholder.enabled = false;
            }

            GameObject cauldronVisual = InstantiateWrapper(
                assets.CauldronWrapperPrefab,
                references.Cauldron.transform,
                CauldronPivotName);
            cauldronVisual.transform.localPosition = Vector3.zero;
            cauldronVisual.transform.localRotation = Quaternion.identity;
            SetWorldScale(cauldronVisual.transform, Vector3.one);
            PlaceBottomAndCenter(
                cauldronVisual,
                references.Cauldron.transform.position,
                references.Cauldron.transform.position.y);

            Renderer liquidRenderer =
                references.LiquidVisual.GetComponent<Renderer>();
            StyleRenderer(liquidRenderer, assets.Liquid, false);

            Vector3 cauldronPosition =
                references.Cauldron.transform.position;

            GameObject cauldronRune = CreateMeshVisual(
                references.Cauldron.transform,
                CauldronRuneName,
                assets.RuneRingMesh,
                assets.CyanEmission,
                cauldronPosition + new Vector3(0f, 0.18f, 0f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.215f, 0.215f, 0.215f),
                false,
                false);
            Renderer cauldronRuneRenderer =
                cauldronRune.GetComponent<Renderer>();
            if (cauldronRuneRenderer != null)
            {
                // The rune is an event accent, not a permanent cyan rim.
                // VisualFeedbackController enables it only during a pulse.
                cauldronRuneRenderer.enabled = false;
            }

            DisableRenderersUnderNamedVisual(
                references.WandRoot.transform);
            GameObject wandVisual = InstantiateWrapper(
                assets.WandWrapperPrefab,
                references.WandRoot.transform,
                WandPivotName);
            CenterOnWorldPoint(
                wandVisual,
                references.WandRoot.transform.position);

            foreach (PotionController potion in references.Potions)
            {
                DisableRenderersUnderNamedVisual(potion.transform);

                GameObject potionVisual = InstantiateWrapper(
                    assets.PotionWrapperPrefab,
                    potion.transform,
                    PotionPivotPrefix + potion.name);
                ApplyPotionMaterials(potionVisual, potion, assets);
                CenterOnWorldPoint(
                    potionVisual,
                    potion.transform.position);
            }

            Renderer shelfRenderer =
                references.PotionShelf.GetComponent<Renderer>();
            Renderer tableRenderer =
                references.WandTable.GetComponent<Renderer>();
            if (shelfRenderer != null)
            {
                shelfRenderer.enabled = false;
            }
            if (tableRenderer != null)
            {
                tableRenderer.enabled = false;
            }

            GameObject furnitureVisual = InstantiateWrapper(
                assets.FurnitureWrapperPrefab,
                references.Placeholders.transform,
                FurniturePivotName);
            furnitureVisual.transform.localScale =
                new Vector3(1.6f, 1.7f, 1.05f);
            PlaceBottomAndCenter(
                furnitureVisual,
                new Vector3(0.05f, 0f, 1f),
                0.1f);
            // The Alex combination mesh is a single renderer: its tall open
            // rails cannot be hidden individually and protrude through the
            // collider-matched project worktops. Retain the wrapper and source
            // reference for replacement, but hide only its renderers. The
            // project-owned stepped workbench below supplies the visible table.
            foreach (Renderer renderer in furnitureVisual
                .GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            // The protected reset prefab consists of its interaction volume
            // and status canvas and intentionally has no placeholder named
            // Visual. If a later prefab revision adds one, hide only its
            // renderers while leaving every collider and UI component intact.
            if (FindDirectChild(references.ResetRoot.transform, "Visual") != null)
            {
                DisableRenderersUnderNamedVisual(
                    references.ResetRoot.transform);
            }
            GameObject resetVisual = InstantiateWrapper(
                assets.ResetWrapperPrefab,
                references.ResetRoot.transform,
                ResetPivotName);

            Collider interactionVolume = references.ResetRoot
                .GetComponentsInChildren<Collider>(true)
                .FirstOrDefault(item =>
                    item.gameObject.name == "InteractionVolume");
            Vector3 resetCenter = interactionVolume != null
                ? interactionVolume.bounds.center
                : references.ResetRoot.transform.position;
            CenterOnWorldPoint(resetVisual, resetCenter);

            return new InteractiveVisuals
            {
                CauldronRuneRenderer = cauldronRuneRenderer
            };
        }

        private static void BuildAstralPortal(
            Transform parent,
            VisualPassAssets assets)
        {
            GameObject portalBackdrop = CreatePrimitiveVisualLocal(
                parent,
                "AstralThresholdBackdrop",
                PrimitiveType.Quad,
                new Vector3(0f, 1.55f, -0.975f),
                Quaternion.identity,
                new Vector3(4.08f, 2.95f, 1f),
                assets.PortalCore,
                true);
            Renderer portalBackdropRenderer =
                portalBackdrop.GetComponent<Renderer>();
            if (portalBackdropRenderer != null)
            {
                portalBackdropRenderer.shadowCastingMode =
                    ShadowCastingMode.Off;
                portalBackdropRenderer.receiveShadows = false;
            }

            GameObject portal = InstantiateWrapper(
                assets.PortalPrefab,
                parent,
                "WC_AstralPortal");
            portal.transform.position =
                new Vector3(0f, 1.55f, -0.925f);
            portal.transform.rotation = Quaternion.identity;
            portal.transform.localScale = new Vector3(2.68f, 1.98f, 1f);
            StripPhysics(portal);

            CreateAstralThresholdStars(parent, assets);
        }

        private static void CreateAstralThresholdStars(
            Transform parent,
            VisualPassAssets assets)
        {
            Vector3[] positions =
            {
                new Vector3(-1.78f, 0.31f, -0.94f),
                new Vector3(-1.51f, 0.83f, -0.94f),
                new Vector3(-1.82f, 1.48f, -0.94f),
                new Vector3(-1.58f, 2.12f, -0.94f),
                new Vector3(-1.76f, 2.71f, -0.94f),
                new Vector3(-1.05f, 2.54f, -0.94f),
                new Vector3(-0.71f, 0.55f, -0.94f),
                new Vector3(-0.34f, 2.79f, -0.94f),
                new Vector3(0.28f, 0.37f, -0.94f),
                new Vector3(0.67f, 2.66f, -0.94f),
                new Vector3(1.04f, 0.72f, -0.94f),
                new Vector3(1.43f, 2.38f, -0.94f),
                new Vector3(1.78f, 1.78f, -0.94f),
                new Vector3(1.62f, 1.08f, -0.94f),
                new Vector3(1.82f, 0.34f, -0.94f),
                new Vector3(0.05f, 2.18f, -0.94f)
            };

            for (int index = 0; index < positions.Length; index++)
            {
                Material material = index % 5 == 0
                    ? assets.OrangeEmission
                    : index % 2 == 0
                        ? assets.CyanEmission
                        : assets.VioletEmission;
                float size = 0.018f + (index % 4) * 0.006f;
                CreateMeshVisual(
                    parent,
                    "AstralThresholdStar_" + (index + 1),
                    assets.DiscMesh,
                    material,
                    positions[index],
                    Quaternion.identity,
                    new Vector3(size, size, size),
                    true,
                    false);
            }
        }

        private static void BuildDecoration(
            Transform parent,
            VisualPassAssets assets)
        {
            CreateCube(
                parent,
                "BackDisplayShelf",
                new Vector3(1.05f, 1.25f, 2.76f),
                new Vector3(1.38f, 0.07f, 0.27f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "BackDisplayShelfBraceLeft",
                new Vector3(0.55f, 1.12f, 2.82f),
                new Vector3(0.08f, 0.24f, 0.12f),
                assets.Gold,
                true);
            CreateCube(
                parent,
                "BackDisplayShelfBraceRight",
                new Vector3(1.55f, 1.12f, 2.82f),
                new Vector3(0.08f, 0.24f, 0.12f),
                assets.Gold,
                true);

            BuildReadableFurnitureSilhouette(parent, assets);
            BuildPotionSampleTray(parent, assets);

            CreateBook(
                BookNecromancyPath,
                parent,
                "NecromancyBook",
                new Vector3(0.72f, 1.29f, 2.72f),
                new Vector3(0f, 180f, 8f));
            CreateBook(
                BookFirePath,
                parent,
                "FireBook",
                new Vector3(0.93f, 1.29f, 2.72f),
                new Vector3(0f, 174f, -5f));
            CreateBook(
                BookIcePath,
                parent,
                "IceBook",
                new Vector3(1.14f, 1.29f, 2.72f),
                new Vector3(0f, 186f, 4f));

            CreateDecorativePotion(
                assets.PotionWrapperPrefab,
                parent,
                "DecorativeBottleBlue",
                assets.GlassBlue,
                assets,
                new Vector3(1.38f, 1.29f, 2.72f));
            CreateDecorativePotion(
                assets.PotionWrapperPrefab,
                parent,
                "DecorativeBottleViolet",
                assets.GlassViolet,
                assets,
                new Vector3(1.57f, 1.29f, 2.72f));

            BuildSideShelfVignettes(parent, assets);

            CreateCube(
                parent,
                "BlueCrystalPedestal",
                new Vector3(-1.58f, 0.34f, 2.42f),
                new Vector3(0.28f, 0.48f, 0.28f),
                assets.DarkStone,
                true);
            CreateCube(
                parent,
                "BlueCrystalPedestalCap",
                new Vector3(-1.58f, 0.59f, 2.42f),
                new Vector3(0.34f, 0.05f, 0.34f),
                assets.Gold,
                true);

            GameObject blueGem = InstantiateDecorativePrefab(
                BlueGemPath,
                parent,
                "BlueAstralGem");
            FitToMaximumDimension(blueGem, 0.32f);
            PlaceBottomAndCenter(
                blueGem,
                new Vector3(-1.58f, 0f, 2.42f),
                0.62f);
            SetStaticRecursively(blueGem);

            GameObject goldCrystal = InstantiateDecorativePrefab(
                GoldCrystalPath,
                parent,
                "GoldenShelfCrystal");
            FitToMaximumDimension(goldCrystal, 0.18f);
            PlaceBottomAndCenter(
                goldCrystal,
                new Vector3(1.72f, 0f, 2.72f),
                1.29f);
            SetStaticRecursively(goldCrystal);

            GameObject chest = InstantiateDecorativePrefab(
                ChestPath,
                parent,
                "AstronomerChest");
            AssignMaterialSequence(
                chest,
                assets.WarmWood,
                assets.Gold);
            FitToMaximumDimension(chest, 0.56f);
            PlaceBottomAndCenter(
                chest,
                new Vector3(1.55f, 0f, 2.42f),
                0.1f);
            SetStaticRecursively(chest);

            CreateWallTorch(
                parent,
                "WarmTorchLeft",
                new Vector3(-0.75f, 1.95f, 2.78f),
                assets);
            CreateWallTorch(
                parent,
                "WarmTorchRight",
                new Vector3(0.75f, 1.95f, 2.78f),
                assets);

            CreateWallCandleSconce(
                parent,
                "LeftFrontCandle",
                new Vector3(-1.81f, 1.62f, -0.18f),
                Quaternion.Euler(0f, 90f, 0f),
                assets);
            CreateWallCandleSconce(
                parent,
                "LeftRearCandle",
                new Vector3(-1.81f, 1.62f, 2.18f),
                Quaternion.Euler(0f, 90f, 0f),
                assets);
            CreateWallCandleSconce(
                parent,
                "RightFrontCandle",
                new Vector3(1.81f, 1.62f, -0.18f),
                Quaternion.Euler(0f, -90f, 0f),
                assets);
            CreateWallCandleSconce(
                parent,
                "RightRearCandle",
                new Vector3(1.81f, 1.62f, 2.18f),
                Quaternion.Euler(0f, -90f, 0f),
                assets);
        }

        private static void BuildReadableFurnitureSilhouette(
            Transform parent,
            VisualPassAssets assets)
        {
            CreateCube(
                parent,
                "WandWorktableTop",
                new Vector3(0.84f, 0.8f, 1.01f),
                new Vector3(0.76f, 0.09f, 0.62f),
                assets.WarmWood,
                true);

            CreateCube(
                parent,
                "CentralAlchemyWorktopLeft",
                new Vector3(-0.38f, 0.8f, 1.01f),
                new Vector3(0.64f, 0.09f, 0.62f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "ExpandedPotionWorktopFront",
                new Vector3(-0.36f, 0.8f, 0.64f),
                new Vector3(0.92f, 0.09f, 0.24f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "ExpandedPotionWorktopFrontTrim",
                new Vector3(-0.36f, 0.812f, 0.512f),
                new Vector3(0.94f, 0.025f, 0.025f),
                assets.Gold,
                true);
            CreateCube(
                parent,
                "CentralAlchemyWorktopBridge",
                new Vector3(0.2f, 0.8f, 0.745f),
                new Vector3(0.52f, 0.09f, 0.09f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "ClosedWorkbenchApron",
                new Vector3(0.05f, 0.67f, 0.7f),
                new Vector3(2.36f, 0.22f, 0.06f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "ClosedWorkbenchApronTrim",
                new Vector3(0.05f, 0.792f, 0.665f),
                new Vector3(2.4f, 0.025f, 0.025f),
                assets.Gold,
                true);

            Vector3[] tableLegs =
            {
                new Vector3(0.58f, 0.45f, 0.76f),
                new Vector3(1.12f, 0.45f, 0.76f),
                new Vector3(0.58f, 0.45f, 1.14f),
                new Vector3(1.12f, 0.45f, 1.14f)
            };
            for (int index = 0; index < tableLegs.Length; index++)
            {
                CreateCube(
                    parent,
                    "WandWorktableLeg_" + (index + 1),
                    tableLegs[index],
                    new Vector3(0.075f, 0.66f, 0.075f),
                    assets.WarmWood,
                    true);
            }

            CreateCube(
                parent,
                "PotionShelfBoard",
                new Vector3(-0.65f, 1f, 1f),
                new Vector3(1.06f, 0.09f, 0.42f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "PotionShelfBracketLeft",
                new Vector3(-0.95f, 0.86f, 1.12f),
                new Vector3(0.065f, 0.3f, 0.065f),
                assets.Gold,
                true);
            CreateCube(
                parent,
                "PotionShelfBracketRight",
                new Vector3(-0.35f, 0.86f, 1.12f),
                new Vector3(0.065f, 0.3f, 0.065f),
                assets.Gold,
                true);

            CreateCube(
                parent,
                "CauldronHearthBase",
                new Vector3(0.2f, 0.51f, 1.05f),
                new Vector3(0.46f, 0.34f, 0.46f),
                assets.DarkStone,
                true);
            CreateCube(
                parent,
                "CauldronHearthGoldBand",
                new Vector3(0.2f, 0.69f, 1.05f),
                new Vector3(0.5f, 0.025f, 0.5f),
                assets.Gold,
                true);
        }

        private static void BuildPotionSampleTray(
            Transform parent,
            VisualPassAssets assets)
        {
            CreateCube(
                parent,
                "PotionSamplerTableExtension",
                new Vector3(-1.49f, 1f, 1f),
                new Vector3(0.62f, 0.09f, 0.42f),
                assets.WarmWood,
                true);

            Vector3[] extensionLegs =
            {
                new Vector3(-1.72f, 0.53f, 0.86f),
                new Vector3(-1.72f, 0.53f, 1.14f),
                new Vector3(-1.27f, 0.53f, 0.86f),
                new Vector3(-1.27f, 0.53f, 1.14f)
            };
            for (int index = 0; index < extensionLegs.Length; index++)
            {
                CreateCube(
                    parent,
                    "PotionSamplerTableLeg_" + (index + 1),
                    extensionLegs[index],
                    new Vector3(0.06f, 0.85f, 0.06f),
                    assets.WarmWood,
                    true);
            }

            CreateCube(
                parent,
                "PotionSamplerTableFrontTrim",
                new Vector3(-1.49f, 1.018f, 0.785f),
                new Vector3(0.63f, 0.025f, 0.025f),
                assets.Gold,
                true);

            Vector3[] highPotionDocks =
            {
                new Vector3(-1.68f, 1.047f, 1.02f),
                new Vector3(-1.49f, 1.047f, 1.02f),
                new Vector3(-1.3f, 1.047f, 1.02f)
            };
            Vector3[] lowPotionDocks =
            {
                new Vector3(-0.58f, 0.847f, 0.64f),
                new Vector3(-0.3f, 0.847f, 0.64f)
            };
            Vector3[] allPotionDocks = highPotionDocks
                .Concat(lowPotionDocks)
                .ToArray();
            for (int index = 0; index < allPotionDocks.Length; index++)
            {
                CreateMeshVisual(
                    parent,
                    "PotionDock_" + (index + 1),
                    assets.DiscMesh,
                    assets.BlackIron,
                    allPotionDocks[index],
                    Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(0.105f, 0.105f, 0.105f),
                    true,
                    false);
                CreateMeshVisual(
                    parent,
                    "PotionDockGoldRing_" + (index + 1),
                    assets.RingMesh,
                    assets.Gold,
                    allPotionDocks[index] + new Vector3(0f, 0.001f, 0f),
                    Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(0.105f, 0.105f, 0.105f),
                    true,
                    false);
            }
        }

        private static void CreatePotionRatioMarker(
            Transform parent,
            string name,
            Vector3 center,
            float lifeWidth,
            float poisonWidth,
            VisualPassAssets assets)
        {
            const float gap = 0.008f;
            float totalWidth = lifeWidth + poisonWidth + gap;
            CreateCube(
                parent,
                name + "_Life",
                center + new Vector3(
                    -totalWidth * 0.5f + lifeWidth * 0.5f,
                    0f,
                    0f),
                new Vector3(lifeWidth, 0.006f, 0.028f),
                assets.OrangeEmission,
                true);
            CreateCube(
                parent,
                name + "_Poison",
                center + new Vector3(
                    totalWidth * 0.5f - poisonWidth * 0.5f,
                    0f,
                    0f),
                new Vector3(poisonWidth, 0.006f, 0.028f),
                assets.VioletEmission,
                true);
        }

        private static void BuildSideShelfVignettes(
            Transform parent,
            VisualPassAssets assets)
        {
            CreateCube(
                parent,
                "LeftWallCurioShelf",
                new Vector3(-1.72f, 1.18f, 0.72f),
                new Vector3(0.28f, 0.06f, 0.88f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "LeftWallCurioBrace",
                new Vector3(-1.78f, 1.04f, 0.72f),
                new Vector3(0.12f, 0.25f, 0.08f),
                assets.Gold,
                true);
            CreateBook(
                BookIcePath,
                parent,
                "LeftWallIceBook",
                new Vector3(-1.55f, 1.22f, 0.56f),
                new Vector3(0f, 90f, 5f));
            CreateBook(
                BookNecromancyPath,
                parent,
                "LeftWallAstralBook",
                new Vector3(-1.55f, 1.22f, 0.82f),
                new Vector3(0f, 92f, -4f));

            CreateCube(
                parent,
                "RightWallPotionShelf",
                new Vector3(1.72f, 1.38f, 2f),
                new Vector3(0.28f, 0.06f, 0.82f),
                assets.WarmWood,
                true);
            CreateCube(
                parent,
                "RightWallPotionBrace",
                new Vector3(1.78f, 1.24f, 2f),
                new Vector3(0.12f, 0.25f, 0.08f),
                assets.Gold,
                true);
            CreateDecorativePotion(
                assets.PotionWrapperPrefab,
                parent,
                "RightWallBottleBlue",
                assets.GlassBlue,
                assets,
                new Vector3(1.55f, 1.42f, 1.82f));
            CreateDecorativePotion(
                assets.PotionWrapperPrefab,
                parent,
                "RightWallBottleViolet",
                assets.GlassViolet,
                assets,
                new Vector3(1.55f, 1.42f, 2.15f));

            CreateMeshVisual(
                parent,
                "LeftWallRunePlaque",
                assets.RuneRingMesh,
                assets.CyanEmission,
                new Vector3(-1.82f, 2.28f, 0.72f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(0.24f, 0.24f, 0.24f),
                true,
                false);
            CreateMeshVisual(
                parent,
                "RightWallRunePlaque",
                assets.RuneRingMesh,
                assets.VioletEmission,
                new Vector3(1.82f, 2.3f, 2f),
                Quaternion.Euler(0f, -90f, 0f),
                new Vector3(0.24f, 0.24f, 0.24f),
                true,
                false);
        }

        private static LightingVisuals BuildLightingAndPost(
            Transform parent,
            SceneReferences references,
            VisualPassAssets assets)
        {
            Light existingDirectional = FindInScene<Light>(
                    references.Environment.scene)
                .FirstOrDefault(light =>
                    light.type == LightType.Directional);

            if (existingDirectional != null)
            {
                existingDirectional.name = "WarmAstralDirectional";
                existingDirectional.color =
                    new Color(1f, 0.54f, 0.3f, 1f);
                existingDirectional.intensity = 0.82f;
                existingDirectional.shadows = LightShadows.None;
                existingDirectional.bounceIntensity = 0f;
            }

            Light warmKey = CreatePointLight(
                parent,
                "WarmAlchemyKey",
                new Vector3(0.05f, 2.12f, 0.78f),
                new Color(1f, 0.62f, 0.38f, 1f),
                48f,
                3.6f);

            Light cauldronAccent = CreatePointLight(
                parent,
                "CauldronCyanAccent",
                references.Cauldron.transform.position +
                    new Vector3(0f, 0.385f, 0f),
                new Color(0.08f, 0.72f, 1f, 1f),
                0f,
                1.25f);

            GameObject volumeObject = new GameObject(
                "GlobalVisualVolume");
            volumeObject.transform.SetParent(parent, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = assets.GlobalVolumeProfile;

            Camera mainCamera = FindInScene<Camera>(
                    references.Environment.scene)
                .FirstOrDefault(camera =>
                    camera.CompareTag("MainCamera")) ??
                FindInScene<Camera>(references.Environment.scene)
                    .FirstOrDefault();

            if (mainCamera != null)
            {
                UniversalAdditionalCameraData cameraData =
                    mainCamera.GetComponent<
                        UniversalAdditionalCameraData>();
                if (cameraData == null)
                {
                    cameraData = mainCamera.gameObject.AddComponent<
                        UniversalAdditionalCameraData>();
                }

                cameraData.renderPostProcessing = true;
            }

            RenderSettings.skybox = assets.SkyboxMaterial;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor =
                new Color(0.16f, 0.17f, 0.23f, 1f);
            RenderSettings.ambientEquatorColor =
                new Color(0.16f, 0.08f, 0.07f, 1f);
            RenderSettings.ambientGroundColor =
                new Color(0.05f, 0.035f, 0.04f, 1f);
            RenderSettings.ambientIntensity = 0.92f;
            RenderSettings.reflectionIntensity = 0.2f;
            RenderSettings.fog = false;

            return new LightingVisuals
            {
                WarmKey = warmKey,
                CauldronAccent = cauldronAccent
            };
        }

        private static void BuildVisualFeedback(
            Transform parent,
            SceneReferences references,
            Renderer runeRenderer,
            Light cauldronAccent,
            VisualPassAssets assets)
        {
            GameObject controllerObject = new GameObject(
                "WC_VisualFeedbackController");
            controllerObject.transform.SetParent(parent, false);

            Vector3 cauldronPosition =
                references.Cauldron.transform.position;

            ParticleSystem accepted = CreateBurstParticles(
                controllerObject.transform,
                "AcceptedPotionSpark",
                cauldronPosition + new Vector3(0f, 0.305f, 0f),
                assets.PortalParticle,
                16,
                0.45f,
                0.55f,
                0.055f);
            ParticleSystem rejected = CreateBurstParticles(
                controllerObject.transform,
                "RejectedPotionSmoke",
                cauldronPosition + new Vector3(0f, 0.295f, 0f),
                assets.PortalParticle,
                12,
                0.65f,
                0.28f,
                0.085f);
            ParticleSystem success = CreateBurstParticles(
                controllerObject.transform,
                "AttemptSuccessBurst",
                cauldronPosition + new Vector3(0f, 0.475f, 0f),
                assets.PortalParticle,
                24,
                1.05f,
                0.75f,
                0.065f);
            ParticleSystem reset = CreateBurstParticles(
                controllerObject.transform,
                "SessionResetPulse",
                cauldronPosition + new Vector3(0f, 0.235f, 0f),
                assets.PortalParticle,
                12,
                0.55f,
                0.38f,
                0.05f);

            VisualFeedbackController controller =
                controllerObject.AddComponent<VisualFeedbackController>();
            SerializedObject serialized = new SerializedObject(controller);
            SetObjectReference(
                serialized,
                "_cauldronIntake",
                references.CauldronIntake);
            SetObjectReference(
                serialized,
                "_gameSession",
                references.GameSession);
            SetObjectReference(serialized, "_acceptedParticles", accepted);
            SetObjectReference(serialized, "_rejectedParticles", rejected);
            SetObjectReference(serialized, "_successParticles", success);
            SetObjectReference(serialized, "_resetParticles", reset);
            SetObjectReference(
                serialized,
                "_cauldronAccentLight",
                cauldronAccent);
            SetObjectReference(serialized, "_runeRenderer", runeRenderer);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void StyleWorldSpaceUi(
            SceneReferences references,
            VisualPassAssets assets)
        {
            GameObject[] canvases =
            {
                FindOptionalByName(
                    references.Environment.scene,
                    "CauldronStatusCanvas"),
                FindOptionalByName(
                    references.Environment.scene,
                    "ResultCanvas"),
                FindOptionalByName(
                    references.Environment.scene,
                    "PotionInspectionCanvas"),
                references.ResetRoot
            };

            foreach (GameObject canvasRoot in canvases)
            {
                if (canvasRoot == null)
                {
                    continue;
                }

                foreach (Image image in canvasRoot
                    .GetComponentsInChildren<Image>(true))
                {
                    image.color = new Color(
                        0.035f,
                        0.025f,
                        0.075f,
                        0.9f);
                }

                foreach (TMP_Text text in canvasRoot
                    .GetComponentsInChildren<TMP_Text>(true))
                {
                    text.color = GetUiTextColor(text.gameObject.name);
                }
            }
        }

        private static void StyleFinishTarget(
            SceneReferences references,
            VisualPassAssets assets)
        {
            Renderer targetRenderer =
                references.FinishTarget.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                // Keep the gameplay trigger untouched, but hide the large
                // primitive sphere that otherwise dominates the VR view.
                targetRenderer.enabled = false;
            }

            CreateMeshVisual(
                references.FinishTarget.transform,
                FinishVisualName,
                assets.RuneRingMesh,
                assets.CyanEmission,
                references.FinishTarget.transform.position,
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.13f, 0.13f, 0.13f),
                false,
                false);
        }

        private static void CreateWallTorch(
            Transform parent,
            string name,
            Vector3 position,
            VisualPassAssets assets)
        {
            GameObject holder = InstantiateDecorativePrefab(
                LanternPath,
                parent,
                name + "Holder");
            AssignMaterialSequence(holder, assets.BlackIron, assets.Gold);
            FitToMaximumDimension(holder, 0.28f);
            CenterOnWorldPoint(holder, position);
            SetStaticRecursively(holder);

            CreateTorchParticles(
                parent,
                name + "Flame",
                position + new Vector3(0f, 0.16f, -0.045f),
                assets.FireParticle);
        }

        private static void CreateWallCandleSconce(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            VisualPassAssets assets)
        {
            GameObject rootObject = new GameObject(name);
            rootObject.transform.SetParent(parent, true);
            rootObject.transform.position = position;
            rootObject.transform.rotation = rotation;

            CreatePrimitiveVisualLocal(
                rootObject.transform,
                "Backplate",
                PrimitiveType.Cube,
                Vector3.zero,
                Quaternion.identity,
                new Vector3(0.13f, 0.22f, 0.035f),
                assets.BlackIron,
                true);
            CreatePrimitiveVisualLocal(
                rootObject.transform,
                "GoldenArm",
                PrimitiveType.Cube,
                new Vector3(0f, -0.04f, 0.11f),
                Quaternion.identity,
                new Vector3(0.035f, 0.035f, 0.22f),
                assets.Gold,
                true);
            CreatePrimitiveVisualLocal(
                rootObject.transform,
                "DripTray",
                PrimitiveType.Cylinder,
                new Vector3(0f, -0.025f, 0.21f),
                Quaternion.identity,
                new Vector3(0.08f, 0.012f, 0.08f),
                assets.BlackIron,
                true);
            CreatePrimitiveVisualLocal(
                rootObject.transform,
                "CandleWax",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.085f, 0.21f),
                Quaternion.identity,
                new Vector3(0.043f, 0.105f, 0.043f),
                assets.Label,
                true);
            CreatePrimitiveVisualLocal(
                rootObject.transform,
                "Wick",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.2f, 0.21f),
                Quaternion.identity,
                new Vector3(0.008f, 0.018f, 0.008f),
                assets.BlackIron,
                true);

            CreateTorchParticles(
                rootObject.transform,
                name + "Flame",
                rootObject.transform.TransformPoint(
                    new Vector3(0f, 0.235f, 0.21f)),
                assets.FireParticle,
                4,
                2.5f,
                0.58f);
        }

        private static void CreateBook(
            string assetPath,
            Transform parent,
            string name,
            Vector3 bottomCenter,
            Vector3 eulerAngles)
        {
            GameObject book = InstantiateDecorativePrefab(
                assetPath,
                parent,
                name);
            book.transform.rotation = Quaternion.Euler(eulerAngles);
            FitToMaximumDimension(book, 0.2f);
            PlaceBottomAndCenter(book, bottomCenter, bottomCenter.y);
            SetStaticRecursively(book);
        }

        private static void CreateDecorativePotion(
            GameObject prefab,
            Transform parent,
            string name,
            Material glassMaterial,
            VisualPassAssets assets,
            Vector3 bottomCenter)
        {
            GameObject bottle = InstantiateWrapper(prefab, parent, name);
            AssignMaterialSequence(
                bottle,
                glassMaterial,
                assets.Cork,
                assets.Label);
            FitToMaximumDimension(bottle, 0.22f);
            PlaceBottomAndCenter(bottle, bottomCenter, bottomCenter.y);
            SetStaticRecursively(bottle);
        }

        private static void CreateDecorativePotionVariant(
            GameObject prefab,
            Transform parent,
            string name,
            Material glassMaterial,
            Material labelMaterial,
            VisualPassAssets assets,
            Vector3 bottomCenter)
        {
            GameObject bottle = InstantiateWrapper(prefab, parent, name);
            AssignMaterialSequence(
                bottle,
                glassMaterial,
                assets.Cork,
                labelMaterial);
            FitToMaximumDimension(bottle, 0.18f);
            PlaceBottomAndCenter(bottle, bottomCenter, bottomCenter.y);
            SetStaticRecursively(bottle);
        }

        private static ParticleSystem CreateBurstParticles(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Material material,
            int count,
            float lifetime,
            float speed,
            float size)
        {
            GameObject particleObject = new GameObject(name);
            particleObject.transform.SetParent(parent, true);
            particleObject.transform.position = worldPosition;

            ParticleSystem particles =
                particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = Mathf.Max(0.1f, lifetime);
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                lifetime * 0.7f,
                lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                speed * 0.55f,
                speed);
            main.startSize = new ParticleSystem.MinMaxCurve(
                size * 0.6f,
                size);
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)count)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.09f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = CreateFadeGradient();

            ParticleSystemRenderer particleRenderer =
                particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode =
                ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        private static ParticleSystem CreateTorchParticles(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Material material,
            int maximumParticles = 12,
            float emissionRate = 6f,
            float sizeMultiplier = 1f)
        {
            GameObject particleObject = new GameObject(name);
            particleObject.transform.SetParent(parent, true);
            particleObject.transform.position = worldPosition;

            ParticleSystem particles =
                particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.055f * sizeMultiplier,
                0.11f * sizeMultiplier);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.35f, 0.04f, 0.9f),
                new Color(1f, 0.78f, 0.18f, 0.95f));
            main.maxParticles = maximumParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 0.025f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = CreateFadeGradient();

            ParticleSystemRenderer particleRenderer =
                particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            return particles;
        }

        private static ParticleSystem.MinMaxGradient CreateFadeGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0f, 1f)
                });
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        private static Light CreatePointLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0f;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.renderMode = LightRenderMode.Auto;
            return light;
        }

        private static GameObject InstantiateDecorativePrefab(
            string assetPath,
            Transform parent,
            string name)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                assetPath);
            if (source == null)
            {
                throw new FileNotFoundException(
                    "Required decorative prefab is missing: " + assetPath);
            }

            GameObject instance = InstantiateWrapper(source, parent, name);
            StripPhysics(instance);
            foreach (Light light in instance.GetComponentsInChildren<Light>(true))
            {
                UnityEngine.Object.DestroyImmediate(light);
            }

            return instance;
        }

        private static GameObject InstantiateWrapper(
            GameObject prefab,
            Transform parent,
            string name)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "A required project-owned visual prefab is missing.");
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                parent) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate visual prefab " + prefab.name + ".");
            }

            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            StripPhysics(instance);
            return instance;
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool markStatic)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, true);
            cube.transform.position = position;
            cube.transform.rotation = Quaternion.identity;
            cube.transform.localScale = scale;

            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            StyleRenderer(cube.GetComponent<Renderer>(), material, true);
            if (markStatic)
            {
                SetStaticRecursively(cube);
            }

            return cube;
        }

        private static void DisableShadowsRecursively(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Renderer renderer in root
                .GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static GameObject CreatePrimitiveVisualLocal(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material,
            bool markStatic)
        {
            GameObject visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            StyleRenderer(visual.GetComponent<Renderer>(), material, true);
            if (markStatic)
            {
                SetStaticRecursively(visual);
            }

            return visual;
        }

        private static GameObject CreateMeshVisual(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 worldScale,
            bool markStatic,
            bool castShadows)
        {
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, true);
            visual.transform.position = worldPosition;
            visual.transform.rotation = worldRotation;
            SetWorldScale(visual.transform, worldScale);

            MeshFilter filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;

            if (markStatic)
            {
                SetStaticRecursively(visual);
            }

            return visual;
        }

        private static void StyleRenderer(
            Renderer renderer,
            Material material,
            bool castShadows)
        {
            if (renderer == null || material == null)
            {
                return;
            }

            Material[] existing = renderer.sharedMaterials;
            int count = Mathf.Max(1, existing.Length);
            Material[] replacement = new Material[count];
            for (int index = 0; index < replacement.Length; index++)
            {
                replacement[index] = material;
            }

            renderer.sharedMaterials = replacement;
            renderer.enabled = true;
            renderer.shadowCastingMode = castShadows
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;
        }

        private static void ApplyPotionMaterials(
            GameObject visual,
            PotionController potion,
            VisualPassAssets assets)
        {
            string stableId = potion.Definition != null
                ? potion.Definition.StableId
                : string.Empty;
            Material glass = assets.GlassBlue;

            if (stableId.IndexOf("red", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                glass = assets.GlassRed;
            }
            else if (stableId.IndexOf("green", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                glass = assets.GlassGreen;
            }
            else if (stableId.IndexOf("yellow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                glass = assets.GlassYellow;
            }
            else if (stableId.IndexOf("violet", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                glass = assets.GlassViolet;
            }
            else if (stableId.IndexOf("cyan", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                glass = assets.GlassCyan;
            }
            else if (stableId.IndexOf("orange", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                glass = assets.GlassOrange;
            }
            else if (stableId.IndexOf("magenta", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                glass = assets.GlassMagenta;
            }

            AssignMaterialSequence(visual, glass, assets.Cork, assets.Label);
        }

        private static void AssignMaterialSequence(
            GameObject root,
            params Material[] materials)
        {
            if (root == null || materials == null || materials.Length == 0)
            {
                return;
            }

            foreach (Renderer renderer in root
                .GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                int slotCount = Mathf.Max(
                    1,
                    renderer.sharedMaterials.Length);
                Material[] assigned = new Material[slotCount];
                for (int slot = 0; slot < slotCount; slot++)
                {
                    assigned[slot] = materials[
                        Mathf.Min(slot, materials.Length - 1)];
                }

                renderer.sharedMaterials = assigned;
            }
        }

        private static void DisableRenderersUnderNamedVisual(
            Transform gameplayRoot)
        {
            Transform visual = FindDirectChild(gameplayRoot, "Visual");
            if (visual == null)
            {
                throw new InvalidOperationException(
                    "Protected gameplay root " + gameplayRoot.name +
                    " has no Visual child. Build stopped to protect colliders.");
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }
        }

        private static void StripPhysics(GameObject root)
        {
            foreach (Joint joint in root.GetComponentsInChildren<Joint>(true))
            {
                UnityEngine.Object.DestroyImmediate(joint);
            }
            foreach (Rigidbody rigidbody in root
                .GetComponentsInChildren<Rigidbody>(true))
            {
                UnityEngine.Object.DestroyImmediate(rigidbody);
            }
            foreach (Collider collider in root
                .GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void FitToMaximumDimension(
            GameObject root,
            float targetMaximumDimension)
        {
            if (!TryGetRendererBounds(root, out Bounds bounds))
            {
                return;
            }

            float currentMaximum = Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z);
            if (currentMaximum <= 0.0001f)
            {
                return;
            }

            float scale = targetMaximumDimension / currentMaximum;
            root.transform.localScale *= scale;
        }

        private static void PlaceBottomAndCenter(
            GameObject root,
            Vector3 targetCenter,
            float targetBottomY)
        {
            if (!TryGetRendererBounds(root, out Bounds bounds))
            {
                root.transform.position = targetCenter;
                return;
            }

            Vector3 delta = new Vector3(
                targetCenter.x - bounds.center.x,
                targetBottomY - bounds.min.y,
                targetCenter.z - bounds.center.z);
            root.transform.position += delta;
        }

        private static void CenterOnWorldPoint(
            GameObject root,
            Vector3 worldPoint)
        {
            if (!TryGetRendererBounds(root, out Bounds bounds))
            {
                root.transform.position = worldPoint;
                return;
            }

            root.transform.position += worldPoint - bounds.center;
        }

        private static bool TryGetRendererBounds(
            GameObject root,
            out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    !(renderer is ParticleSystemRenderer) &&
                    renderer.enabled)
                .ToArray();

            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }

        private static void SetWorldScale(
            Transform transform,
            Vector3 desiredWorldScale)
        {
            transform.localScale = Vector3.one;
            Vector3 currentWorldScale = transform.lossyScale;
            transform.localScale = new Vector3(
                SafeDivide(desiredWorldScale.x, currentWorldScale.x),
                SafeDivide(desiredWorldScale.y, currentWorldScale.y),
                SafeDivide(desiredWorldScale.z, currentWorldScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.00001f
                ? value / divisor
                : value;
        }

        private static void SetStaticRecursively(GameObject root)
        {
            StaticEditorFlags flags =
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.ReflectionProbeStatic;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(
                    child.gameObject,
                    flags);
            }
        }

        private static Color GetUiTextColor(string objectName)
        {
            if (objectName.IndexOf("Health", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Outcome", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(1f, 0.72f, 0.28f, 1f);
            }

            if (objectName.IndexOf("Capacity", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(0.25f, 0.9f, 1f, 1f);
            }

            if (objectName.IndexOf("Name", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(0.73f, 0.55f, 1f, 1f);
            }

            return new Color(0.92f, 0.94f, 1f, 1f);
        }

        private static void SetObjectReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Serialized field not found: " + propertyName + ".");
            }

            property.objectReferenceValue = value;
        }

        private static Transform CreateGroup(
            Transform parent,
            string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject FindUniqueByName(
            Scene scene,
            string objectName)
        {
            GameObject[] matches = EnumerateSceneGameObjects(scene)
                .Where(item => item.name == objectName)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one scene object named " + objectName +
                    ", found " + matches.Length + ".");
            }

            return matches[0];
        }

        private static GameObject FindOptionalByName(
            Scene scene,
            string objectName)
        {
            return EnumerateSceneGameObjects(scene)
                .FirstOrDefault(item => item.name == objectName);
        }

        private static T FindUniqueInScene<T>(Scene scene)
            where T : Component
        {
            T[] matches = FindInScene<T>(scene);
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + typeof(T).Name +
                    " in the visual scene, found " + matches.Length + ".");
            }

            return matches[0];
        }

        private static T[] FindInScene<T>(Scene scene)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static IEnumerable<GameObject> EnumerateSceneGameObjects(
            Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject);
        }

        private static Transform FindDirectChild(
            Transform parent,
            string childName)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void DestroyDirectChildrenNamed(
            Transform parent,
            params string[] names)
        {
            HashSet<string> nameSet = new HashSet<string>(
                names,
                StringComparer.Ordinal);
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (nameSet.Contains(child.name))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void DestroyDirectChildrenWithPrefix(
            Transform parent,
            string prefix)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (child.name.StartsWith(
                    prefix,
                    StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private sealed class SceneReferences
        {
            public GameObject Environment;
            public GameObject Placeholders;
            public GameObject Floor;
            public GameObject WallLeft;
            public GameObject WallRight;
            public GameObject WallBack;
            public GameObject PotionShelf;
            public GameObject WandTable;
            public CauldronController Cauldron;
            public CauldronIntake CauldronIntake;
            public GameSessionController GameSession;
            public RoomResetCoordinator RoomReset;
            public WandActivator WandActivator;
            public ResetHoldControl ResetHoldControl;
            public CauldronLiquidDisplay LiquidDisplay;
            public PotionController[] Potions;
            public PhysicsResettable WandRoot;
            public GameObject ResetRoot;
            public GameObject FinishTarget;
            public GameObject LiquidVisual;
        }

        private sealed class InteractiveVisuals
        {
            public Renderer CauldronRuneRenderer;
        }

        private sealed class LightingVisuals
        {
            public Light WarmKey;
            public Light CauldronAccent;
        }
    }
}
