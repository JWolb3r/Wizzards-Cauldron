using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Creates deterministic desktop preview renders without saving or changing
    /// the visual scene. The previews are diagnostics, not gameplay cameras.
    /// </summary>
    internal static class VisualPassPreviewCapture
    {
        private const string VisualScenePath =
            "Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity";

        private const string OutputFolder = "Logs/VisualPassPreviews";

        private static readonly PreviewView[] Views =
        {
            new PreviewView(
                "01_RoomOverview.png",
                new Vector3(0f, 1.55f, -1.25f),
                new Vector3(0f, 1.25f, 1.25f),
                68f),
            new PreviewView(
                "02_GameplayArea.png",
                new Vector3(0f, 1.62f, -0.25f),
                new Vector3(0f, 1.05f, 1.05f),
                62f),
            new PreviewView(
                "03_AstralPortal.png",
                new Vector3(0.85f, 1.6f, 0.35f),
                new Vector3(-1.91f, 1.75f, 1.9f),
                58f),
            new PreviewView(
                "04_BackCorner.png",
                new Vector3(1.55f, 1.62f, 2.55f),
                new Vector3(-0.05f, 1.1f, 1.05f),
                64f)
        };

        [MenuItem(
            "Tools/Wizzards Cauldron/Capture Visual Pass Previews",
            priority = 220)]
        public static void CaptureVisualPassPreviews()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log(
                    "[WC_VISUAL_PREVIEW] Capture cancelled before " +
                    "opening the visual scene.");
                return;
            }

            SceneSetup[] previousSetup = Application.isBatchMode
                ? null
                : EditorSceneManager.GetSceneManagerSetup();

            try
            {
                CaptureVisualPassPreviewsBatch();
            }
            finally
            {
                if (previousSetup != null)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
                }
            }

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Wizzards Cauldron",
                    "Four visual-pass previews were written to " +
                    OutputFolder + ".",
                    "OK");
            }
        }

        public static void CaptureVisualPassPreviewsBatch()
        {
            if (!File.Exists(VisualScenePath))
            {
                throw new FileNotFoundException(
                    "Visual target scene is missing.",
                    VisualScenePath);
            }

            Scene scene = EditorSceneManager.OpenScene(
                VisualScenePath,
                OpenSceneMode.Single);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Could not load the visual target scene.");
            }

            string absoluteOutputFolder = Path.GetFullPath(
                OutputFolder);
            Directory.CreateDirectory(absoluteOutputFolder);

            GameObject cameraObject = new GameObject(
                "__WC_PREVIEW_CAMERA__");

            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.cameraType = CameraType.Preview;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = new Color(
                    0.005f,
                    0.008f,
                    0.02f,
                    1f);
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;
                camera.allowHDR = true;
                camera.allowMSAA = false;

                UniversalAdditionalCameraData cameraData =
                    cameraObject.AddComponent<
                        UniversalAdditionalCameraData>();
                cameraData.renderPostProcessing = true;
                cameraData.antialiasing =
                    AntialiasingMode.FastApproximateAntialiasing;

                foreach (PreviewView view in Views)
                {
                    CaptureView(
                        camera,
                        view,
                        absoluteOutputFolder);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            Debug.Log(
                "[WC_VISUAL_PREVIEW] Captured " + Views.Length +
                " previews in " + absoluteOutputFolder + ".");
        }

        private static void CaptureView(
            Camera camera,
            PreviewView view,
            string absoluteOutputFolder)
        {
            const int width = 1280;
            const int height = 720;

            camera.transform.position = view.Position;
            camera.transform.rotation = Quaternion.LookRotation(
                view.Target - view.Position,
                Vector3.up);
            camera.fieldOfView = view.FieldOfView;

            RenderTexture renderTexture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                name = "WC Visual Preview"
            };

            Texture2D image = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false,
                false);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                image.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0,
                    false);
                image.Apply(false, false);

                string outputPath = Path.Combine(
                    absoluteOutputFolder,
                    view.FileName);
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                Debug.Log(
                    "[WC_VISUAL_PREVIEW] Wrote " + outputPath + ".");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private readonly struct PreviewView
        {
            public PreviewView(
                string fileName,
                Vector3 position,
                Vector3 target,
                float fieldOfView)
            {
                FileName = fileName;
                Position = position;
                Target = target;
                FieldOfView = fieldOfView;
            }

            public string FileName { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
            public float FieldOfView { get; }
        }
    }
}
