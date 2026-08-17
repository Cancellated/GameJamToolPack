using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MyGame.Data;
using MyGame.UI.SaveLoad.Controller;
using MyGame.UI.SaveLoad.View;

namespace MyGame.UI.SaveLoad.EditorTools
{
    /// <summary>
    /// 编辑器工具：一键重建完整的 SaveLoadMenu.prefab 与 SaveSlotPrefab.prefab。
    /// 生成物包含完整可见层级（标题/槽位容器/操作栏/确认弹窗/返回按钮），
    /// 并自动接线 SaveLoadMenuPanel、SaveLoadMenuController 的序列化字段。
    /// </summary>
    public static class SaveLoadMenuPrefabBuilder
    {
        private const string MENU_ITEM = "Tools/Game Jam Tool Pack/Rebuild SaveLoad Prefabs";

        private const string SAVE_LOAD_FOLDER = "Assets/Art Assets/UI/SaveLoad";
        private const string SAVE_LOAD_MENU_PATH = SAVE_LOAD_FOLDER + "/SaveLoadMenu.prefab";
        private const string SAVE_SLOT_PATH = SAVE_LOAD_FOLDER + "/SaveSlotPrefab.prefab";
        private const string CONFIG_PATH = "Assets/Config/SaveLoadMenuConfig.asset";
        private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static TMP_FontAsset s_fontAsset;

        [MenuItem(MENU_ITEM)]
        public static void RebuildAll()
        {
            EnsureFolder();
            s_fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (s_fontAsset == null)
            {
                Debug.LogError($"[SaveLoadPrefabBuilder] 找不到 TMP 字体: {FONT_PATH}");
                return;
            }

            SaveLoadMenuConfig config = AssetDatabase.LoadAssetAtPath<SaveLoadMenuConfig>(CONFIG_PATH);
            if (config == null)
            {
                Debug.LogError($"[SaveLoadPrefabBuilder] 找不到存档菜单配置: {CONFIG_PATH}");
            }

            GameObject slotPrefab = BuildSaveSlotPrefab();
            GameObject slotPrefabAsset = PrefabUtility.SaveAsPrefabAsset(slotPrefab, SAVE_SLOT_PATH);
            Object.DestroyImmediate(slotPrefab);

            GameObject menuPrefab = BuildSaveLoadMenuPrefab(config, slotPrefabAsset);
            GameObject menuPrefabAsset = PrefabUtility.SaveAsPrefabAsset(menuPrefab, SAVE_LOAD_MENU_PATH);
            Object.DestroyImmediate(menuPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = menuPrefabAsset != null ? menuPrefabAsset : slotPrefabAsset;
            Debug.Log($"[SaveLoadPrefabBuilder] 完成:\n{SAVE_LOAD_MENU_PATH}\n{SAVE_SLOT_PATH}");
        }

        [MenuItem(MENU_ITEM, true)]
        private static bool ValidateRebuildAll()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void EnsureFolder()
        {
            if (!Directory.Exists(SAVE_LOAD_FOLDER))
            {
                Directory.CreateDirectory(SAVE_LOAD_FOLDER);
            }
        }

        #region SaveSlotPrefab

        private static GameObject BuildSaveSlotPrefab()
        {
            GameObject root = new("SaveSlot", typeof(RectTransform));
            SetRect((RectTransform)root.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 64f));

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 56f;
            layout.preferredHeight = 64f;

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            background.raycastTarget = true;

            Button button = root.AddComponent<Button>();
            button.targetGraphic = background;

            TextMeshProUGUI nameText = CreateTMPText("Name", root.transform,
                "存档槽", 20, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.12f, 0.12f, 1f));
            SetStretchRect((RectTransform)nameText.transform, new Vector2(16f, 30f), new Vector2(-240f, -6f));

            TextMeshProUGUI timeText = CreateTMPText("Time", root.transform,
                "存档时间", 16, TextAlignmentOptions.MidlineLeft, new Color(0.25f, 0.25f, 0.25f, 1f));
            SetStretchRect((RectTransform)timeText.transform, new Vector2(16f, 6f), new Vector2(-240f, -28f));

            TextMeshProUGUI progressText = CreateTMPText("Progress", root.transform,
                "关卡进度", 16, TextAlignmentOptions.MidlineRight, new Color(0.3f, 0.3f, 0.3f, 1f));
            SetStretchRect((RectTransform)progressText.transform, new Vector2(460f, 6f), new Vector2(-16f, -28f));

            GameObject highlight = new("Highlight", typeof(RectTransform));
            highlight.transform.SetParent(root.transform, false);
            SetStretchRect((RectTransform)highlight.transform, Vector2.zero, Vector2.zero);
            Image highlightImage = highlight.AddComponent<Image>();
            highlightImage.color = new Color(0.35f, 0.72f, 0.45f, 0.45f);
            highlightImage.raycastTarget = false;
            highlightImage.enabled = false;

            SaveSlotUIImplementation slotUI = root.AddComponent<SaveSlotUIImplementation>();
            SerializedObject so = new(slotUI);
            so.FindProperty("_slotNameText").objectReferenceValue = nameText;
            so.FindProperty("_saveTimeText").objectReferenceValue = timeText;
            so.FindProperty("_gameProgressText").objectReferenceValue = progressText;
            so.FindProperty("_highlightImage").objectReferenceValue = highlightImage;
            so.FindProperty("_slotButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        /// <summary>
        /// 在槽位容器中放置一个预览槽（仅编辑器预览使用，运行时会被清除）。
        /// </summary>
        private static void CreatePreviewSlot(Transform parent, GameObject slotPrefabAsset,
            string objectName, string displayName, string timeText, string progressText, bool highlighted)
        {
            GameObject slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefabAsset);
            if (slot == null)
            {
                return;
            }

            slot.name = objectName;
            slot.transform.SetParent(parent, false);

            SetChildText(slot.transform, "Name", displayName);
            SetChildText(slot.transform, "Time", timeText);
            SetChildText(slot.transform, "Progress", progressText);

            Transform highlightTransform = slot.transform.Find("Highlight");
            if (highlightTransform != null)
            {
                Image highlightImage = highlightTransform.GetComponent<Image>();
                if (highlightImage != null)
                {
                    highlightImage.enabled = highlighted;
                }
            }
        }

        private static void SetChildText(Transform parent, string childName, string text)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
            }
        }

        #endregion

        #region SaveLoadMenuPrefab

        private static GameObject BuildSaveLoadMenuPrefab(SaveLoadMenuConfig config, GameObject slotPrefabAsset)
        {
            GameObject root = new("SaveLoadMenu", typeof(RectTransform));
            SetStretchRect((RectTransform)root.transform, Vector2.zero, Vector2.zero);

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.07f, 0.07f, 0.09f, 0.94f);

            // 标题
            TextMeshProUGUI title = CreateTMPText("Title", root.transform,
                "存档/读档菜单", 36, TextAlignmentOptions.Midline, Color.white);
            SetRect((RectTransform)title.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(480f, 60f));

            // 槽位容器：占满除顶部标题外的区域，详情面板以弹层覆盖
            GameObject slotLayout = new("SlotLayout", typeof(RectTransform), typeof(VerticalLayoutGroup));
            slotLayout.transform.SetParent(root.transform, false);
            RectTransform slotLayoutRect = (RectTransform)slotLayout.transform;
            slotLayoutRect.anchorMin = Vector2.zero;
            slotLayoutRect.anchorMax = Vector2.one;
            slotLayoutRect.offsetMin = new Vector2(32f, 32f);
            slotLayoutRect.offsetMax = new Vector2(-32f, -96f);
            VerticalLayoutGroup vertical = slotLayout.GetComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(4, 4, 4, 4);
            vertical.spacing = 6f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            // 放置预览占位槽：让 Prefab Mode 直接看到槽位长什么样。
            // 运行时 SaveLoadMenuView.ClearSaveSlotUIs 会清空这些预览子物体，再实例化真实槽位。
            if (slotPrefabAsset != null)
            {
                CreatePreviewSlot(slotLayout.transform, slotPrefabAsset, "PreviewSlot_auto_save",
                    "自动存档", "2026-08-17 10:00:00", "关卡：1  完成：0个", true);
                CreatePreviewSlot(slotLayout.transform, slotPrefabAsset, "PreviewSlot_save_1",
                    "存档槽 1", "空存档槽", string.Empty, false);
                CreatePreviewSlot(slotLayout.transform, slotPrefabAsset, "PreviewSlot_save_2",
                    "存档槽 2", "空存档槽", string.Empty, false);
            }

            // 槽位详情弹层：点开槽位后居中显示详情与操作
            GameObject detailsPanel = CreateSlotDetailsPanel(root.transform, out TextMeshProUGUI detailText);
            Button newGameButton = detailsPanel.transform.Find("Buttons/Btn_NewGame").GetComponent<Button>();
            Button saveButton = detailsPanel.transform.Find("Buttons/Btn_Save").GetComponent<Button>();
            Button loadButton = detailsPanel.transform.Find("Buttons/Btn_Load").GetComponent<Button>();
            Button deleteButton = detailsPanel.transform.Find("Buttons/Btn_Delete").GetComponent<Button>();
            Button cancelButton = detailsPanel.transform.Find("Buttons/Btn_Cancel").GetComponent<Button>();

            // 返回主菜单：左上角（新游戏不再占用主界面，只在空槽详情面板中显示）
            Button backButton = CreateTMPButton("Btn_Back", root.transform,
                "返回主菜单", new Color(0.24f, 0.3f, 0.38f, 1f));
            SetRect((RectTransform)backButton.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(200f, 48f));

            // 确认弹窗
            GameObject confirmDialog = BuildConfirmDialog(root.transform);

            SaveLoadMenuPanel panel = root.AddComponent<SaveLoadMenuPanel>();
            SerializedObject panelSo = new(panel);
            panelSo.FindProperty("m_canvasGroup").objectReferenceValue = canvasGroup;
            panelSo.FindProperty("m_panelType").enumValueIndex = (int)UIType.SaveLoadMenu;
            panelSo.FindProperty("m_fadeDuration").floatValue = 0.3f;
            panelSo.FindProperty("saveSlotsContainer").objectReferenceValue = slotLayout.transform;
            panelSo.FindProperty("saveOptionsMenu").objectReferenceValue = detailsPanel;
            panelSo.FindProperty("saveButton").objectReferenceValue = saveButton;
            panelSo.FindProperty("loadButton").objectReferenceValue = loadButton;
            panelSo.FindProperty("deleteButton").objectReferenceValue = deleteButton;
            panelSo.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            panelSo.FindProperty("newGameButton").objectReferenceValue = newGameButton;
            panelSo.FindProperty("backButton").objectReferenceValue = backButton;
            panelSo.FindProperty("confirmDialog").objectReferenceValue = confirmDialog;
            panelSo.FindProperty("confirmTitleText").objectReferenceValue = confirmDialog.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            panelSo.FindProperty("confirmMessageText").objectReferenceValue = confirmDialog.transform.Find("Content").GetComponent<TextMeshProUGUI>();
            panelSo.FindProperty("confirmButton").objectReferenceValue = confirmDialog.transform.Find("Confirm").GetComponent<Button>();
            panelSo.FindProperty("confirmCancelButton").objectReferenceValue = confirmDialog.transform.Find("Cancel").GetComponent<Button>();
            panelSo.FindProperty("_menuTitleText").objectReferenceValue = title;
            panelSo.FindProperty("_selectedSlotInfoText").objectReferenceValue = detailText;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            SaveLoadMenuController controller = root.AddComponent<SaveLoadMenuController>();
            SerializedObject controllerSo = new(controller);
            controllerSo.FindProperty("_config").objectReferenceValue = config;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject BuildConfirmDialog(Transform parent)
        {
            GameObject dialog = new("ConfirmDialog", typeof(RectTransform));
            dialog.transform.SetParent(parent, false);
            SetRect((RectTransform)dialog.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 300f));

            Image dialogBackground = dialog.AddComponent<Image>();
            dialogBackground.color = new Color(0.13f, 0.13f, 0.15f, 0.98f);

            TextMeshProUGUI title = CreateTMPText("Title", dialog.transform,
                "确认", 26, TextAlignmentOptions.Midline, Color.white);
            SetRect((RectTransform)title.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(420f, 48f));

            TextMeshProUGUI content = CreateTMPText("Content", dialog.transform,
                "确定要这样做吗？", 18, TextAlignmentOptions.Midline, Color.white);
            SetRect((RectTransform)content.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(-80f, -80f));

            Button confirm = CreateTMPButton("Confirm", dialog.transform,
                "确定", new Color(0.18f, 0.42f, 0.72f, 1f));
            SetRect((RectTransform)confirm.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-120f, 24f), new Vector2(180f, 48f));

            Button cancel = CreateTMPButton("Cancel", dialog.transform,
                "取消", new Color(0.32f, 0.32f, 0.34f, 1f));
            SetRect((RectTransform)cancel.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(120f, 24f), new Vector2(180f, 48f));

            // 保持激活：Prefab 预览时可以直接看到确认面板；
            // 运行时由 SaveLoadMenuView.Initialize 调用 HideConfirmationDialog 关闭
            dialog.SetActive(true);
            return dialog;
        }

        /// <summary>
        /// 槽位详情弹层：居中卡片，包含详情文本与保存/读取/删除/取消按钮。
        /// </summary>
        private static GameObject CreateSlotDetailsPanel(Transform parent, out TextMeshProUGUI detailText)
        {
            GameObject panel = new("SlotDetailsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            SetRect((RectTransform)panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 400f));

            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);

            TextMeshProUGUI title = CreateTMPText("Title", panel.transform,
                "存档详情", 26, TextAlignmentOptions.Midline, Color.white);
            SetRect((RectTransform)title.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(520f, 52f));

            detailText = CreateTMPText("DetailText", panel.transform,
                "请选择一个存档", 18, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.9f, 0.9f, 1f));
            RectTransform detailRect = (RectTransform)detailText.transform;
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = new Vector2(28f, 88f);
            detailRect.offsetMax = new Vector2(-28f, -64f);

            GameObject buttons = new("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttons.transform.SetParent(panel.transform, false);
            RectTransform buttonsRect = (RectTransform)buttons.transform;
            buttonsRect.anchorMin = new Vector2(0f, 0f);
            buttonsRect.anchorMax = new Vector2(1f, 0f);
            buttonsRect.pivot = new Vector2(0.5f, 0f);
            buttonsRect.anchoredPosition = Vector2.zero;
            buttonsRect.sizeDelta = new Vector2(0f, 64f);

            HorizontalLayoutGroup layout = buttons.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateTMPButton("Btn_NewGame", buttons.transform, "新游戏", new Color(0.42f, 0.4f, 0.22f, 1f));
            CreateTMPButton("Btn_Save", buttons.transform, "保存存档", new Color(0.18f, 0.42f, 0.72f, 1f));
            CreateTMPButton("Btn_Load", buttons.transform, "读取存档", new Color(0.2f, 0.5f, 0.32f, 1f));
            CreateTMPButton("Btn_Delete", buttons.transform, "删除存档", new Color(0.68f, 0.24f, 0.24f, 1f));
            CreateTMPButton("Btn_Cancel", buttons.transform, "取消", new Color(0.32f, 0.32f, 0.34f, 1f));

            // 保持激活：Prefab 预览时可见；运行时由 Initialize 隐藏
            panel.SetActive(true);
            return panel;
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

            TextMeshProUGUI labelText = CreateTMPText("Text (TMP)", go.transform,
                label, 20, TextAlignmentOptions.Midline, Color.white);
            labelText.color = Color.white;
            return button;
        }

        private static void SetStretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        #endregion
    }
}
