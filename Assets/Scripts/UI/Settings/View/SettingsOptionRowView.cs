using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MyGame.UI.Settings.Model;
using MyGame.Utils;

namespace MyGame.UI.Settings.View
{
    /// <summary>
    /// 设置行视图。
    /// 控件一律由本类创建，读写经 ISettingsValueProvider。
    /// Dropdown 为自绘弹出列表：点击展开，点选项选中，点外部关闭。
    /// </summary>
    public class SettingsOptionRowView : MonoBehaviour
    {
        private const string RESOLUTION_INDEX_KEY = "resolutionIndex";

        private static readonly Color s_dark = new(0.15f, 0.16f, 0.2f, 1f);
        private static readonly Color s_gray = new(0.55f, 0.56f, 0.6f, 1f);
        private static readonly Color s_blue = new(0.30f, 0.55f, 0.95f, 1f);
        private static readonly Color s_white = new(0.95f, 0.95f, 0.97f, 1f);
        private static readonly Color s_selectedOption = new(0.30f, 0.55f, 0.95f, 0.85f);
        private static readonly Color s_normalOption = new(0.15f, 0.16f, 0.20f, 1f);

        private SettingsOptionDefinition m_definition;
        private ISettingsValueProvider m_provider;

        private TextMeshProUGUI m_titleText;
        private Button m_toggleButton;
        private Image m_toggleImage;
        private Text m_toggleLabel;
        private Slider m_slider;
        private TextMeshProUGUI m_percentageText;
        private Button m_dropdownButton;
        private TextMeshProUGUI m_dropdownLabel;
        private List<string> m_dropdownOptions = new();
        private int m_dropdownIndex;
        private GameObject m_dropdownPopup;
        private GameObject m_dropdownBlocker;
        private readonly List<Image> m_optionBackgrounds = new();

        public string OptionKey
        {
            get { return m_definition != null ? m_definition.key : string.Empty; }
        }

        public void Initialize(SettingsOptionDefinition definition, ISettingsValueProvider provider)
        {
            m_definition = definition;
            m_provider = provider;

            if (m_definition == null || m_provider == null)
            {
                return;
            }

            BuildRow();
            Refresh();
        }

        /// <summary>
        /// 刷新设置行视图。
        /// </summary>
        public void Refresh()
        {
            if (m_definition == null || m_provider == null)
            {
                return;
            }

            if (m_definition.type == SettingsOptionType.Header)
            {
                return;
            }

            object value = m_provider.GetValue(m_definition.key);
            if (value == null)
            {
                return;
            }

            switch (m_definition.type)
            {
                case SettingsOptionType.Toggle:
                    SetToggleVisual(Convert.ToBoolean(value));
                    break;
                case SettingsOptionType.Slider:
                    float raw = Convert.ToSingle(value);
                    float normalized = Mathf.InverseLerp(m_definition.minValue, Mathf.Max(m_definition.minValue, m_definition.maxValue), raw);
                    m_slider.SetValueWithoutNotify(normalized);
                    UpdatePercentage(normalized);
                    break;
                case SettingsOptionType.Dropdown:
                    m_dropdownIndex = Mathf.Clamp(Convert.ToInt32(value), 0, m_dropdownOptions.Count - 1);
                    UpdateDropdownLabel();
                    break;
            }
        }

        /// <summary>
        /// 构建设置行视图。
        /// </summary>
        private void BuildRow()
        {
            if (m_definition.type != SettingsOptionType.Header)
            {
                m_titleText = CreateTmpText("Title", transform, m_definition.title, 28f);
                RectTransform titleRect = m_titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 0.5f);
                titleRect.anchorMax = new Vector2(0f, 0.5f);
                titleRect.pivot = new Vector2(0f, 0.5f);
                titleRect.anchoredPosition = new Vector2(18f, 0f);
                titleRect.sizeDelta = new Vector2(250f, 48f);
            }
            else
            {
                m_titleText = CreateTmpText("Title", transform, m_definition.title, 32f);
                RectTransform titleRect = m_titleText.rectTransform;
                titleRect.anchorMin = Vector2.zero;
                titleRect.anchorMax = Vector2.one;
                titleRect.pivot = new Vector2(0.5f, 0.5f);
                titleRect.offsetMin = new Vector2(18f, 0f);
                titleRect.offsetMax = new Vector2(-18f, 0f);
                m_titleText.fontStyle = FontStyles.Bold;
            }

            switch (m_definition.type)
            {
                case SettingsOptionType.Toggle:
                    BuildToggle();
                    break;
                case SettingsOptionType.Slider:
                    BuildSlider();
                    break;
                case SettingsOptionType.Dropdown:
                    BuildDropdown();
                    break;
            }
        }

        /// <summary>
        /// 构建开关按钮
        /// </summary>
        private void BuildToggle()
        {
            GameObject go = new("ToggleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-18f, 0f);
            rect.sizeDelta = new Vector2(120f, 48f);

            m_toggleImage = go.GetComponent<Image>();
            m_toggleImage.color = s_gray;
            m_toggleButton = go.GetComponent<Button>();
            m_toggleButton.targetGraphic = m_toggleImage;
            m_toggleButton.onClick.AddListener(OnToggleClicked);

            m_toggleLabel = CreateLegacyText("Status", go.transform, "关", 18);
            RectTransform labelRect = (RectTransform)m_toggleLabel.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// 构建滑动条
        /// </summary>
        private void BuildSlider()
        {
            GameObject go = new("Slider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            go.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-70f, 0f);
            rect.sizeDelta = new Vector2(180f, 20f);

            Image background = go.GetComponent<Image>();
            background.color = new Color(0.10f, 0.11f, 0.14f, 1f);
            background.raycastTarget = true;

            RectTransform fillArea = CreateImageChild("Fill Area", go.transform, new Vector2(0f, 0.2f), new Vector2(1f, 0.8f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            RectTransform fillRect = CreateImageChild("Fill", fillArea, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(8f, 0f));
            fillRect.GetComponent<Image>().color = s_blue;

            RectTransform handleArea = CreateImageChild("Handle Slide Area", go.transform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            RectTransform handleRect = CreateImageChild("Handle", handleArea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            handleRect.sizeDelta = new Vector2(20f, 20f);
            Image handleImage = handleRect.GetComponent<Image>();
            handleImage.color = s_white;

            m_slider = go.GetComponent<Slider>();
            m_slider.fillRect = fillRect;
            m_slider.handleRect = handleRect;
            m_slider.targetGraphic = handleImage;
            m_slider.minValue = 0f;
            m_slider.maxValue = 1f;
            m_slider.onValueChanged.AddListener(OnSliderChanged);

            m_percentageText = CreateTmpText("Percentage", transform, "0", 16f);
            RectTransform percentageRect = m_percentageText.rectTransform;
            percentageRect.anchorMin = new Vector2(1f, 0.5f);
            percentageRect.anchorMax = new Vector2(1f, 0.5f);
            percentageRect.pivot = new Vector2(1f, 0.5f);
            percentageRect.anchoredPosition = new Vector2(-18f, 0f);
            percentageRect.sizeDelta = new Vector2(44f, 30f);
            m_percentageText.alignment = TextAlignmentOptions.MidlineRight;
        }
        
        /// <summary>
        /// 构建下拉列表按钮
        /// </summary>
        private void BuildDropdown()
        {
            // 自绘下拉：按钮显示当前值，点击弹出纯代码列表
            GameObject go = new("DropdownButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-18f, 0f);
            rect.sizeDelta = new Vector2(240f, 40f);

            Image background = go.GetComponent<Image>();
            background.color = s_dark;
            m_dropdownButton = go.GetComponent<Button>();
            m_dropdownButton.targetGraphic = background;
            m_dropdownButton.onClick.AddListener(OnDropdownClicked);

            // 下拉列表标签,用18号字体，TmpText组件
            m_dropdownLabel = CreateTmpText("Label", go.transform, string.Empty, 18f);
            RectTransform labelRect = m_dropdownLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);

            m_dropdownOptions = m_definition.key == RESOLUTION_INDEX_KEY
                ? BuildResolutionOptions()
                : new List<string>(m_definition.options ?? new List<string>());

            if (m_dropdownOptions.Count == 0)
            {
                m_dropdownOptions.Add("无选项");
            }

            m_dropdownIndex = 0;
            UpdateDropdownLabel();

            // 弹出列表预创建为 inactive 子物体，Hierarchy 中始终可见 ScrollRect 结构
            CreateDropdownPopup();
        }
        
        /// <summary>
        /// 设置开关按钮的视觉状态。
        /// </summary>
        /// <param name="isOn">是否开启。</param>
        private void SetToggleVisual(bool isOn)
        {
            if (m_toggleImage != null)
            {
                m_toggleImage.color = isOn ? s_blue : s_gray;
            }

            if (m_toggleLabel != null)
            {
                m_toggleLabel.text = isOn ? "开" : "关";
            }
        }
        
        /// <summary>
        /// 更新滑动条的百分比文本。
        /// </summary>
        /// <param name="normalized">归一化值。</param>
        private void UpdatePercentage(float normalized)
        {
            if (m_percentageText != null)
            {
                m_percentageText.text = Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f).ToString();
            }
        }
        
        /// <summary>
        /// 更新下拉列表标签的文本。
        /// </summary>
        private void UpdateDropdownLabel()
        {
            if (m_dropdownLabel != null && m_dropdownIndex >= 0 && m_dropdownIndex < m_dropdownOptions.Count)
            {
                m_dropdownLabel.text = m_dropdownOptions[m_dropdownIndex];
            }
        }
        
        /// <summary>
        /// 处理开关按钮点击事件。
        /// </summary>
        private void OnToggleClicked()
        {
            if (m_definition == null || m_provider == null)
            {
                return;
            }

            object current = m_provider.GetValue(m_definition.key);
            bool next = !(current is bool b && b);
            m_provider.SetValue(m_definition.key, next);
            SetToggleVisual(next);
        }
        
        /// <summary>
        /// 处理滑动条值改变事件。
        /// </summary>
        /// <param name="normalized">归一化值。</param>
        private void OnSliderChanged(float normalized)
        {
            if (m_definition == null || m_provider == null)
            {
                return;
            }

            UpdatePercentage(normalized);
            float value = Mathf.Lerp(m_definition.minValue, m_definition.maxValue, normalized);
            if (m_definition.wholeNumbers)
            {
                value = Mathf.Round(value);
            }
            m_provider.SetValue(m_definition.key, value);
        }
        
        /// <summary>
        /// 处理下拉列表点击事件。
        /// </summary>
        private void OnDropdownClicked()
        {
            if (m_dropdownPopup != null && m_dropdownPopup.activeSelf)
            {
                CloseDropdownPopup();
                return;
            }

            OpenDropdownPopup();
        }
        
        /// <summary>
        /// 创建下拉列表弹出框。
        /// </summary>
        private void CreateDropdownPopup()
        {
            if (m_dropdownButton == null || m_dropdownOptions.Count == 0)
            {
                return;
            }

            // 重建弹窗时清空旧引用，避免重复累积
            m_optionBackgrounds.Clear();

            m_dropdownPopup = new GameObject("Dropdown Popup",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
                typeof(CanvasGroup), typeof(Image), typeof(ScrollRect));
            m_dropdownPopup.transform.SetParent(m_dropdownButton.transform, false);

            RectTransform popupRect = (RectTransform)m_dropdownPopup.transform;
            popupRect.anchorMin = new Vector2(0f, 0f);
            popupRect.anchorMax = new Vector2(1f, 0f);
            popupRect.pivot = new Vector2(0.5f, 1f);
            popupRect.anchoredPosition = new Vector2(0f, -4f);
            float popupHeight = Mathf.Min(m_dropdownOptions.Count * 38f + 8f, 240f);
            popupRect.sizeDelta = new Vector2(0f, popupHeight);

            Canvas popupCanvas = m_dropdownPopup.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 30000;

            CanvasGroup popupGroup = m_dropdownPopup.GetComponent<CanvasGroup>();
            popupGroup.alpha = 1f;
            popupGroup.interactable = true;
            popupGroup.blocksRaycasts = true;

            Image popupBg = m_dropdownPopup.GetComponent<Image>();
            popupBg.color = new Color(0.10f, 0.11f, 0.14f, 1f);
            popupBg.raycastTarget = true;

            // Viewport：裁剪列表，让滚动生效
            GameObject viewportGo = new("Viewport",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(m_dropdownPopup.transform, false);
            RectTransform viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewportGo.GetComponent<Image>().raycastTarget = true;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRect = (RectTransform)contentGo.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup popupLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            popupLayout.padding = new RectOffset(4, 4, 4, 4);
            popupLayout.spacing = 2f;
            popupLayout.childControlWidth = true;
            popupLayout.childControlHeight = true;
            popupLayout.childForceExpandWidth = true;
            popupLayout.childForceExpandHeight = false;

            ContentSizeFitter popupFitter = contentGo.GetComponent<ContentSizeFitter>();
            popupFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect popupScroll = m_dropdownPopup.GetComponent<ScrollRect>();
            popupScroll.viewport = viewportRect;
            popupScroll.content = contentRect;
            popupScroll.horizontal = false;
            popupScroll.vertical = true;
            popupScroll.movementType = ScrollRect.MovementType.Clamped;
            popupScroll.scrollSensitivity = 0.2f;
            popupScroll.inertia = false;

            /// <summary>
            /// 创建下拉列表选项：由按钮和标签组成。
            /// </summary>
            for (int i = 0; i < m_dropdownOptions.Count; i++)
            {
                int optionIndex = i;
                GameObject optionGo = new($"Option_{m_dropdownOptions[i]}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
                optionGo.transform.SetParent(contentGo.transform, false);

                LayoutElement optionLayout = optionGo.GetComponent<LayoutElement>();
                optionLayout.preferredHeight = 36f;
                optionLayout.minHeight = 36f;

                Image optionBg = optionGo.GetComponent<Image>();
                m_optionBackgrounds.Add(optionBg);

                Button optionButton = optionGo.GetComponent<Button>();
                optionButton.targetGraphic = optionBg;
                optionButton.onClick.AddListener(() => OnDropdownOptionClicked(optionIndex));

                TextMeshProUGUI optionLabel = CreateTmpText("Label", optionGo.transform, m_dropdownOptions[i], 16f);
                RectTransform optionLabelRect = optionLabel.rectTransform;
                optionLabelRect.anchorMin = Vector2.zero;
                optionLabelRect.anchorMax = Vector2.one;
                optionLabelRect.offsetMin = new Vector2(10f, 0f);
                optionLabelRect.offsetMax = new Vector2(-10f, 0f);
            }

            m_dropdownPopup.SetActive(false);
        }

        private void OpenDropdownPopup()
        {
            if (m_dropdownPopup == null || m_dropdownButton == null)
            {
                return;
            }

            // 每次打开时按最新选中索引刷新高亮，避免停留在旧选项上
            RefreshDropdownOptionHighlights();
            m_dropdownPopup.SetActive(true);
            CreateDropdownBlocker();
        }

        /// <summary>
        /// 按当前选中索引刷新下拉选项的高亮背景。
        /// </summary>
        private void RefreshDropdownOptionHighlights()
        {
            for (int i = 0; i < m_optionBackgrounds.Count; i++)
            {
                if (m_optionBackgrounds[i] != null)
                {
                    m_optionBackgrounds[i].color = i == m_dropdownIndex ? s_selectedOption : s_normalOption;
                }
            }
        }

        private void CreateDropdownBlocker()
        {
            Canvas parentCanvas = m_dropdownButton.GetComponentInParent<Canvas>();
            if (parentCanvas == null || parentCanvas.rootCanvas == null)
            {
                return;
            }

            Canvas rootCanvas = parentCanvas.rootCanvas;

            m_dropdownBlocker = new GameObject("Dropdown Blocker",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
                typeof(CanvasGroup), typeof(Image), typeof(Button));
            m_dropdownBlocker.transform.SetParent(rootCanvas.transform, false);

            RectTransform blockerRect = (RectTransform)m_dropdownBlocker.transform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            Canvas blockerCanvas = m_dropdownBlocker.GetComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingOrder = 29999;

            CanvasGroup blockerGroup = m_dropdownBlocker.GetComponent<CanvasGroup>();
            blockerGroup.alpha = 1f;
            blockerGroup.interactable = true;
            blockerGroup.blocksRaycasts = true;

            Image blockerImage = m_dropdownBlocker.GetComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.01f);
            blockerImage.raycastTarget = true;

            Button blockerButton = m_dropdownBlocker.GetComponent<Button>();
            blockerButton.targetGraphic = blockerImage;
            blockerButton.onClick.AddListener(CloseDropdownPopup);
        }

        private void OnDropdownOptionClicked(int optionIndex)
        {
            if (m_definition == null || m_provider == null)
            {
                return;
            }

            m_dropdownIndex = optionIndex;
            UpdateDropdownLabel();
            m_provider.SetValue(m_definition.key, m_dropdownIndex);
            CloseDropdownPopup();
        }

        private void CloseDropdownPopup()
        {
            if (m_dropdownPopup != null)
            {
                m_dropdownPopup.SetActive(false);
            }

            if (m_dropdownBlocker != null)
            {
                Destroy(m_dropdownBlocker);
                m_dropdownBlocker = null;
            }
        }

        private void OnDestroy()
        {
            if (m_dropdownBlocker != null)
            {
                Destroy(m_dropdownBlocker);
                m_dropdownBlocker = null;
            }

            if (m_dropdownPopup != null)
            {
                Destroy(m_dropdownPopup);
                m_dropdownPopup = null;
            }

            if (m_toggleButton != null) m_toggleButton.onClick.RemoveListener(OnToggleClicked);
            if (m_slider != null) m_slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (m_dropdownButton != null) m_dropdownButton.onClick.RemoveListener(OnDropdownClicked);
        }

        private static TextMeshProUGUI CreateTmpText(string name, Transform parent, string text, float fontSize)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = fontSize;
            tmp.color = s_white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static Text CreateLegacyText(string name, Transform parent, string text, float fontSize)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            Text label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = Mathf.RoundToInt(fontSize);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform CreateImageChild(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            go.GetComponent<Image>().raycastTarget = true;
            return rect;
        }

        private static List<string> BuildResolutionOptions()
        {
            Resolution[] resolutions = ResolutionUtility.GetUniqueResolutions();
            List<string> labels = new(resolutions.Length);
            foreach (Resolution resolution in resolutions)
            {
                labels.Add($"{resolution.width}x{resolution.height}");
            }
            return labels;
        }
    }
}
