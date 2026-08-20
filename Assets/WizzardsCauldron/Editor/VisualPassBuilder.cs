using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Repeatable entry point for the project-owned visual pass. The builder
    /// protects the functional source scene, updates only generated assets and
    /// the dedicated visual scene, and intentionally never edits shared
    /// gameplay prefab assets.
    /// </summary>
    internal static class VisualPassBuilder
    {
        internal const string SourceScenePath =
            "Assets/WizzardsCauldron/Scenes/SCN_InteractionTest.unity";

        internal const string VisualScenePath =
            "Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity";

        internal const string GeneratedRootName =
            "__WC_VISUAL_PASS__";

        private const string ExpectedSourceSceneSha256 =
            "2A13B8401B8C493386575CBBB09E09BA43F1BCFBEEE3C7420D7EB106A638B0BC";

        [MenuItem(
            "Tools/Wizzards Cauldron/Build Visual Pass",
            priority = 200)]
        public static void BuildVisualPass()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log(
                    "[WC_VISUAL_PASS] Build cancelled before opening the " +
                    "dedicated visual scene.");
                return;
            }

            BuildVisualPassBatch();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Wizzards Cauldron",
                    "The visual pass was rebuilt in " + VisualScenePath +
                    ". The protected source scene was not changed.",
                    "OK");
            }
        }

        /// <summary>
        /// Public so Unity's -executeMethod can run the same deterministic path
        /// used by the editor menu.
        /// </summary>
        public static void BuildVisualPassBatch()
        {
            VerifyProtectedSourceScene();

            if (!File.Exists(VisualScenePath))
            {
                throw new FileNotFoundException(
                    "The visual target scene is missing.",
                    VisualScenePath);
            }

            Scene visualScene = EditorSceneManager.OpenScene(
                VisualScenePath,
                OpenSceneMode.Single);

            if (!visualScene.IsValid() || !visualScene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Unity could not open the visual target scene.");
            }

            Scene sourceScene = default(Scene);
            try
            {
                sourceScene = EditorSceneManager.OpenScene(
                    SourceScenePath,
                    OpenSceneMode.Additive);
                VisualPassValidator.AssertProtectedStateMatchesForBuild(
                    sourceScene,
                    visualScene);
            }
            finally
            {
                if (sourceScene.IsValid() && sourceScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }

            VisualPassAssets assets =
                VisualPassAssetFactory.BuildOrUpdate();

            VisualPassSceneComposer.Compose(
                visualScene,
                assets);

            if (!EditorSceneManager.SaveScene(
                visualScene,
                VisualScenePath,
                false))
            {
                throw new InvalidOperationException(
                    "Unity could not save the visual target scene.");
            }

            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            VerifyProtectedSourceScene();

            Debug.Log(
                "[WC_VISUAL_PASS] BUILD COMPLETE | scene=" +
                VisualScenePath + " | sourceProtected=true | " +
                "buildScene=" + VisualScenePath + ".");
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(
                    VisualScenePath,
                    true)
            };

            Debug.Log(
                "[WC_VISUAL_PASS] Build Settings now contain only the " +
                "enabled visual target scene.");
        }

        private static void VerifyProtectedSourceScene()
        {
            if (!File.Exists(SourceScenePath))
            {
                throw new FileNotFoundException(
                    "The protected functional source scene is missing.",
                    SourceScenePath);
            }

            string sourceHash = ComputeSha256(SourceScenePath);
            if (!string.Equals(
                sourceHash,
                ExpectedSourceSceneSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Protected source-scene checksum changed. Expected " +
                    ExpectedSourceSceneSha256 + " but found " + sourceHash +
                    ". Visual build stopped without saving the target scene.");
            }
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
