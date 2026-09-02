using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WizzardsCauldron.EditorTools
{
    /// <summary>
    /// Adds only the project-owned Quest input helper to the existing visual
    /// scene. This deliberately avoids rebuilding or repositioning scene art.
    /// </summary>
    public static class QuestControlsInstaller
    {
        private const string VisualScenePath =
            "Assets/WizzardsCauldron/Scenes/SCN_InteractionTest_Visual.unity";

        [MenuItem(
            "Tools/Wizzards Cauldron/Install Quest Controls",
            priority = 210)]
        public static void InstallQuestControls()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            InstallQuestControlsBatch();
        }

        public static void InstallQuestControlsBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(
                VisualScenePath,
                OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Unity could not open the visual target scene.");
            }

            GameObject[] roots = scene.GetRootGameObjects()
                .Where(root => string.Equals(
                    root.name,
                    VisualPassGameplayExtension.GameplayRootName,
                    StringComparison.Ordinal))
                .ToArray();
            if (roots.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one gameplay-extension root, found " +
                    roots.Length + ".");
            }

            VisualPassGameplayExtension.CreateQuestFaceButtonBindings(
                roots[0].transform);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(
                scene,
                VisualScenePath,
                false))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Quest control addition.");
            }

            Debug.Log(
                "[WC_QUEST_CONTROLS] INSTALL COMPLETE | only=" +
                VisualPassGameplayExtension.GameplayRootName + "/" +
                VisualPassGameplayExtension.QuestFaceButtonBindingsName +
                " | layoutUnchanged=true | buildSettingsUnchanged=true");
        }
    }
}
