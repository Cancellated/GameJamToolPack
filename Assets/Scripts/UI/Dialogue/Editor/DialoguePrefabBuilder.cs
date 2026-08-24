using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MyGame.UI.Dialogue.Controller;
using MyGame.UI.Dialogue.View;

namespace MyGame.UI.Dialogue.EditorTools
{
    /// <summary>
    /// 编辑器工具：一键重建 DialoguePanel.prefab 与 DialogueChoiceButton.prefab。
    /// 生成完整可见层级（底部对话栏/说话人/台词/选项列/继续/跳过），
    /// 并自动接线 DialoguePanelView 的全部序列化字段。
    /// 生成后请确认 UIConfig 中存在 panelType=DialoguePanel 的条目（addressableAddress 指向本预制体）。
    /// </summary>
    public static class DialoguePrefabBuilder
    {
        private const string MENU_ITEM = "Tools/Game Jam Tool Pack/Rebuild Dialogue Prefabs";
        private const string DIALOGUE_FOLDER = "Assets/Art Assets/UI/Dialogue";
        private const string DIALOGUE_PANEL_PATH = DIALOGUE_FOLDER + "/DialoguePanel.prefab";
        private const string DIALOGUE_CHOICE_BUTTON_PATH = DIALOGUE_FOLDER + "/DialogueChoiceButton.prefab";
        private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static TMP_FontAsset s_fontAsset;

        [MenuItem(MENU_ITEM)]
        public static void RebuildAll()
        {
            EnsureFolder();

            s_fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (s_fontAsset == null)
            {
                Debug.LogError($"[DialoguePrefabBuilder] 找不到 TMP 字体: {FONT_PATH}");
                return;
            }

            GameObject choiceButtonPrefab = BuildChoiceButtonPrefab();
            GameObject choiceButtonAsset = PrefabUtility.SaveAsPrefabAsset(choiceButtonPrefab, DIALOGUE_CHOICE_BUTTON_PATH);
            Object.DestroyImmediate(choiceButtonPrefab);

            GameObject panelPrefab = BuildDialoguePanelPrefab(choiceButtonAsset);
            GameObject panelAsset = PrefabUtility.SaveAsPrefabAsset(panelPrefab, DIALOGUE_PANEL_PATH);
            Object.DestroyImmediate(panelPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = panelAsset != null ? panelAsset : choiceButtonAsset;
            Debug.Log($"[DialoguePrefabBuilder] 完成:\n{DIALOGUE_PANEL_PATH}\n{DIALOGUE_CHOICE_BUTTON_PATH}\n" +
                      "请确认 UIConfig 中 DialoguePanel 条目的 addressableAddress 为: " +
                      "Assets/Art Assets/UI/Dialogue/DialoguePanel.prefab");
        }

        [MenuItem(MENU_ITEM, true)]
        private static bool ValidateRebuildAll()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void EnsureFolder()
        {
            if (!Directory.Exists(DIALOGUE_FOLDER))
            {
                Directory.CreateDirectory(DIALOGUE_FOLDER);
            }
        }

        #region 选项按钮预制体

        private static GameObject BuildChoiceButtonPrefab()
        {
            GameObject root = new("DialogueChoiceButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            SetSizeDelta((RectTransform)root.transform, new Vector2(460f, 52f));

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.16f, 0.17f, 0.2f, 0.98f);

            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;

            TextMeshProUGUI label = CreateTMPText("Text (TMP)", root.transform,
                "选项", 20, TextAlignmentOptions.MidlineLeft, Color.white);
            SetStretchRect((RectTransform)label.transform, new Vector2(16f, 4f), new Vector2(-16f, -4f));

            return root;
        }

        #endregion

        #region 对话面板预制体

        private static GameObject BuildDialoguePanelPrefab(GameObject choiceButtonAsset)
        {
            GameObject root = new("DialoguePanel", typeof(RectTransform));
            SetStretchRect((RectTransform)root.transform, Vector2.zero, Vector2.zero);

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 全屏背板：视觉压暗 + 点击蒙版（无选项节点时点击任意位置继续）
            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.45f);
            backdrop.raycastTarget = true;

            Button backdropButton = root.AddComponent<Button>();
            backdropButton.targetGraphic = backdrop;

            // 底部对话栏
            GameObject dialogueBox = new("DialogueBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dialogueBox.transform.SetParent(root.transform, false);
            RectTransform boxRect = (RectTransform)dialogueBox.transform;
            boxRect.anchorMin = new Vector2(0f, 0f);
            boxRect.anchorMax = new Vector2(1f, 0f);
            boxRect.pivot = new Vector2(0.5f, 0f);
            boxRect.anchoredPosition = new Vector2(0f, 28f);
            boxRect.sizeDelta = new Vector2(-80f, 260f);
            Image boxImage = dialogueBox.GetComponent<Image>();
            boxImage.color = new Color(0.07f, 0.08f, 0.11f, 0.98f);
            // 台词区点击应落到全屏蒙版（任意位置继续），不拦截 Continue/Skip/选项按钮
            boxImage.raycastTarget = false;

            // 说话人
            TextMeshProUGUI speakerText = CreateTMPText("SpeakerName", dialogueBox.transform,
                "说话人", 22, TextAlignmentOptions.MidlineLeft, new Color(0.98f, 0.85f, 0.42f, 1f));
            RectTransform speakerRect = (RectTransform)speakerText.transform;
            speakerRect.anchorMin = new Vector2(0f, 1f);
            speakerRect.anchorMax = new Vector2(0f, 1f);
            speakerRect.pivot = new Vector2(0f, 1f);
            speakerRect.anchoredPosition = new Vector2(28f, -16f);
            speakerRect.sizeDelta = new Vector2(520f, 36f);

            // 台词：顶部留出说话人栏（上边距 64px），避免正文与说话人重叠
            TextMeshProUGUI dialogueText = CreateTMPText("DialogueText", dialogueBox.transform,
                "这是一句示例台词。运行 Tools/Game Jam Tool Pack/Rebuild Dialogue Prefabs 后，本面板由运行时对话系统驱动。",
                24, TextAlignmentOptions.TopLeft, new Color(0.93f, 0.93f, 0.95f, 1f));
            RectTransform dialogueRect = (RectTransform)dialogueText.transform;
            dialogueRect.anchorMin = Vector2.zero;
            dialogueRect.anchorMax = Vector2.one;
            dialogueRect.offsetMin = new Vector2(28f, 64f);
            dialogueRect.offsetMax = new Vector2(-28f, -64f);

            // 跳过按钮
            Button skipButton = CreateTMPButton("SkipButton", dialogueBox.transform,
                "跳过", new Color(0.28f, 0.28f, 0.3f, 1f));
            RectTransform skipRect = (RectTransform)skipButton.transform;
            skipRect.anchorMin = new Vector2(1f, 1f);
            skipRect.anchorMax = new Vector2(1f, 1f);
            skipRect.pivot = new Vector2(1f, 1f);
            skipRect.anchoredPosition = new Vector2(-24f, -16f);
            skipRect.sizeDelta = new Vector2(110f, 40f);

            // 选项容器：屏幕中央纵向居中排列（运行时由 DialoguePanelView 实例化选项按钮）
            GameObject choicesContainer = new("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            choicesContainer.transform.SetParent(root.transform, false);
            RectTransform choicesRect = (RectTransform)choicesContainer.transform;
            choicesRect.anchorMin = new Vector2(0.5f, 0.5f);
            choicesRect.anchorMax = new Vector2(0.5f, 0.5f);
            choicesRect.pivot = new Vector2(0.5f, 0.5f);
            choicesRect.anchoredPosition = new Vector2(0f, 400f);
            choicesRect.sizeDelta = new Vector2(560f, 520f);

            VerticalLayoutGroup choicesLayout = choicesContainer.GetComponent<VerticalLayoutGroup>();
            choicesLayout.padding = new RectOffset(0, 0, 0, 0);
            choicesLayout.spacing = 12f;
            choicesLayout.childAlignment = TextAnchor.MiddleCenter;
            choicesLayout.childControlWidth = false;
            choicesLayout.childControlHeight = false;
            choicesLayout.childForceExpandWidth = false;
            choicesLayout.childForceExpandHeight = false;
            choicesContainer.SetActive(false);

            DialoguePanelView view = root.AddComponent<DialoguePanelView>();
            SerializedObject viewSo = new(view);
            viewSo.FindProperty("m_canvasGroup").objectReferenceValue = canvasGroup;
            viewSo.FindProperty("m_panelType").enumValueIndex = (int)UIType.DialoguePanel;
            viewSo.FindProperty("m_fadeDuration").floatValue = 0.3f;
            viewSo.FindProperty("m_speakerNameText").objectReferenceValue = speakerText;
            viewSo.FindProperty("m_dialogueText").objectReferenceValue = dialogueText;
            viewSo.FindProperty("m_backdropButton").objectReferenceValue = backdropButton;
            viewSo.FindProperty("m_choicesContainer").objectReferenceValue = choicesRect;
            viewSo.FindProperty("m_skipButton").objectReferenceValue = skipButton;
            viewSo.FindProperty("m_choiceButtonPrefab").objectReferenceValue = choiceButtonAsset;
            viewSo.FindProperty("m_charactersPerSecond").floatValue = 40f;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            // 控制器同物体预置：View.TryBindController 会优先找到它，不重复创建
            root.AddComponent<DialoguePanelController>();

            return root;
        }

        #endregion

        #region 通用 UI 辅助

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = s_fontAsset;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;

            SetStretchRect((RectTransform)tmp.transform, Vector2.zero, Vector2.zero);
            return tmp;
        }

        private static Button CreateTMPButton(string name, Transform parent, string label, Color backgroundColor)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            SetStretchRect((RectTransform)go.transform, Vector2.zero, Vector2.zero);

            Image image = go.GetComponent<Image>();
            image.color = backgroundColor;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            CreateTMPText("Text (TMP)", go.transform, label, 20, TextAlignmentOptions.Midline, Color.white);
            return button;
        }

        private static void SetStretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetSizeDelta(RectTransform rect, Vector2 sizeDelta)
        {
            rect.sizeDelta = sizeDelta;
        }

        #endregion
    }
}
