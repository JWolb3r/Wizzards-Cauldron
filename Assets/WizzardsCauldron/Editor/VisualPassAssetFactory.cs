using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using WizzardsCauldron.Presentation;

namespace WizzardsCauldron.EditorTools
{
    internal sealed class VisualPassAssets
    {
        internal Material DarkStone { get; }
        internal Material FloorStone { get; }
        internal Material StoneTrim { get; }
        internal Material WarmWood { get; }
        internal Material BlackIron { get; }
        internal Material Gold { get; }
        internal Material CyanEmission { get; }
        internal Material VioletEmission { get; }
        internal Material OrangeEmission { get; }
        internal Material PortalCore { get; }
        internal Material Liquid { get; }
        internal Material GlassRed { get; }
        internal Material GlassGreen { get; }
        internal Material GlassYellow { get; }
        internal Material GlassBlue { get; }
        internal Material GlassViolet { get; }
        internal Material GlassCyan { get; }
        internal Material GlassOrange { get; }
        internal Material GlassMagenta { get; }
        internal Material Cork { get; }
        internal Material Label { get; }
        internal Material FireParticle { get; }
        internal Material PortalParticle { get; }
        internal Material SkyboxMaterial { get; }
        internal VolumeProfile GlobalVolumeProfile { get; }
        internal Mesh DiscMesh { get; }
        internal Mesh RingMesh { get; }
        internal Mesh RuneRingMesh { get; }
        internal GameObject CauldronWrapperPrefab { get; }
        internal GameObject WandWrapperPrefab { get; }
        internal GameObject PotionWrapperPrefab { get; }
        internal GameObject FurnitureWrapperPrefab { get; }
        internal GameObject ResetWrapperPrefab { get; }
        internal GameObject PortalPrefab { get; }

        internal VisualPassAssets(
            Material darkStone,
            Material floorStone,
            Material stoneTrim,
            Material warmWood,
            Material blackIron,
            Material gold,
            Material cyanEmission,
            Material violetEmission,
            Material orangeEmission,
            Material portalCore,
            Material liquid,
            Material glassRed,
            Material glassGreen,
            Material glassYellow,
            Material glassBlue,
            Material glassViolet,
            Material glassCyan,
            Material glassOrange,
            Material glassMagenta,
            Material cork,
            Material label,
            Material fireParticle,
            Material portalParticle,
            Material skyboxMaterial,
            VolumeProfile globalVolumeProfile,
            Mesh discMesh,
            Mesh ringMesh,
            Mesh runeRingMesh,
            GameObject cauldronWrapperPrefab,
            GameObject wandWrapperPrefab,
            GameObject potionWrapperPrefab,
            GameObject furnitureWrapperPrefab,
            GameObject resetWrapperPrefab,
            GameObject portalPrefab)
        {
            DarkStone = darkStone;
            FloorStone = floorStone;
            StoneTrim = stoneTrim;
            WarmWood = warmWood;
            BlackIron = blackIron;
            Gold = gold;
            CyanEmission = cyanEmission;
            VioletEmission = violetEmission;
            OrangeEmission = orangeEmission;
            PortalCore = portalCore;
            Liquid = liquid;
            GlassRed = glassRed;
            GlassGreen = glassGreen;
            GlassYellow = glassYellow;
            GlassBlue = glassBlue;
            GlassViolet = glassViolet;
            GlassCyan = glassCyan;
            GlassOrange = glassOrange;
            GlassMagenta = glassMagenta;
            Cork = cork;
            Label = label;
            FireParticle = fireParticle;
            PortalParticle = portalParticle;
            SkyboxMaterial = skyboxMaterial;
            GlobalVolumeProfile = globalVolumeProfile;
            DiscMesh = discMesh;
            RingMesh = ringMesh;
            RuneRingMesh = runeRingMesh;
            CauldronWrapperPrefab = cauldronWrapperPrefab;
            WandWrapperPrefab = wandWrapperPrefab;
            PotionWrapperPrefab = potionWrapperPrefab;
            FurnitureWrapperPrefab = furnitureWrapperPrefab;
            ResetWrapperPrefab = resetWrapperPrefab;
            PortalPrefab = portalPrefab;
        }
    }

    internal static class VisualPassAssetFactory
    {
        private const string ProjectRoot = "Assets/WizzardsCauldron";
        private const string MaterialsFolder = ProjectRoot + "/Art/Materials";
        private const string GeneratedModelsFolder = ProjectRoot + "/Art/Models/Generated";
        private const string SettingsFolder = ProjectRoot + "/Art/Settings";
        private const string VisualPrefabsFolder = ProjectRoot + "/Prefabs/Visual";
        private const string EnvironmentPrefabsFolder = ProjectRoot + "/Prefabs/Environment";
        private const string AlexModelsFolder = ProjectRoot + "/Art/Models/Alex";

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
        private const string SkyboxShaderName = "Skybox/Panoramic";

        private const string CastleBrickTexturePath =
            "Assets/StylizedDarkCastle/Textures/tex_brick.png";
        private const string CastleBrickNormalPath =
            "Assets/StylizedDarkCastle/Textures/tex_brick_normal.png";
        private const string CastleFloorTexturePath =
            "Assets/StylizedDarkCastle/Textures/tex_floorTile.png";
        private const string CastleFloorNormalPath =
            "Assets/StylizedDarkCastle/Textures/tex_floorTile_normal.png";
        private const string CastleWoodTexturePath =
            "Assets/StylizedDarkCastle/Textures/tex_wood.png";
        private const string CastleWoodNormalPath =
            "Assets/StylizedDarkCastle/Textures/tex_wood_normal.png";
        private const string FireGlowTexturePath =
            "Assets/PolyOne/Fire Effects/Texture/glow_v1.psd";
        private const string PortalSmokeTexturePath =
            "Assets/Eric VFX Studio/Resource/Textures/FX_smoke_02.png";
        private const string MagicCircleRuneTexturePath =
            "Assets/Eric VFX Studio/Resource/Textures/m10_1.png";
        private const string MagicCircleRingTexturePath =
            "Assets/Eric VFX Studio/Resource/Textures/circle2.PNG";
        private const string NightSkyTexturePath =
            "Assets/Fantasy Skybox FREE/Panoramics/FS017/FS017_Night.png";
        private const string MagicCirclePrefabPath =
            "Assets/Eric VFX Studio/Game VFX - Magic Circle(Free)/Prefabs/FX_MagicCircle_Icearrow01.prefab";

        private const string CauldronModelPath = AlexModelsFolder + "/cauldron_1.fbx";
        private const string WandModelPath = AlexModelsFolder + "/wand_1.fbx";
        private const string PotionModelPath = AlexModelsFolder + "/bottle_1.fbx";
        private const string FurnitureModelPath = AlexModelsFolder + "/shelf_and_table_!.fbx";
        private const string ResetModelPath = AlexModelsFolder + "/reset_button.fbx";

        private static readonly Color DarkStoneTint =
            new Color(0.46f, 0.45f, 0.5f, 1f);
        private static readonly Color FloorStoneTint =
            new Color(0.42f, 0.39f, 0.43f, 1f);
        private static readonly Color StoneTrimTint =
            new Color(0.16f, 0.17f, 0.21f, 1f);
        private static readonly Color WarmWoodTint =
            new Color(0.64f, 0.37f, 0.17f, 1f);

        internal static VisualPassAssets BuildOrUpdate()
        {
            EnsureProjectFolders();

            Shader litShader = RequireShader(LitShaderName);
            Shader particleShader = RequireShader(ParticleShaderName);
            Shader skyboxShader = RequireShader(SkyboxShaderName);

            Texture2D brickTexture = LoadRequiredAsset<Texture2D>(CastleBrickTexturePath);
            Texture2D brickNormal = LoadRequiredAsset<Texture2D>(CastleBrickNormalPath);
            Texture2D floorTexture = LoadRequiredAsset<Texture2D>(CastleFloorTexturePath);
            Texture2D floorNormal = LoadRequiredAsset<Texture2D>(CastleFloorNormalPath);
            Texture2D woodTexture = LoadRequiredAsset<Texture2D>(CastleWoodTexturePath);
            Texture2D woodNormal = LoadRequiredAsset<Texture2D>(CastleWoodNormalPath);
            Texture2D fireGlowTexture = LoadRequiredAsset<Texture2D>(FireGlowTexturePath);
            Texture2D portalSmokeTexture = LoadRequiredAsset<Texture2D>(PortalSmokeTexturePath);
            Texture2D magicCircleRuneTexture =
                LoadRequiredAsset<Texture2D>(MagicCircleRuneTexturePath);
            Texture2D magicCircleRingTexture =
                LoadRequiredAsset<Texture2D>(MagicCircleRingTexturePath);
            Texture2D nightSkyTexture = LoadRequiredAsset<Texture2D>(NightSkyTexturePath);

            Material darkStone = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_DarkStone.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    DarkStoneTint,
                    0.02f,
                    0.2f,
                    brickTexture,
                    brickNormal,
                    Vector2.one,
                    Color.black));

            Material floorStone = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_FloorStone.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    FloorStoneTint,
                    0.01f,
                    0.18f,
                    floorTexture,
                    floorNormal,
                    new Vector2(2f, 2f),
                    Color.black));

            Material stoneTrim = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_StoneTrim.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    StoneTrimTint,
                    0.04f,
                    0.24f,
                    null,
                    null,
                    Vector2.one,
                    Color.black));

            Material warmWood = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_WarmWood.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    WarmWoodTint,
                    0.02f,
                    0.25f,
                    woodTexture,
                    woodNormal,
                    Vector2.one,
                    Color.black));

            Material blackIron = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_BlackIron.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.075f, 0.075f, 0.085f, 1f),
                    0.68f,
                    0.3f,
                    null,
                    null,
                    Vector2.one,
                    Color.black));

            Material gold = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_Gold.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.62f, 0.34f, 0.08f, 1f),
                    0.9f,
                    0.48f,
                    null,
                    null,
                    Vector2.one,
                    Color.black));

            Material cyanEmission = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_CyanEmission.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.015f, 0.12f, 0.15f, 1f),
                    0.05f,
                    0.3f,
                    null,
                    null,
                    Vector2.one,
                    new Color(0.015f, 0.28f, 0.36f, 1f)));

            Material violetEmission = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_VioletEmission.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.12f, 0.035f, 0.18f, 1f),
                    0.04f,
                    0.3f,
                    null,
                    null,
                    Vector2.one,
                    new Color(0.28f, 0.07f, 0.4f, 1f)));

            Material orangeEmission = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_OrangeEmission.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.32f, 0.09f, 0.012f, 1f),
                    0.02f,
                    0.25f,
                    null,
                    null,
                    Vector2.one,
                    new Color(0.55f, 0.13f, 0.015f, 1f)));

            Material portalCore = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_PortalCore.mat",
                litShader,
                material =>
                {
                    ConfigureOpaqueLit(
                        material,
                        new Color(0.025f, 0.11f, 0.16f, 1f),
                        0f,
                        0.02f,
                        nightSkyTexture,
                        null,
                        Vector2.one,
                        new Color(0.012f, 0.08f, 0.12f, 1f));
                    SetFloatIfPresent(material, "_EnvironmentReflections", 0f);
                    SetFloatIfPresent(material, "_SpecularHighlights", 0f);
                });

            Material liquid = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_Liquid.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.015f, 0.2f, 0.24f, 1f),
                    0f,
                    0.38f,
                    null,
                    null,
                    Vector2.one,
                    new Color(0.012f, 0.2f, 0.27f, 1f)));

            Material glassRed = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassRed.mat",
                new Color(0.72f, 0.08f, 0.07f, 0.34f));
            Material glassGreen = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassGreen.mat",
                new Color(0.08f, 0.65f, 0.2f, 0.34f));
            Material glassYellow = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassYellow.mat",
                new Color(0.9f, 0.58f, 0.06f, 0.34f));
            Material glassBlue = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassBlue.mat",
                new Color(0.04f, 0.42f, 0.82f, 0.34f));
            Material glassViolet = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassViolet.mat",
                new Color(0.52f, 0.1f, 0.74f, 0.34f));
            Material glassCyan = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassCyan.mat",
                new Color(0.03f, 0.78f, 0.86f, 0.34f));
            Material glassOrange = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassOrange.mat",
                new Color(0.96f, 0.26f, 0.035f, 0.34f));
            Material glassMagenta = BuildGlassMaterial(
                litShader,
                "MAT_VP_GlassMagenta.mat",
                new Color(0.9f, 0.04f, 0.4f, 0.34f));

            Material cork = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_Cork.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.32f, 0.18f, 0.075f, 1f),
                    0f,
                    0.1f,
                    null,
                    null,
                    Vector2.one,
                    Color.black));

            Material label = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_Label.mat",
                litShader,
                material => ConfigureOpaqueLit(
                    material,
                    new Color(0.62f, 0.48f, 0.27f, 1f),
                    0f,
                    0.16f,
                    null,
                    null,
                    Vector2.one,
                    Color.black));

            Material fireParticle = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_FireParticle.mat",
                particleShader,
                material => ConfigureParticleMaterial(
                    material,
                    fireGlowTexture,
                    new Color(1.25f, 0.34f, 0.055f, 0.68f),
                    true));

            Material portalParticle = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_PortalParticle.mat",
                particleShader,
                material => ConfigureParticleMaterial(
                    material,
                    portalSmokeTexture,
                    new Color(1f, 1f, 1f, 0.34f),
                    true));

            Material magicCircleRuneParticle = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_MagicCircleRuneParticle.mat",
                particleShader,
                material => ConfigureParticleMaterial(
                    material,
                    magicCircleRuneTexture,
                    Color.white,
                    true));
            Material magicCircleRingParticle = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_MagicCircleRingParticle.mat",
                particleShader,
                material => ConfigureParticleMaterial(
                    material,
                    magicCircleRingTexture,
                    Color.white,
                    true));

            Material skyboxMaterial = UpsertMaterial(
                MaterialsFolder + "/MAT_VP_AstralNightSky.mat",
                skyboxShader,
                material => ConfigureSkybox(material, nightSkyTexture));

            VolumeProfile globalVolumeProfile = BuildOrUpdateVolumeProfile();

            Mesh discMesh = UpsertMesh(
                GeneratedModelsFolder + "/MSH_VP_Disc.asset",
                "MSH_VP_Disc",
                mesh => PopulateDisc(mesh, 0.75f, 64));
            Mesh ringMesh = UpsertMesh(
                GeneratedModelsFolder + "/MSH_VP_Ring.asset",
                "MSH_VP_Ring",
                mesh => PopulateRing(mesh, 0.66f, 0.75f, 64));
            Mesh runeRingMesh = UpsertMesh(
                GeneratedModelsFolder + "/MSH_VP_RuneRing.asset",
                "MSH_VP_RuneRing",
                PopulateRuneRing);

            Scene previewScene = EditorSceneManager.NewPreviewScene();

            try
            {
                GameObject cauldronWrapper = BuildAlexWrapper(
                    previewScene,
                "PF_VP_AlexCauldronVisual",
                VisualPrefabsFolder + "/PF_VP_AlexCauldronVisual.prefab",
                CauldronModelPath,
                blackIron,
                blackIron);

                GameObject wandWrapper = BuildAlexWrapper(
                    previewScene,
                    "PF_VP_AlexWandVisual",
                    VisualPrefabsFolder + "/PF_VP_AlexWandVisual.prefab",
                    WandModelPath,
                    warmWood,
                    gold);

                GameObject potionWrapper = BuildAlexWrapper(
                    previewScene,
                    "PF_VP_AlexPotionVisual",
                    VisualPrefabsFolder + "/PF_VP_AlexPotionVisual.prefab",
                    PotionModelPath,
                    glassBlue,
                    cork,
                    label);

                GameObject furnitureWrapper = BuildAlexWrapper(
                    previewScene,
                    "PF_VP_AlexFurnitureVisual",
                    VisualPrefabsFolder + "/PF_VP_AlexFurnitureVisual.prefab",
                    FurnitureModelPath,
                    warmWood,
                    gold);

                GameObject resetWrapper = BuildAlexWrapper(
                    previewScene,
                    "PF_VP_AlexResetVisual",
                    VisualPrefabsFolder + "/PF_VP_AlexResetVisual.prefab",
                    ResetModelPath,
                    blackIron,
                    cyanEmission);

                GameObject portalPrefab = BuildPortalPrefab(
                    previewScene,
                    discMesh,
                    ringMesh,
                    runeRingMesh,
                    portalCore,
                    cyanEmission,
                    violetEmission,
                    portalParticle,
                    magicCircleRuneParticle,
                    magicCircleRingParticle);

                AssetDatabase.SaveAssets();

                return new VisualPassAssets(
                    darkStone,
                    floorStone,
                    stoneTrim,
                    warmWood,
                    blackIron,
                    gold,
                    cyanEmission,
                    violetEmission,
                    orangeEmission,
                    portalCore,
                    liquid,
                    glassRed,
                    glassGreen,
                    glassYellow,
                    glassBlue,
                    glassViolet,
                    glassCyan,
                    glassOrange,
                    glassMagenta,
                    cork,
                    label,
                    fireParticle,
                    portalParticle,
                    skyboxMaterial,
                    globalVolumeProfile,
                    discMesh,
                    ringMesh,
                    runeRingMesh,
                    cauldronWrapper,
                    wandWrapper,
                    potionWrapper,
                    furnitureWrapper,
                    resetWrapper,
                    portalPrefab);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static Material BuildGlassMaterial(
            Shader litShader,
            string fileName,
            Color tint)
        {
            return UpsertMaterial(
                MaterialsFolder + "/" + fileName,
                litShader,
                material => ConfigureTransparentLit(material, tint));
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder(MaterialsFolder);
            EnsureFolder(GeneratedModelsFolder);
            EnsureFolder(SettingsFolder);
            EnsureFolder(VisualPrefabsFolder);
            EnsureFolder(EnvironmentPrefabsFolder);
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetFolder);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                throw new InvalidOperationException(
                    "Invalid Unity asset folder: " + assetFolder);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Shader RequireShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Required shader was not found: " + shaderName);
            }

            return shader;
        }

        private static T LoadRequiredAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                throw new InvalidOperationException(
                    "Required visual-pass source asset was not found: " + path);
            }

            return asset;
        }

        private static Material UpsertMaterial(
            string path,
            Shader shader,
            Action<Material> configure)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);

                if (existing != null)
                {
                    throw new InvalidOperationException(
                        "Expected a Material at project-owned path: " + path);
                }

                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.name = Path.GetFileNameWithoutExtension(path);
            material.enableInstancing = true;
            material.doubleSidedGI = false;
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            configure(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureOpaqueLit(
            Material material,
            Color baseColor,
            float metallic,
            float smoothness,
            Texture2D baseTexture,
            Texture2D normalTexture,
            Vector2 textureScale,
            Color emissionColor)
        {
            SetFloatIfPresent(material, "_Surface", 0f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.Zero);
            SetFloatIfPresent(material, "_ZWrite", 1f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Back);
            SetFloatIfPresent(material, "_Metallic", metallic);
            SetFloatIfPresent(material, "_Smoothness", smoothness);
            SetFloatIfPresent(material, "_BumpScale", 0.65f);
            SetColorIfPresent(material, "_BaseColor", baseColor);
            SetColorIfPresent(material, "_Color", baseColor);
            SetTextureIfPresent(material, "_BaseMap", baseTexture);
            SetTextureIfPresent(material, "_MainTex", baseTexture);
            SetTextureScaleIfPresent(material, "_BaseMap", textureScale);
            SetTextureScaleIfPresent(material, "_MainTex", textureScale);
            SetTextureIfPresent(material, "_BumpMap", normalTexture);

            if (normalTexture != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            bool hasEmission =
                emissionColor.r > 0.0001f ||
                emissionColor.g > 0.0001f ||
                emissionColor.b > 0.0001f;
            if (hasEmission)
            {
                SetColorIfPresent(material, "_EmissionColor", emissionColor);
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                SetColorIfPresent(material, "_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)RenderQueue.Geometry;
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.SetShaderPassEnabled("DepthOnly", true);
        }

        private static void ConfigureTransparentLit(Material material, Color tint)
        {
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Back);
            SetFloatIfPresent(material, "_Metallic", 0f);
            SetFloatIfPresent(material, "_Smoothness", 0.62f);
            SetColorIfPresent(material, "_BaseColor", tint);
            SetColorIfPresent(material, "_Color", tint);
            SetTextureIfPresent(material, "_BaseMap", null);
            SetTextureIfPresent(material, "_MainTex", null);
            SetTextureIfPresent(material, "_BumpMap", null);
            SetColorIfPresent(material, "_EmissionColor", Color.black);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_EMISSION");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
        }

        private static void ConfigureParticleMaterial(
            Material material,
            Texture2D texture,
            Color tint,
            bool additive)
        {
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", additive ? 2f : 0f);
            SetFloatIfPresent(material, "_AlphaClip", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(
                material,
                "_DstBlend",
                additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Off);
            SetColorIfPresent(material, "_BaseColor", tint);
            SetColorIfPresent(material, "_Color", tint);
            SetTextureIfPresent(material, "_BaseMap", texture);
            SetTextureIfPresent(material, "_MainTex", texture);
            SetFloatIfPresent(material, "_SoftParticlesEnabled", 0f);
            SetFloatIfPresent(material, "_CameraFadingEnabled", 0f);
            SetFloatIfPresent(material, "_DistortionEnabled", 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_FADING_ON");
            material.DisableKeyword("_DISTORTION_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
        }

        private static void ConfigureSkybox(Material material, Texture2D nightSkyTexture)
        {
            material.enableInstancing = false;
            SetTextureIfPresent(material, "_MainTex", nightSkyTexture);
            SetFloatIfPresent(material, "_Exposure", 0.55f);
            SetFloatIfPresent(material, "_Rotation", 0f);
            SetFloatIfPresent(material, "_Mapping", 1f);
            SetFloatIfPresent(material, "_ImageType", 0f);
            SetFloatIfPresent(material, "_MirrorOnBack", 0f);
            material.renderQueue = -1;
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void SetColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void SetTextureIfPresent(
            Material material,
            string property,
            Texture texture)
        {
            if (material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static void SetTextureScaleIfPresent(
            Material material,
            string property,
            Vector2 scale)
        {
            if (material.HasProperty(property))
            {
                material.SetTextureScale(property, scale);
            }
        }

        private static VolumeProfile BuildOrUpdateVolumeProfile()
        {
            string path = SettingsFolder + "/VP_GlobalVolumeProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

            if (profile == null)
            {
                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);

                if (existing != null)
                {
                    throw new InvalidOperationException(
                        "Expected a VolumeProfile at project-owned path: " + path);
                }

                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "VP_GlobalVolumeProfile";
                AssetDatabase.CreateAsset(profile, path);
            }

            Bloom bloom = GetOrAddVolumeComponent<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.Override(0.5f);
            bloom.intensity.Override(0.08f);
            bloom.scatter.Override(0.32f);
            bloom.clamp.Override(1.5f);
            bloom.highQualityFiltering.Override(false);
            bloom.dirtTexture.Override(null);
            bloom.dirtIntensity.Override(0f);

            Tonemapping tonemapping = GetOrAddVolumeComponent<Tonemapping>(profile);
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments colorAdjustments =
                GetOrAddVolumeComponent<ColorAdjustments>(profile);
            colorAdjustments.active = true;
            colorAdjustments.postExposure.Override(0.28f);
            colorAdjustments.contrast.Override(4f);
            colorAdjustments.colorFilter.Override(
                new Color(1f, 0.97f, 0.94f, 1f));
            colorAdjustments.hueShift.Override(0f);
            colorAdjustments.saturation.Override(2f);

            if (profile.TryGet(out DepthOfField depthOfField))
            {
                depthOfField.active = false;
                EditorUtility.SetDirty(depthOfField);
            }

            if (profile.TryGet(out MotionBlur motionBlur))
            {
                motionBlur.active = false;
                EditorUtility.SetDirty(motionBlur);
            }

            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(tonemapping);
            EditorUtility.SetDirty(colorAdjustments);
            profile.Reset();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAddVolumeComponent<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
            {
                return component;
            }

            T newComponent = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(newComponent, profile);
            EditorUtility.SetDirty(newComponent);
            return newComponent;
        }

        private static Mesh UpsertMesh(
            string path,
            string name,
            Action<Mesh> populate)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null)
            {
                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);

                if (existing != null)
                {
                    throw new InvalidOperationException(
                        "Expected a Mesh at project-owned path: " + path);
                }

                mesh = new Mesh { name = name };
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.name = name;
            mesh.Clear();
            populate(mesh);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void PopulateDisc(Mesh mesh, float radius, int segments)
        {
            List<Vector3> vertices = new List<Vector3>(segments + 1)
            {
                Vector3.zero
            };
            List<Vector3> normals = new List<Vector3>(segments + 1)
            {
                Vector3.forward
            };
            List<Vector2> uvs = new List<Vector2>(segments + 1)
            {
                new Vector2(0.5f, 0.5f)
            };
            List<int> triangles = new List<int>(segments * 3);

            for (int index = 0; index < segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector3 vertex = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);
                vertices.Add(vertex);
                normals.Add(Vector3.forward);
                uvs.Add(new Vector2(
                    vertex.x / (radius * 2f) + 0.5f,
                    vertex.y / (radius * 2f) + 0.5f));
            }

            for (int index = 0; index < segments; index++)
            {
                triangles.Add(0);
                triangles.Add(index + 1);
                triangles.Add((index + 1) % segments + 1);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
        }

        private static void PopulateRing(
            Mesh mesh,
            float innerRadius,
            float outerRadius,
            int segments)
        {
            List<Vector3> vertices = new List<Vector3>(segments * 2);
            List<Vector3> normals = new List<Vector3>(segments * 2);
            List<Vector2> uvs = new List<Vector2>(segments * 2);
            List<int> triangles = new List<int>(segments * 6);

            for (int index = 0; index < segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                float cosine = Mathf.Cos(angle);
                float sine = Mathf.Sin(angle);

                vertices.Add(new Vector3(cosine * outerRadius, sine * outerRadius, 0f));
                vertices.Add(new Vector3(cosine * innerRadius, sine * innerRadius, 0f));
                normals.Add(Vector3.forward);
                normals.Add(Vector3.forward);
                uvs.Add(new Vector2(index / (float)segments, 1f));
                uvs.Add(new Vector2(index / (float)segments, 0f));
            }

            for (int index = 0; index < segments; index++)
            {
                int next = (index + 1) % segments;
                int outer = index * 2;
                int inner = outer + 1;
                int nextOuter = next * 2;
                int nextInner = nextOuter + 1;

                triangles.Add(outer);
                triangles.Add(nextOuter);
                triangles.Add(inner);
                triangles.Add(nextOuter);
                triangles.Add(nextInner);
                triangles.Add(inner);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
        }

        private static void PopulateRuneRing(Mesh mesh)
        {
            const int runeCount = 24;
            const float innerRadius = 0.52f;
            const float baseOuterRadius = 0.61f;
            float step = Mathf.PI * 2f / runeCount;
            float halfSpan = step * 0.29f;

            List<Vector3> vertices = new List<Vector3>(runeCount * 4);
            List<Vector3> normals = new List<Vector3>(runeCount * 4);
            List<Vector2> uvs = new List<Vector2>(runeCount * 4);
            List<int> triangles = new List<int>(runeCount * 6);

            for (int index = 0; index < runeCount; index++)
            {
                float center = index * step;
                float start = center - halfSpan;
                float end = center + halfSpan;
                float outerRadius = baseOuterRadius +
                    (index % 3 == 0 ? 0.028f : index % 2 == 0 ? 0.012f : 0f);
                int baseIndex = vertices.Count;

                vertices.Add(PolarPoint(start, innerRadius));
                vertices.Add(PolarPoint(start, outerRadius));
                vertices.Add(PolarPoint(end, outerRadius));
                vertices.Add(PolarPoint(end, innerRadius));

                normals.Add(Vector3.forward);
                normals.Add(Vector3.forward);
                normals.Add(Vector3.forward);
                normals.Add(Vector3.forward);

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
        }

        private static Vector3 PolarPoint(float angle, float radius)
        {
            return new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f);
        }

        private static GameObject BuildAlexWrapper(
            Scene previewScene,
            string prefabName,
            string prefabPath,
            string modelPath,
            params Material[] materials)
        {
            GameObject modelAsset = LoadRequiredAsset<GameObject>(modelPath);
            GameObject root = new GameObject(prefabName);
            SceneManager.MoveGameObjectToScene(root, previewScene);

            try
            {
                GameObject visualPivot = new GameObject("VisualPivot");
                SceneManager.MoveGameObjectToScene(visualPivot, previewScene);
                visualPivot.transform.SetParent(root.transform, false);
                visualPivot.transform.localRotation =
                    GetAlexVisualPivotRotation(prefabName);

                GameObject modelInstance =
                    PrefabUtility.InstantiatePrefab(modelAsset, previewScene) as GameObject;

                if (modelInstance == null)
                {
                    throw new InvalidOperationException(
                        "Could not instantiate Alex model: " + modelPath);
                }

                modelInstance.name = "AlexMesh";
                modelInstance.transform.SetParent(visualPivot.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one * 10f;

                ApplyMaterials(modelInstance, materials);
                RemoveColliders(modelInstance);

                return SavePrefab(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Quaternion GetAlexVisualPivotRotation(
            string prefabName)
        {
            if (prefabName.IndexOf(
                    "Potion",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Quaternion.Euler(180f, 0f, 0f);
            }

            if (prefabName.IndexOf(
                    "Cauldron",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Alex's final FBX carries a -90 degree geometric X rotation.
                // A +90 compensation makes its long axis vertical, but leaves
                // the authored top and bottom exchanged. The equivalent -90
                // wrapper rotation both compensates the axis and turns the
                // visible vessel right-side up without touching gameplay or FBX.
                return Quaternion.Euler(-90f, 0f, 0f);
            }

            if (prefabName.IndexOf(
                    "Furniture",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // The combined shelf/table FBX is authored at -87.500008 X.
                return Quaternion.Euler(87.500008f, 0f, 0f);
            }

            return Quaternion.identity;
        }

        private static void ApplyMaterials(GameObject root, Material[] materials)
        {
            if (materials == null || materials.Length == 0)
            {
                throw new ArgumentException("At least one material is required.", nameof(materials));
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                int slotCount = GetSubMeshCount(renderer);
                Material[] assignedMaterials = new Material[slotCount];

                for (int slot = 0; slot < slotCount; slot++)
                {
                    assignedMaterials[slot] = materials[Mathf.Min(slot, materials.Length - 1)];
                }

                renderer.sharedMaterials = assignedMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
        }

        private static int GetSubMeshCount(Renderer renderer)
        {
            Mesh mesh = null;

            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }
            else
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

                if (meshFilter != null)
                {
                    mesh = meshFilter.sharedMesh;
                }
            }

            if (mesh != null)
            {
                return Mathf.Max(1, mesh.subMeshCount);
            }

            return Mathf.Max(1, renderer.sharedMaterials.Length);
        }

        private static void RemoveColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

            foreach (Collider collider in colliders)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(collider))
                {
                    collider.enabled = false;
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }

        private static GameObject BuildPortalPrefab(
            Scene previewScene,
            Mesh discMesh,
            Mesh ringMesh,
            Mesh runeRingMesh,
            Material portalCore,
            Material cyanEmission,
            Material violetEmission,
            Material portalParticle,
            Material magicCircleRuneParticle,
            Material magicCircleRingParticle)
        {
            const string prefabPath =
                EnvironmentPrefabsFolder + "/PF_VP_AstralPortal.prefab";
            GameObject root = new GameObject("PF_VP_AstralPortal");
            SceneManager.MoveGameObjectToScene(root, previewScene);

            try
            {
                MeshRenderer coreRenderer = CreateMeshChild(
                    root.transform,
                    "Core",
                    discMesh,
                    portalCore,
                    new Vector3(0f, 0f, 0f));

                MeshRenderer outerRenderer = CreateMeshChild(
                    root.transform,
                    "OuterRing",
                    ringMesh,
                    cyanEmission,
                    new Vector3(0f, 0f, 0.012f));

                MeshRenderer innerRenderer = CreateMeshChild(
                    root.transform,
                    "InnerRuneRing",
                    runeRingMesh,
                    violetEmission,
                    new Vector3(0f, 0f, 0.022f));
                innerRenderer.transform.localScale = Vector3.one * 0.9f;

                outerRenderer.shadowCastingMode = ShadowCastingMode.Off;
                outerRenderer.receiveShadows = false;
                innerRenderer.shadowCastingMode = ShadowCastingMode.Off;
                innerRenderer.receiveShadows = false;
                coreRenderer.shadowCastingMode = ShadowCastingMode.Off;
                coreRenderer.receiveShadows = false;

                // The room portal is a full-wall astral opening. Keep these
                // reusable animator anchors but hide the old circular plate and
                // heavy geometric rings so they cannot read as a framed disc.
                coreRenderer.enabled = false;
                outerRenderer.enabled = false;
                innerRenderer.enabled = false;

                AddMagicCircleSource(
                    root.transform,
                    previewScene,
                    discMesh,
                    portalParticle,
                    magicCircleRuneParticle,
                    magicCircleRingParticle);

                Light portalLight = CreatePortalLight(root.transform);
                CreateInwardPortalParticles(root.transform, portalParticle);

                AstralPortalAnimator animator = root.AddComponent<AstralPortalAnimator>();
                SerializedObject serializedAnimator = new SerializedObject(animator);
                serializedAnimator.FindProperty("_outerRing").objectReferenceValue =
                    outerRenderer.transform;
                serializedAnimator.FindProperty("_innerRing").objectReferenceValue =
                    innerRenderer.transform;
                serializedAnimator.FindProperty("_portalLight").objectReferenceValue = portalLight;
                serializedAnimator.FindProperty("_coreRenderer").objectReferenceValue = coreRenderer;
                serializedAnimator.FindProperty("_outerRingDegreesPerSecond").floatValue = 2.2f;
                serializedAnimator.FindProperty("_innerRingDegreesPerSecond").floatValue = 3.1f;
                serializedAnimator.FindProperty("_minimumLightIntensity").floatValue = 0.08f;
                serializedAnimator.FindProperty("_maximumLightIntensity").floatValue = 0.18f;
                serializedAnimator.FindProperty("_pulseCyclesPerSecond").floatValue = 0.09f;
                serializedAnimator.FindProperty("_coreEmissionColor").colorValue =
                    new Color(0.08f, 0.42f, 0.62f, 1f);
                serializedAnimator.FindProperty("_minimumEmissionMultiplier").floatValue = 0.35f;
                serializedAnimator.FindProperty("_maximumEmissionMultiplier").floatValue = 0.68f;
                serializedAnimator.ApplyModifiedPropertiesWithoutUndo();

                RemoveColliders(root);
                return SavePrefab(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static MeshRenderer CreateMeshChild(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            MeshFilter filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return renderer;
        }

        private static void AddMagicCircleSource(
            Transform parent,
            Scene previewScene,
            Mesh fixedCircleMesh,
            Material portalParticle,
            Material magicCircleRuneParticle,
            Material magicCircleRingParticle)
        {
            GameObject sourceAsset =
                LoadRequiredAsset<GameObject>(MagicCirclePrefabPath);
            GameObject source = PrefabUtility.InstantiatePrefab(
                sourceAsset,
                previewScene) as GameObject;
            if (source == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate the imported Magic Circle source.");
            }

            PrefabUtility.UnpackPrefabInstance(
                source,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            source.name = "MagicCircle_Source_QuestSubset";
            source.transform.SetParent(parent, false);
            source.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            source.transform.localRotation = Quaternion.identity;
            source.transform.localScale = Vector3.one * 0.18f;

            HashSet<string> keptSystemNames = new HashSet<string>(
                new[] { "rot_rune", "rot_ring", "dust" },
                StringComparer.OrdinalIgnoreCase);
            ParticleSystem[] systems =
                source.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem system in systems)
            {
                ParticleSystemRenderer renderer =
                    system.GetComponent<ParticleSystemRenderer>();
                if (!keptSystemNames.Contains(system.gameObject.name))
                {
                    if (renderer != null)
                    {
                        UnityEngine.Object.DestroyImmediate(renderer);
                    }
                    UnityEngine.Object.DestroyImmediate(system);
                    continue;
                }

                ParticleSystem.MainModule main = system.main;
                main.maxParticles = string.Equals(
                    system.gameObject.name,
                    "dust",
                    StringComparison.OrdinalIgnoreCase)
                    ? 6
                    : 1;
                if (renderer != null)
                {
                    if (string.Equals(
                        system.gameObject.name,
                        "rot_rune",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        system.transform.localRotation = Quaternion.identity;
                        renderer.renderMode = ParticleSystemRenderMode.Mesh;
                        renderer.mesh = fixedCircleMesh;
                        renderer.alignment = ParticleSystemRenderSpace.Local;
                        renderer.sharedMaterial = magicCircleRuneParticle;
                        main.startColor = new ParticleSystem.MinMaxGradient(
                            new Color(0.42f, 0.12f, 0.68f, 0.11f));
                    }
                    else if (string.Equals(
                        system.gameObject.name,
                        "rot_ring",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        system.transform.localRotation = Quaternion.identity;
                        renderer.renderMode = ParticleSystemRenderMode.Mesh;
                        renderer.mesh = fixedCircleMesh;
                        renderer.alignment = ParticleSystemRenderSpace.Local;
                        renderer.sharedMaterial = magicCircleRingParticle;
                        main.startColor = new ParticleSystem.MinMaxGradient(
                            new Color(0.08f, 0.52f, 0.72f, 0.09f));
                    }
                    else
                    {
                        renderer.renderMode =
                            ParticleSystemRenderMode.Billboard;
                        renderer.sharedMaterial = portalParticle;
                        main.startColor = new ParticleSystem.MinMaxGradient(
                            new Color(0.22f, 0.55f, 0.85f, 0.18f));
                    }
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.motionVectorGenerationMode =
                        MotionVectorGenerationMode.ForceNoMotion;
                }
            }

            RemoveColliders(source);
        }

        private static Light CreatePortalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("PortalAccentLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(0f, 0f, 0.18f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.19f, 0.48f, 0.88f, 1f);
            light.intensity = 0.12f;
            light.range = 1.8f;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0f;
            light.renderMode = LightRenderMode.Auto;
            return light;
        }

        private static void CreateInwardPortalParticles(
            Transform parent,
            Material portalParticle)
        {
            GameObject particleObject = new GameObject("InwardParticles");
            particleObject.transform.SetParent(parent, false);
            particleObject.transform.localPosition = new Vector3(0f, 0f, 0.032f);

            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 4f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 10;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.6f, 3.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(-0.13f, -0.07f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.028f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.12f, 0.72f, 1f, 0.18f),
                new Color(0.55f, 0.18f, 0.9f, 0.24f));

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 2.4f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.68f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.randomDirectionAmount = 0.08f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
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
                    new GradientAlphaKey(0.65f, 0.25f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = alphaFade;

            ParticleSystemRenderer renderer =
                particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = portalParticle;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.sortingFudge = -0.1f;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);

            if (!success || prefab == null)
            {
                throw new InvalidOperationException(
                    "Could not create or update project-owned prefab: " + path);
            }

            return prefab;
        }
    }
}
