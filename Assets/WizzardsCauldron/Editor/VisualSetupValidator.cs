using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Read-only checks for the visual-pass workspace. This utility deliberately
    /// contains no calls that save scenes, prefabs, or build settings.
    /// </summary>
    internal static class VisualSetupValidator
    {
        private const string ProjectFolder = "Assets/WizzardsCauldron";
        private const string SourceScenePath = ProjectFolder + "/Scenes/SCN_InteractionTest.unity";
        private const string VisualScenePath = ProjectFolder + "/Scenes/SCN_InteractionTest_Visual.unity";
        private const string AlexModelsFolder = ProjectFolder + "/Art/Models/Alex";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private static readonly PackageFolder[] RequiredPackageFolders =
        {
            new PackageFolder("Dark Stylized Castle Kit", "Assets/StylizedDarkCastle"),
            new PackageFolder("Stylized Magic Books", "Assets/GreyratsLab/Stylized magic books"),
            new PackageFolder("Free Mining Pack", "Assets/PurePoly/Mining_Free_Assets"),
            new PackageFolder("Free Pack - Fire Effects", "Assets/PolyOne/Fire Effects"),
            new PackageFolder("Free Game VFX - Magic Circle URP", "Assets/Eric VFX Studio/Game VFX - Magic Circle(Free)"),
            new PackageFolder("Fantasy Skybox FREE", "Assets/Fantasy Skybox FREE")
        };

        private static readonly ExpectedModel[] ExpectedAlexModels =
        {
            new ExpectedModel("Kessel", "cauldron", "kessel"),
            new ExpectedModel("Zauberstab", "wand", "zauberstab"),
            new ExpectedModel("Trankflasche", "bottle", "potion", "trankflasche", "flasche"),
            new ExpectedModel("Regal/Tisch", "shelf", "table", "regal", "tisch"),
            new ExpectedModel("Reset-Knopf", "reset", "button", "knopf")
        };

        [MenuItem("Tools/Wizzards Cauldron/Validate Visual Setup", priority = 100)]
        public static void ValidateVisualSetup()
        {
            ValidationReport report = new ValidationReport();

            try
            {
                ValidateScenes(report);
                ValidateAlexModels(report);
                ValidateShaders(report);
                ValidatePackageFolders(report);
                ValidateMissingScripts(report);
                ValidateBuildSettings(report);
            }
            catch (Exception exception)
            {
                report.Fail("Die Pruefung wurde unerwartet abgebrochen: " + exception.Message);
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string fullReport = report.BuildFullReport();
            Debug.Log(fullReport);

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Wizzards Cauldron - Visual Setup",
                    report.BuildDialogSummary(),
                    "OK");
            }
        }

        private static void ValidateScenes(ValidationReport report)
        {
            ValidateRequiredAsset(report, "Ausgangsszene", SourceScenePath);
            ValidateRequiredAsset(report, "visuelle Szenenkopie", VisualScenePath);
        }

        private static void ValidateRequiredAsset(ValidationReport report, string label, string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset != null)
            {
                report.Pass(label + " vorhanden: " + assetPath);
            }
            else
            {
                report.Fail(label + " fehlt: " + assetPath);
            }
        }

        private static void ValidateAlexModels(ValidationReport report)
        {
            if (!AssetDatabase.IsValidFolder(AlexModelsFolder))
            {
                report.Fail("Alex-Modellordner fehlt: " + AlexModelsFolder);
                return;
            }

            string[] fbxPaths = AssetDatabase.FindAssets("t:Model", new[] { AlexModelsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (fbxPaths.Length == ExpectedAlexModels.Length)
            {
                report.Pass("Genau fuenf Alex-FBX-Dateien gefunden.");
            }
            else if (fbxPaths.Length < ExpectedAlexModels.Length)
            {
                report.Fail("Nur " + fbxPaths.Length + " von fuenf erwarteten Alex-FBX-Dateien gefunden.");
            }
            else
            {
                report.Warn(fbxPaths.Length + " Alex-FBX-Dateien gefunden; erwartet sind fuenf finale Modelle.");
            }

            foreach (ExpectedModel expectedModel in ExpectedAlexModels)
            {
                string match = fbxPaths.FirstOrDefault(path => expectedModel.Matches(Path.GetFileNameWithoutExtension(path)));
                if (!string.IsNullOrEmpty(match))
                {
                    report.Pass("Alex-Modell " + expectedModel.Label + ": " + match);
                }
                else
                {
                    report.Fail("Alex-Modell nicht erkannt: " + expectedModel.Label + " (Dateiname pruefen).");
                }
            }
        }

        private static void ValidateShaders(ValidationReport report)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader != null)
            {
                report.Pass("URP Lit Shader gefunden.");
            }
            else
            {
                report.Fail("Shader fehlt: Universal Render Pipeline/Lit.");
            }

            string[] particleShaderNames =
            {
                "Universal Render Pipeline/Particles/Unlit",
                "Universal Render Pipeline/Particles/Lit"
            };

            string foundParticleShader = particleShaderNames.FirstOrDefault(shaderName => Shader.Find(shaderName) != null);
            if (!string.IsNullOrEmpty(foundParticleShader))
            {
                report.Pass("URP-Partikelshader gefunden: " + foundParticleShader);
            }
            else
            {
                report.Fail("Kein URP-Partikelshader gefunden (Particles/Unlit oder Particles/Lit).");
            }
        }

        private static void ValidatePackageFolders(ValidationReport report)
        {
            foreach (PackageFolder packageFolder in RequiredPackageFolders)
            {
                if (!AssetDatabase.IsValidFolder(packageFolder.Path))
                {
                    report.Fail("Asset-Paket fehlt: " + packageFolder.Label + " (" + packageFolder.Path + ")");
                    continue;
                }

                int importedAssetCount = AssetDatabase.FindAssets(string.Empty, new[] { packageFolder.Path }).Length;
                if (importedAssetCount == 0)
                {
                    report.Warn("Asset-Paketordner ist leer: " + packageFolder.Label + " (" + packageFolder.Path + ")");
                }
                else
                {
                    report.Pass("Asset-Paket gefunden: " + packageFolder.Label + " (" + importedAssetCount + " Eintraege)");
                }
            }
        }

        private static void ValidateMissingScripts(ValidationReport report)
        {
            if (!AssetDatabase.IsValidFolder(ProjectFolder))
            {
                report.Fail("Projektordner fuer die Missing-Script-Pruefung fehlt: " + ProjectFolder);
                return;
            }

            string[] scenePaths = FindOwnedAssets("t:Scene", ".unity");
            string[] prefabPaths = FindOwnedAssets("t:Prefab", ".prefab");
            List<string> missingScriptLocations = new List<string>();
            int checkedObjectCount = 0;
            int totalAssetCount = scenePaths.Length + prefabPaths.Length;
            int processedAssetCount = 0;
            Scene previouslyActiveScene = SceneManager.GetActiveScene();

            try
            {
                foreach (string prefabPath in prefabPaths)
                {
                    ShowProgress("Pruefe Prefab", prefabPath, processedAssetCount++, totalAssetCount);
                    try
                    {
                        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefabRoot == null)
                        {
                            report.Warn("Prefab konnte nicht gelesen werden: " + prefabPath);
                            continue;
                        }

                        checkedObjectCount += InspectHierarchy(prefabRoot, prefabPath, missingScriptLocations);
                    }
                    catch (Exception exception)
                    {
                        report.Warn("Prefab-Pruefung fehlgeschlagen: " + prefabPath + " (" + exception.Message + ")");
                    }
                }

                foreach (string scenePath in scenePaths)
                {
                    ShowProgress("Pruefe Szene", scenePath, processedAssetCount++, totalAssetCount);
                    InspectSceneReadOnly(scenePath, missingScriptLocations, ref checkedObjectCount, report);
                }
            }
            finally
            {
                if (previouslyActiveScene.IsValid() && previouslyActiveScene.isLoaded &&
                    SceneManager.GetActiveScene() != previouslyActiveScene)
                {
                    SceneManager.SetActiveScene(previouslyActiveScene);
                }

                EditorUtility.ClearProgressBar();
            }

            if (missingScriptLocations.Count == 0)
            {
                report.Pass(
                    "Keine fehlenden Script-Komponenten in " + scenePaths.Length + " Szenen und " +
                    prefabPaths.Length + " Prefabs unter " + ProjectFolder + " (" + checkedObjectCount + " GameObjects geprueft).");
                return;
            }

            report.Fail(missingScriptLocations.Count + " fehlende Script-Komponente(n) in projekt-eigenen Szenen/Prefabs gefunden:");
            const int maximumListedLocations = 30;
            foreach (string location in missingScriptLocations.Take(maximumListedLocations))
            {
                report.Detail("  - " + location);
            }

            if (missingScriptLocations.Count > maximumListedLocations)
            {
                report.Detail("  - ... und " + (missingScriptLocations.Count - maximumListedLocations) + " weitere");
            }
        }

        private static string[] FindOwnedAssets(string filter, string extension)
        {
            return AssetDatabase.FindAssets(filter, new[] { ProjectFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void InspectSceneReadOnly(
            string scenePath,
            List<string> missingScriptLocations,
            ref int checkedObjectCount,
            ValidationReport report)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedByValidator = !scene.IsValid() || !scene.isLoaded;

            try
            {
                if (openedByValidator)
                {
                    // Open additively, inspect, then close. There is intentionally no save call.
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    checkedObjectCount += InspectHierarchy(root, scenePath, missingScriptLocations);
                }
            }
            catch (Exception exception)
            {
                report.Warn("Szenenpruefung fehlgeschlagen: " + scenePath + " (" + exception.Message + ")");
            }
            finally
            {
                if (openedByValidator && scene.IsValid() && scene.isLoaded)
                {
                    // Discard any incidental edit-mode dirtiness caused by third-party ExecuteAlways scripts.
                    // Never write the inspected scene back to disk.
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static int InspectHierarchy(
            GameObject root,
            string assetPath,
            List<string> missingScriptLocations)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform currentTransform in transforms)
            {
                Component[] components = currentTransform.gameObject.GetComponents<Component>();
                int missingCount = components.Count(component => component == null);
                for (int index = 0; index < missingCount; index++)
                {
                    missingScriptLocations.Add(assetPath + " :: " + GetHierarchyPath(currentTransform));
                }
            }

            return transforms.Length;
        }

        private static string GetHierarchyPath(Transform current)
        {
            StringBuilder path = new StringBuilder(current.name);
            Transform parent = current.parent;
            while (parent != null)
            {
                path.Insert(0, parent.name + "/");
                parent = parent.parent;
            }

            return path.ToString();
        }

        private static void ShowProgress(string title, string assetPath, int processed, int total)
        {
            float progress = total > 0 ? (float)processed / total : 1f;
            EditorUtility.DisplayProgressBar("Wizzards Cauldron - Visual Setup", title + ": " + assetPath, progress);
        }

        private static void ValidateBuildSettings(ValidationReport report)
        {
            EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
            EditorBuildSettingsScene[] enabledScenes = configuredScenes.Where(scene => scene.enabled).ToArray();

            if (configuredScenes.Length == 0)
            {
                report.Warn("Build Settings enthalten keine Szenen.");
                return;
            }

            report.Info("Build Settings (werden durch diese Pruefung nicht veraendert):");
            foreach (EditorBuildSettingsScene configuredScene in configuredScenes)
            {
                report.Detail("  - " + (configuredScene.enabled ? "AKTIV" : "inaktiv") + ": " + configuredScene.path);
            }

            bool sampleEnabled = IsEnabled(enabledScenes, SampleScenePath);
            bool sourceEnabled = IsEnabled(enabledScenes, SourceScenePath);
            bool visualEnabled = IsEnabled(enabledScenes, VisualScenePath);
            bool onlySampleEnabled = enabledScenes.Length == 1 && sampleEnabled;

            if (onlySampleEnabled)
            {
                report.Warn("Build Settings: Derzeit ist faelschlicherweise nur SampleScene aktiviert; SCN_InteractionTest ist nicht aktiviert.");
            }
            else if (sampleEnabled && !sourceEnabled && !visualEnabled)
            {
                report.Warn("Build Settings: SampleScene ist aktiv, aber weder SCN_InteractionTest noch die visuelle Szenenkopie ist aktiv.");
            }
            else if (sourceEnabled || visualEnabled)
            {
                string activeProjectScene = visualEnabled ? VisualScenePath : SourceScenePath;
                report.Pass("Build Settings enthalten eine aktive Wizzards-Cauldron-Szene: " + activeProjectScene);
            }
            else
            {
                report.Warn("Build Settings enthalten keine aktive Wizzards-Cauldron-Szene.");
            }
        }

        private static bool IsEnabled(IEnumerable<EditorBuildSettingsScene> scenes, string expectedPath)
        {
            return scenes.Any(scene => string.Equals(
                scene.path.Replace('\\', '/'),
                expectedPath,
                StringComparison.OrdinalIgnoreCase));
        }

        private sealed class ExpectedModel
        {
            private readonly string[] keywords;

            public ExpectedModel(string label, params string[] keywords)
            {
                Label = label;
                this.keywords = keywords;
            }

            public string Label { get; private set; }

            public bool Matches(string fileName)
            {
                string normalizedName = fileName.Replace('_', ' ').Replace('-', ' ').ToLowerInvariant();
                return keywords.Any(keyword => normalizedName.Contains(keyword));
            }
        }

        private sealed class PackageFolder
        {
            public PackageFolder(string label, string path)
            {
                Label = label;
                Path = path;
            }

            public string Label { get; private set; }
            public string Path { get; private set; }
        }

        private sealed class ValidationReport
        {
            private readonly StringBuilder lines = new StringBuilder();

            public int PassedCount { get; private set; }
            public int WarningCount { get; private set; }
            public int FailedCount { get; private set; }

            public void Pass(string message)
            {
                PassedCount++;
                lines.AppendLine("[OK] " + message);
            }

            public void Warn(string message)
            {
                WarningCount++;
                lines.AppendLine("[WARNUNG] " + message);
            }

            public void Fail(string message)
            {
                FailedCount++;
                lines.AppendLine("[FEHLER] " + message);
            }

            public void Info(string message)
            {
                lines.AppendLine("[INFO] " + message);
            }

            public void Detail(string message)
            {
                lines.AppendLine(message);
            }

            public string BuildFullReport()
            {
                StringBuilder report = new StringBuilder();
                report.AppendLine("=== Wizzards Cauldron: Visual Setup Validation ===");
                report.AppendLine("Zeitpunkt: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                report.AppendLine();
                report.Append(lines);
                report.AppendLine();
                report.AppendLine(BuildSummaryLine());
                report.AppendLine("Die Pruefung hat keine Szenen, Prefabs oder Build Settings gespeichert oder veraendert.");
                return report.ToString();
            }

            public string BuildDialogSummary()
            {
                string status = FailedCount == 0 ? "Pruefung abgeschlossen." : "Pruefung mit offenen Punkten abgeschlossen.";
                return status + "\n\n" + BuildSummaryLine() + "\n\nDetails stehen in der Unity Console.";
            }

            private string BuildSummaryLine()
            {
                return "Ergebnis: " + PassedCount + " OK, " + WarningCount + " Warnung(en), " + FailedCount + " Fehler.";
            }
        }
    }
}
