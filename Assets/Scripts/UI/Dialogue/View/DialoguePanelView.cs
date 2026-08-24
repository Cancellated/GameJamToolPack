using System.Collections;
using System.Collections.Generic;
using Logger;
using MyGame.Dialogue;
using MyGame.UI.Dialogue.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.UI.Dialogue.View
{
    /// <summary>
    /// 对话面板视图：显示说话人/台词/选项，负责打字机效果。
    /// 台词流程数据一律经 DialoguePanelController 查询，不直接访问 Model 或 DialogueManager。
    ///
    /// 预制体由 Tools/Game Jam Tool Pack/Rebuild Dialogue Prefab 一键生成并接线；
    /// 若字段缺失（如未烘焙预制体），View 会对关键控件做最小运行时兜底。
    /// </summary>
    public class DialoguePanelView : BaseView<DialoguePanelController>
    {
        private const string LOG_MODULE = LogModules.DIALOGUE;

        #region 序列化字段

        [Header("内容")]
        [Tooltip("说话人文本（可空；留空时自动隐藏）")]
        [SerializeField] private TextMeshProUGUI m_speakerNameText;

        [Tooltip("台词文本（必需）")]
        [SerializeField] private TextMeshProUGUI m_dialogueText;

        [Tooltip("全屏蒙版按钮：阻挡下层点击；无选项节点时点击任意位置=继续")]
        [SerializeField] private Button m_backdropButton;

        [Tooltip("选项按钮容器（居中纵向排列）")]
        [SerializeField] private RectTransform m_choicesContainer;

        [Tooltip("跳过按钮（可选）")]
        [SerializeField] private Button m_skipButton;

        [Tooltip("选项按钮预制体；未配置时使用运行时兜底按钮")]
        [SerializeField] private Button m_choiceButtonPrefab;

        [Header("打字机")]
        [Tooltip("每秒显示字符数；<=0 表示瞬间显示全文")]
        [SerializeField] private float m_charactersPerSecond = 40f;

        #endregion

        #region 运行时状态

        private readonly List<Button> m_activeChoiceButtons = new();
        private Coroutine m_typewriterRoutine;
        private bool m_isTyping;
        private bool m_pendingShowChoices;
        private string m_typingTargetText = string.Empty;

        #endregion

        #region 生命周期

        protected override void Awake()
        {
            m_panelType = UIType.DialoguePanel;
            base.Awake();

            EnsureMinimumReferences();
            EnsureBackdropMask();
        }

        /// <summary>
        /// 显示时立即开启拦截，不等淡入动画结束，避免淡入期间点击穿透到下层面板。
        /// </summary>
        public override void Show()
        {
            base.Show();

            if (m_canvasGroup != null)
            {
                m_canvasGroup.interactable = true;
                m_canvasGroup.blocksRaycasts = true;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            BindButtonEvents();
        }

        public override void Cleanup()
        {
            UnbindButtonEvents();
            StopTypewriter();
            ClearChoiceButtons();

            base.Cleanup();
        }

        protected override void OnDestroy()
        {
            StopTypewriter();
            base.OnDestroy();
        }

        /// <summary>
        /// 控制器绑定完成后补一次内容刷新：
        /// 面板经 Addressable 懒加载晚于对话开始时，Initialize 阶段 m_view 尚未注入，
        /// 在这里补刷当前节点，避免首句台词空白。
        /// </summary>
        protected override void OnControllerBound()
        {
            base.OnControllerBound();

            if (m_controller != null && DialogueManager.Instance.IsActive
                && DialogueManager.Instance.CurrentNode != null)
            {
                UpdateDialogueContent();
            }
        }

        /// <summary>
        /// 标准模式：同物体查找或创建控制器。
        /// </summary>
        protected override void TryBindController()
        {
            if (TryGetComponent<DialoguePanelController>(out var controller))
            {
                controller.Initialize();
                controller.SetView(this);
                BindController(controller);
                return;
            }

            controller = gameObject.AddComponent<DialoguePanelController>();
            controller.Initialize();
            controller.SetView(this);
            BindController(controller);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 由 Controller 调用：按当前 Model 内容刷新说话人、台词与选项。
        /// </summary>
        public void UpdateDialogueContent()
        {
            if (m_controller == null)
            {
                return;
            }

            string speaker = m_controller.GetSpeakerName();
            string text = m_controller.GetDialogueText();
            bool hasChoices = m_controller.HasChoices();

            UpdateSpeaker(speaker);

            // 先收起上一节点的选项：本节点的选项要等台词播完再出现
            UpdateChoices(false);
            StartTypewriter(text, hasChoices);
        }

        #endregion

        #region 按钮事件

        private void BindButtonEvents()
        {
            if (m_skipButton != null)
            {
                m_skipButton.onClick.AddListener(OnSkipButtonClicked);
            }

            if (m_backdropButton != null)
            {
                m_backdropButton.onClick.AddListener(OnBackdropClicked);
            }
        }

        private void UnbindButtonEvents()
        {
            if (m_skipButton != null)
            {
                m_skipButton.onClick.RemoveListener(OnSkipButtonClicked);
            }

            if (m_backdropButton != null)
            {
                m_backdropButton.onClick.RemoveListener(OnBackdropClicked);
            }
        }

        /// <summary>
        /// 全屏蒙版点击：无选项节点时任意位置=继续；有选项时必须显式点选项。
        /// 打字中点击任意位置先补全当前句。
        /// </summary>
        private void OnBackdropClicked()
        {
            if (m_isTyping)
            {
                CompleteTypewriter();
                return;
            }

            if (m_controller == null)
            {
                return;
            }

            if (m_controller.HasChoices())
            {
                Log.InfoWithCooldown(LOG_MODULE, "当前节点有选项，请点击选项按钮", "backdrop-click-with-choices");
                return;
            }

            m_controller.OnContinueClicked();
        }

        private void OnChoiceButtonClicked(int availableChoiceIndex)
        {
            if (m_controller != null)
            {
                m_controller.OnChoiceClicked(availableChoiceIndex);
            }
        }

        private void OnSkipButtonClicked()
        {
            if (m_controller != null)
            {
                m_controller.OnSkipClicked();
            }
        }

        #endregion

        #region 内容更新

        private void UpdateSpeaker(string speaker)
        {
            if (m_speakerNameText != null)
            {
                m_speakerNameText.text = speaker ?? string.Empty;
                m_speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            }
        }

        private void UpdateChoices(bool hasChoices)
        {
            ClearChoiceButtons();

            if (m_choicesContainer != null)
            {
                m_choicesContainer.gameObject.SetActive(hasChoices);
            }

            if (!hasChoices || m_controller == null)
            {
                return;
            }

            IReadOnlyList<DialogueChoice> choices = m_controller.GetAvailableChoices();
            for (int i = 0; i < choices.Count; i++)
            {
                DialogueChoice choice = choices[i];
                int choiceIndex = i;
                Button button = CreateChoiceButton(choice.choiceText, choiceIndex);
                if (button != null)
                {
                    m_activeChoiceButtons.Add(button);
                }
            }
        }

        private Button CreateChoiceButton(string label, int choiceIndex)
        {
            if (m_choicesContainer == null)
            {
                Log.Error(LOG_MODULE, "无法创建选项按钮：缺少选项容器");
                return null;
            }

            if (m_choiceButtonPrefab != null)
            {
                Button button = Instantiate(m_choiceButtonPrefab, m_choicesContainer);
                TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (labelText != null)
                {
                    labelText.text = label ?? string.Empty;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));
                return button;
            }

            // 兜底：未配置选项按钮预制体时生成最简按钮（紧急路径，正式项目应烘焙预制体）
            Log.WarningWithCooldown(LOG_MODULE,
                "对话面板未配置选项按钮预制体，使用运行时兜底按钮（建议运行 Rebuild Dialogue Prefab）",
                "dialogue-fallback-choice");

            if (m_choicesContainer == null)
            {
                Log.Error(LOG_MODULE, "无法创建兜底选项按钮：缺少选项容器");
                return null;
            }

            GameObject fallback = new("ChoiceButton_Fallback", typeof(RectTransform), typeof(Image), typeof(Button));
            fallback.transform.SetParent(m_choicesContainer, false);
            Image image = fallback.GetComponent<Image>();
            image.color = new Color(0.15f, 0.16f, 0.19f, 0.95f);

            Button fallbackButton = fallback.GetComponent<Button>();
            fallbackButton.targetGraphic = image;
            fallbackButton.onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));

            RectTransform rect = (RectTransform)fallback.transform;
            rect.sizeDelta = new Vector2(420f, 48f);

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(fallback.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            Text labelTextLegacy = labelObject.GetComponent<Text>();
            labelTextLegacy.text = label ?? string.Empty;
            labelTextLegacy.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelTextLegacy.fontSize = 20;
            labelTextLegacy.alignment = TextAnchor.MiddleLeft;
            labelTextLegacy.color = Color.white;
            labelTextLegacy.raycastTarget = false;

            return fallbackButton;
        }

        private void ClearChoiceButtons()
        {
            for (int i = 0; i < m_activeChoiceButtons.Count; i++)
            {
                if (m_activeChoiceButtons[i] != null)
                {
                    Destroy(m_activeChoiceButtons[i].gameObject);
                }
            }
            m_activeChoiceButtons.Clear();
        }

        #endregion

        #region 打字机

        private void StartTypewriter(string fullText, bool showChoicesWhenFinished)
        {
            StopTypewriter();

            if (m_dialogueText == null)
            {
                return;
            }

            m_typingTargetText = fullText ?? string.Empty;
            m_pendingShowChoices = showChoicesWhenFinished;

            if (m_charactersPerSecond <= 0f)
            {
                m_dialogueText.text = m_typingTargetText;
                m_isTyping = false;
                FinishTypewriter();
                return;
            }

            // 启动前清空旧文本：否则上一句的残留内容会让下方循环
            // 用“旧长度 < 新长度”比较，新台词更短时被整段跳过，失去打字机效果。
            m_dialogueText.text = string.Empty;
            m_typewriterRoutine = StartCoroutine(TypeTextRoutine(m_typingTargetText));
        }

        private IEnumerator TypeTextRoutine(string fullText)
        {
            m_isTyping = true;

            float secondsPerCharacter = 1f / Mathf.Max(0.001f, m_charactersPerSecond);
            float elapsed = 0f;
            int visibleCount = 0;

            // 以局部计数器判断进度，不依赖 m_dialogueText 的当前长度，
            // 避免旧文本残留干扰逐字输出。
            while (m_dialogueText != null && visibleCount < fullText.Length)
            {
                m_dialogueText.text = fullText.Substring(0, visibleCount);
                yield return null;

                elapsed += Time.unscaledDeltaTime;
                visibleCount = Mathf.Clamp(Mathf.FloorToInt(elapsed / secondsPerCharacter), 0, fullText.Length);
            }

            if (m_dialogueText != null)
            {
                m_dialogueText.text = fullText;
            }

            m_isTyping = false;
            m_typewriterRoutine = null;
            FinishTypewriter();
        }

        private void CompleteTypewriter()
        {
            if (m_typewriterRoutine != null)
            {
                StopCoroutine(m_typewriterRoutine);
                m_typewriterRoutine = null;
            }

            if (m_dialogueText != null)
            {
                m_dialogueText.text = m_typingTargetText;
            }

            m_isTyping = false;
            FinishTypewriter();
        }

        private void StopTypewriter()
        {
            if (m_typewriterRoutine != null)
            {
                StopCoroutine(m_typewriterRoutine);
                m_typewriterRoutine = null;
            }
            m_isTyping = false;
            m_pendingShowChoices = false;
        }

        /// <summary>
        /// 打字机结束（自然播完或被点击补全）后，才显示当前节点暂存的选项。
        /// </summary>
        private void FinishTypewriter()
        {
            if (!m_pendingShowChoices)
            {
                return;
            }

            m_pendingShowChoices = false;
            UpdateChoices(true);
        }

        #endregion

        #region 最小兜底引用

        /// <summary>
        /// 仅在关键字段缺失时创建最小控件，保证"未烘焙预制体也能凑合跑"。
        /// 正式项目请使用编辑器工具烘焙完整预制体。
        /// </summary>
        private void EnsureMinimumReferences()
        {
            if (m_dialogueText == null)
            {
                m_dialogueText = CreateFallbackTMPText("DialogueText", TextAlignmentOptions.TopLeft, 22);
            }

            if (m_speakerNameText == null)
            {
                m_speakerNameText = CreateFallbackTMPText("SpeakerName", TextAlignmentOptions.TopLeft, 20);
            }
        }

        /// <summary>
        /// 全屏点击蒙版兜底：
        ///   1. 旧预制体未接线 m_backdropButton 时，用根节点已有的全屏 Image 补一个 Button；
        ///   2. 关闭 DialogueBox 背景图的 raycastTarget，让点击台词区域时事件落到蒙版按钮，
        ///      实现"点任意位置=继续"，同时不拦截 Continue/Skip/选项按钮。
        /// </summary>
        private void EnsureBackdropMask()
        {
            if (m_backdropButton == null)
            {
                Image rootImage = GetComponent<Image>();
                if (rootImage != null)
                {
                    m_backdropButton = gameObject.AddComponent<Button>();
                    m_backdropButton.targetGraphic = rootImage;
                    Log.Warning(LOG_MODULE,
                        "对话面板未接线全屏蒙版按钮，已用根节点 Image 运行时补挂（建议重新执行 Rebuild Dialogue Prefabs）");
                }
            }

            Transform dialogueBoxTransform = transform.Find("DialogueBox");
            if (dialogueBoxTransform != null)
            {
                Image dialogueBoxImage = dialogueBoxTransform.GetComponent<Image>();
                if (dialogueBoxImage != null)
                {
                    dialogueBoxImage.raycastTarget = false;
                }
            }
        }

        private TextMeshProUGUI CreateFallbackTMPText(string objectName, TextAlignmentOptions alignment, int fontSize)
        {
            GameObject textObject = new(objectName, typeof(RectTransform));
            textObject.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0f, 0.55f);
            rect.anchorMax = new Vector2(1f, 0.95f);
            rect.offsetMin = new Vector2(24f, 0f);
            rect.offsetMax = new Vector2(-24f, 0f);

            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.alignment = alignment;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.text = string.Empty;

            return tmp;
        }

        #endregion
    }
}
