using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;
using WizzardsCauldron.Presentation;
using WizzardsCauldron.UI;

namespace WizzardsCauldron.EditorTools
{
    internal static class CampaignContentInstaller
    {
        private const string VisualScenePath =
            "Assets/WizzardsCauldron/Scenes/" +
            "SCN_InteractionTest_Visual.unity";
        private const string PuzzleFolder =
            "Assets/WizzardsCauldron/Data/Puzzles";
        private const string PotionFolder =
            "Assets/WizzardsCauldron/Data/Potions";
        private const string ApprenticePuzzlePath =
            PuzzleFolder + "/SO_PuzzleApprentice.asset";
        private const string KnightPuzzlePath =
            PuzzleFolder + "/SO_PuzzleKnight.asset";
        private const string MasterPuzzlePath =
            PuzzleFolder + "/SO_PuzzleVisualExpanded.asset";

        private static readonly string[] RoundTitles =
        {
            "Apprentice Order",
            "Knight Order",
            "Master Trial"
        };

        [MenuItem(
            "Tools/Wizzards Cauldron/Install Campaign Content",
            priority = 210)]
        public static void InstallCampaignContent()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            InstallCampaignContentBatch();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Wizzards Cauldron",
                    "Three quest rounds, wand hint and star ratings " +
                    "were installed without moving scene objects.",
                    "OK");
            }
        }

        public static void InstallCampaignContentBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(
                VisualScenePath,
                OpenSceneMode.Single);
            InstallOrUpdate(scene);

            if (!EditorSceneManager.SaveScene(
                scene,
                VisualScenePath,
                false))
            {
                throw new InvalidOperationException(
                    "Unity could not save the visual campaign scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[WC_CAMPAIGN] INSTALL COMPLETE | rounds=3 | " +
                "positionsUnchanged=true | buildSettingsUnchanged=true");
        }

        internal static void InstallOrUpdate(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(
                    scene.path,
                    VisualScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Campaign content can only be installed in " +
                    VisualScenePath + ".");
            }

            TransformSnapshot[] transformsBefore =
                CaptureTransforms(scene);

            PotionDefinition green = LoadPotion("Green");
            PotionDefinition yellow = LoadPotion("Yellow");
            PotionDefinition blue = LoadPotion("Blue");
            PotionDefinition violet = LoadPotion("Violet");
            PotionDefinition orange = LoadPotion("Orange");

            PuzzleDefinition apprentice = CreateOrUpdatePuzzle(
                ApprenticePuzzlePath,
                4,
                new[] { green, yellow, blue });
            PuzzleDefinition knight = CreateOrUpdatePuzzle(
                KnightPuzzlePath,
                6,
                new[] { green, yellow, blue, violet, orange });
            PuzzleDefinition master =
                LoadRequiredAsset<PuzzleDefinition>(
                    MasterPuzzlePath);

            GameSessionController gameSession =
                FindUniqueInScene<GameSessionController>(scene);
            RoomResetCoordinator roomReset =
                FindUniqueInScene<RoomResetCoordinator>(scene);
            CauldronStatusPanel statusPanel =
                FindUniqueInScene<CauldronStatusPanel>(scene);
            ResultPanel resultPanel =
                FindUniqueInScene<ResultPanel>(scene);
            WandTip wandTip =
                FindUniqueInScene<WandTip>(scene);
            XRGrabInteractable wandInteractable =
                wandTip.GetComponentInParent<XRGrabInteractable>();
            if (wandInteractable == null)
            {
                throw new InvalidOperationException(
                    "The scene wand has no XRGrabInteractable.");
            }

            GameObject gameplayRoot = FindUniqueByName(
                scene,
                VisualPassGameplayExtension.GameplayRootName);
            PotionController[] potions = OrderScenePotions(
                scene,
                master);

            QuestCampaignController legacyCampaign =
                gameplayRoot.GetComponent<QuestCampaignController>();
            PotionHintFeedback legacyHintFeedback =
                gameplayRoot.GetComponent<PotionHintFeedback>();

            GameObject campaignRuntime = GetOrCreateChild(
                gameplayRoot.transform,
                "QuestCampaignRuntime");
            QuestCampaignController campaign =
                GetOrAddComponent<QuestCampaignController>(
                    campaignRuntime);
            PotionHintFeedback hintFeedback =
                GetOrAddComponent<PotionHintFeedback>(
                    campaignRuntime);
            WandHintInput wandHintInput =
                GetOrAddComponent<WandHintInput>(
                    wandInteractable.gameObject);

            for (int potionIndex = 0;
                 potionIndex < potions.Length;
                 potionIndex++)
            {
                PotionController potion = potions[potionIndex];
                XRGrabInteractable potionGrab =
                    potion.GetComponent<XRGrabInteractable>();
                if (potionGrab == null)
                {
                    throw new InvalidOperationException(
                        potion.name +
                        " has no XRGrabInteractable.");
                }

                PotionSpawnPoseStabilizer stabilizer =
                    GetOrAddComponent<
                        PotionSpawnPoseStabilizer>(
                            potion.gameObject);
                stabilizer.Configure(potion, potionGrab);
                EditorUtility.SetDirty(stabilizer);
            }

            campaign.Configure(
                gameSession,
                roomReset,
                new[] { apprentice, knight, master },
                RoundTitles,
                potions,
                8f);
            hintFeedback.Configure(campaign);
            wandHintInput.Configure(
                campaign,
                wandInteractable);
            // UI panels discover the generated campaign at runtime. Keeping
            // these serialized fields empty preserves the protected source
            // scene's existing component data.
            statusPanel.ConfigureCampaign(null);
            resultPanel.ConfigureCampaign(null);

            WandDifficultySelectionPanel selectionPanel =
                InstallDifficultySelection(
                    campaign,
                    statusPanel,
                    RoundTitles,
                    wandTip,
                    null);

            if (legacyCampaign != null &&
                legacyCampaign != campaign)
            {
                UnityEngine.Object.DestroyImmediate(
                    legacyCampaign);
            }

            if (legacyHintFeedback != null &&
                legacyHintFeedback != hintFeedback)
            {
                UnityEngine.Object.DestroyImmediate(
                    legacyHintFeedback);
            }

            EditorUtility.SetDirty(campaign);
            EditorUtility.SetDirty(hintFeedback);
            EditorUtility.SetDirty(wandHintInput);
            EditorUtility.SetDirty(statusPanel);
            EditorUtility.SetDirty(resultPanel);
            EditorUtility.SetDirty(selectionPanel);

            AssertTransformsUnchanged(transformsBefore);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(
                "[WC_CAMPAIGN] Configured Apprentice 3/4, Knight 5/6, " +
                "Master 7/8, one wand hint per round and star ratings.");
        }

        private static WandDifficultySelectionPanel
            InstallDifficultySelection(
                QuestCampaignController campaign,
                CauldronStatusPanel statusPanel,
                string[] titles,
                WandTip wandTip,
                Transform placementParent)
        {
            Canvas statusCanvas =
                statusPanel.GetComponentInParent<Canvas>();
            if (statusCanvas == null)
            {
                throw new InvalidOperationException(
                    "The cauldron status panel has no parent Canvas.");
            }

            Transform[] existingMatches = statusCanvas.gameObject.scene
                .GetRootGameObjects()
                .SelectMany(rootObject => rootObject
                    .GetComponentsInChildren<Transform>(true))
                .Where(item => item.name ==
                    "DifficultySelectionCanvas")
                .ToArray();
            if (existingMatches.Length > 1)
            {
                throw new InvalidOperationException(
                    "Multiple difficulty selection canvases exist.");
            }

            Transform existing = existingMatches.FirstOrDefault();
            GameObject root;

            if (existing == null)
            {
                root = new GameObject(
                    "DifficultySelectionCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(WandDifficultySelectionPanel));
                root.transform.SetParent(placementParent, false);
                SceneManager.MoveGameObjectToScene(
                    root,
                    statusCanvas.gameObject.scene);
                CopyCanvasTransform(
                    (RectTransform)statusCanvas.transform,
                    (RectTransform)root.transform);
            }
            else
            {
                root = existing.gameObject;
                root.transform.SetParent(placementParent, true);
                CopyCanvasTransform(
                    (RectTransform)statusCanvas.transform,
                    (RectTransform)root.transform);
            }

            Canvas canvas = GetOrAddComponent<Canvas>(root);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = statusCanvas.sortingOrder + 1;

            CanvasScaler scaler =
                GetOrAddComponent<CanvasScaler>(root);
            scaler.dynamicPixelsPerUnit = 10f;

            TMP_Text fontSource = statusCanvas
                .GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault();

            Transform contentTransform = root.transform.Find(
                "SelectionContent");
            GameObject content;
            if (contentTransform == null)
            {
                content = CreateSelectionContent(
                    root.transform,
                    campaign,
                    titles,
                    wandTip,
                    fontSource);
            }
            else
            {
                content = contentTransform.gameObject;
            }

            WandDifficultySelector[] selectors = content
                .GetComponentsInChildren<
                    WandDifficultySelector>(true)
                .OrderBy(selector => selector.name)
                .ToArray();

            string[] details =
            {
                "3 potions | Capacity 4",
                "5 potions | Capacity 6",
                "7 potions | Capacity 8"
            };

            for (int index = 0;
                 index < selectors.Length && index < titles.Length;
                 index++)
            {
                TMP_Text label = selectors[index]
                    .GetComponentInChildren<TMP_Text>(true);
                BoxCollider legacyCollider =
                    selectors[index].GetComponent<BoxCollider>();
                if (legacyCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        legacyCollider);
                }

                selectors[index].Configure(
                    campaign,
                    index,
                    label,
                    wandTip,
                    titles[index],
                    details[index]);
                EditorUtility.SetDirty(selectors[index]);
            }

            WandDifficultySelectionPanel panel =
                GetOrAddComponent<
                    WandDifficultySelectionPanel>(root);
            panel.Configure(
                campaign,
                content,
                statusCanvas,
                selectors);

            content.SetActive(false);
            return panel;
        }

        private static GameObject CreateSelectionContent(
            Transform parent,
            QuestCampaignController campaign,
            string[] titles,
            WandTip wandTip,
            TMP_Text fontSource)
        {
            GameObject content = new GameObject(
                "SelectionContent",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform contentRect =
                (RectTransform)content.transform;
            contentRect.SetParent(parent, false);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            Image background = content.GetComponent<Image>();
            background.color = new Color(
                0.035f,
                0.008f,
                0.055f,
                0.96f);
            background.raycastTarget = false;

            CreateSelectionText(
                content.transform,
                "SelectionTitle",
                "CHOOSE DIFFICULTY\nTouch a plaque with the wand",
                new Vector2(0f, 130f),
                new Vector2(720f, 80f),
                30f,
                Color.white,
                fontSource);

            Color[] colors =
            {
                new Color(0.08f, 0.32f, 0.18f, 0.96f),
                new Color(0.45f, 0.22f, 0.04f, 0.96f),
                new Color(0.30f, 0.08f, 0.35f, 0.96f)
            };
            string[] details =
            {
                "3 potions | Capacity 4",
                "5 potions | Capacity 6",
                "7 potions | Capacity 8"
            };

            for (int index = 0; index < 3; index++)
            {
                GameObject plaque = new GameObject(
                    index.ToString("D2") + "_Difficulty",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(WandDifficultySelector));
                RectTransform plaqueRect =
                    (RectTransform)plaque.transform;
                plaqueRect.SetParent(content.transform, false);
                plaqueRect.anchorMin = new Vector2(0.5f, 0.5f);
                plaqueRect.anchorMax = new Vector2(0.5f, 0.5f);
                plaqueRect.pivot = new Vector2(0.5f, 0.5f);
                plaqueRect.sizeDelta = new Vector2(215f, 190f);
                plaqueRect.anchoredPosition = new Vector2(
                    -240f + index * 240f,
                    -40f);

                Image plaqueImage = plaque.GetComponent<Image>();
                plaqueImage.color = colors[index];
                plaqueImage.raycastTarget = false;

                TMP_Text label = CreateSelectionText(
                    plaque.transform,
                    "Label",
                    titles[index] + "\n" + details[index] +
                    "\nNot completed",
                    Vector2.zero,
                    new Vector2(195f, 170f),
                    23f,
                    Color.white,
                    fontSource);

                plaque.GetComponent<WandDifficultySelector>()
                    .Configure(
                        campaign,
                        index,
                        label,
                        wandTip,
                        titles[index],
                        details[index]);
            }

            return content;
        }

        private static TMP_Text CreateSelectionText(
            Transform parent,
            string objectName,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            Color color,
            TMP_Text fontSource)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            RectTransform rect =
                (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI label =
                textObject.GetComponent<TextMeshProUGUI>();
            if (fontSource != null)
            {
                label.font = fontSource.font;
            }

            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static void CopyCanvasTransform(
            RectTransform source,
            RectTransform destination)
        {
            destination.anchorMin = new Vector2(0.5f, 0.5f);
            destination.anchorMax = new Vector2(0.5f, 0.5f);
            destination.pivot = new Vector2(0.5f, 0.5f);
            destination.SetPositionAndRotation(
                source.position,
                source.rotation);

            Vector3 parentScale = destination.parent != null
                ? destination.parent.lossyScale
                : Vector3.one;
            Vector3 sourceScale = source.lossyScale;
            destination.localScale = new Vector3(
                sourceScale.x / parentScale.x,
                sourceScale.y / parentScale.y,
                sourceScale.z / parentScale.z);
            destination.sizeDelta = new Vector2(760f, 360f);
        }

        private static GameObject GetOrCreateChild(
            Transform parent,
            string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static PuzzleDefinition CreateOrUpdatePuzzle(
            string path,
            int capacity,
            PotionDefinition[] potions)
        {
            PuzzleDefinition puzzle =
                AssetDatabase.LoadAssetAtPath<PuzzleDefinition>(path);
            if (puzzle == null)
            {
                puzzle = ScriptableObject.CreateInstance<
                    PuzzleDefinition>();
                puzzle.name = System.IO.Path.GetFileNameWithoutExtension(
                    path);
                AssetDatabase.CreateAsset(puzzle, path);
            }

            var serialized = new SerializedObject(puzzle);
            serialized.FindProperty("_cauldronCapacity").intValue =
                capacity;
            SerializedProperty potionProperty =
                serialized.FindProperty("_potions");
            potionProperty.arraySize = potions.Length;

            for (int index = 0;
                 index < potions.Length;
                 index++)
            {
                potionProperty.GetArrayElementAtIndex(index)
                    .objectReferenceValue = potions[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(puzzle);

            if (!puzzle.TryValidate(out string error))
            {
                throw new InvalidOperationException(
                    "Generated puzzle is invalid: " + error);
            }

            return puzzle;
        }

        private static PotionDefinition LoadPotion(string colorName)
        {
            return LoadRequiredAsset<PotionDefinition>(
                PotionFolder + "/SO_Potion" + colorName + ".asset");
        }

        private static T LoadRequiredAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "Required asset is missing: " + path);
            }

            return asset;
        }

        private static PotionController[] OrderScenePotions(
            Scene scene,
            PuzzleDefinition masterPuzzle)
        {
            PotionController[] scenePotions =
                FindInScene<PotionController>(scene);
            var result = new PotionController[
                masterPuzzle.Potions.Count];

            for (int definitionIndex = 0;
                 definitionIndex < masterPuzzle.Potions.Count;
                 definitionIndex++)
            {
                PotionDefinition definition =
                    masterPuzzle.Potions[definitionIndex];
                PotionController[] matches = scenePotions
                    .Where(potion =>
                        potion.Definition == definition)
                    .ToArray();

                if (matches.Length != 1)
                {
                    throw new InvalidOperationException(
                        "Expected exactly one scene potion for '" +
                        definition.StableId + "', found " +
                        matches.Length + ".");
                }

                result[definitionIndex] = matches[0];
            }

            return result;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : target.AddComponent<T>();
        }

        private static T FindUniqueInScene<T>(Scene scene)
            where T : Component
        {
            T[] matches = FindInScene<T>(scene);
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + typeof(T).Name +
                    ", found " + matches.Length + ".");
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

        private static GameObject FindUniqueByName(
            Scene scene,
            string objectName)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == objectName)
                .Select(item => item.gameObject)
                .ToArray();

            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one object named '" +
                    objectName + "', found " + matches.Length + ".");
            }

            return matches[0];
        }

        private static TransformSnapshot[] CaptureTransforms(
            Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform
                    .GetComponentInParent<
                        WandDifficultySelectionPanel>(true) == null)
                .Select(transform =>
                    new TransformSnapshot(transform))
                .ToArray();
        }

        private static void AssertTransformsUnchanged(
            TransformSnapshot[] snapshots)
        {
            for (int index = 0;
                 index < snapshots.Length;
                 index++)
            {
                snapshots[index].AssertUnchanged();
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform _transform;
            private readonly Vector3 _localPosition;
            private readonly Quaternion _localRotation;
            private readonly Vector3 _localScale;
            private readonly string _name;

            internal TransformSnapshot(Transform transform)
            {
                _transform = transform;
                _localPosition = transform.localPosition;
                _localRotation = transform.localRotation;
                _localScale = transform.localScale;
                _name = transform.name;
            }

            internal void AssertUnchanged()
            {
                if (_transform == null)
                {
                    throw new InvalidOperationException(
                        "Campaign installation removed an existing object: " +
                        _name + ".");
                }

                bool unchanged =
                    Vector3.SqrMagnitude(
                        _transform.localPosition -
                        _localPosition) < 0.000000001f &&
                    Quaternion.Angle(
                        _transform.localRotation,
                        _localRotation) < 0.0001f &&
                    Vector3.SqrMagnitude(
                        _transform.localScale -
                        _localScale) < 0.000000001f;

                if (!unchanged)
                {
                    throw new InvalidOperationException(
                        "Campaign installation changed the transform of '" +
                        _name + "'.");
                }
            }
        }
    }
}
