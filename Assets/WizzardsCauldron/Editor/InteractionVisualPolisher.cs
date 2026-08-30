using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.Presentation;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Targeted, repeatable polish for the two physical controls. This tool
    /// never runs the full visual builder and never changes gameplay colliders.
    /// </summary>
    internal static class InteractionVisualPolisher
    {
        private const string FinishRuneName = "WC_FinishTargetRune";
        private const string ResetRuneName = "WC_ResetRuneButton";

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
                "[WC_INTERACTION_VISUALS] COMPLETE | fullVisualBuild=false | gameplayCollidersUnchanged=true");
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
                resetControl,
                disc,
                ring,
                runes,
                iron,
                gold,
                cyan);

            EditorSceneManager.MarkSceneDirty(scene);
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
            ResetHoldControl resetControl,
            Mesh disc,
            Mesh ring,
            Mesh runes,
            Material iron,
            Material gold,
            Material cyan)
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
            visual.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            visual.transform.SetParent(resetRoot, true);

            CreateMesh(
                visual.transform,
                "IronButtonBase",
                disc,
                iron,
                new Vector3(0.24f, 0.24f, 1f),
                new Vector3(0f, 0f, 0.018f),
                Quaternion.identity);

            CreateMesh(
                visual.transform,
                "GoldButtonFrame",
                ring,
                gold,
                new Vector3(0.225f, 0.225f, 1f),
                new Vector3(0f, 0f, 0.012f),
                Quaternion.identity);

            CreateMesh(
                visual.transform,
                "CyanResetRunes",
                runes,
                cyan,
                new Vector3(0.17f, 0.17f, 1f),
                new Vector3(0f, 0f, 0.006f),
                Quaternion.identity);

            CreateRuneGlyph(
                visual.transform,
                "ResetGlyph",
                gold,
                cyan,
                0f,
                0.075f);
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
            GameObject particlesObject = new GameObject(
                "WandActivationSparkBurst");
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
                new Color(0.1f, 0.9f, 1f, 1f),
                new Color(1f, 0.58f, 0.12f, 1f));
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 24)
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
    }
}
