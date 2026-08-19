using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Logger;
using MyGame.UI.Settings.Controller;
using System.Collections;
using System.Collections.Generic;
using MyGame.UI.Settings.Components;

namespace MyGame.UI.Settings.View
{
    /// <summary>
    /// 设置面板视图，负责显示设置界面和处理用户输入
    /// </summary>
    public class SettingsPanelView : BaseView<SettingsPanelController>
    {
        #region 字段

        [Header("UI References")]
        [Tooltip("设置面板根对象")]
        [SerializeField] private GameObject m_settingsPanel;

        [Tooltip("返回按钮")]
        [SerializeField] private Button m_backButton;

        [Header("Action Buttons")]
        [Tooltip("应用按钮")]
        [SerializeField] private Button m_applyButton;

        [Tooltip("保存按钮")]
        [SerializeField] private Button m_saveButton;

        [Header("Feedback")]
        [Tooltip("操作反馈文本（位于左上角返回按钮旁；缺失时运行时自动生成兜底文本）")]
        [SerializeField] private TextMeshProUGUI m_feedbackText;

        [Header("Unsaved Confirm Dialog")]
        [Tooltip("未保存修改提示弹窗（可选；缺失时运行时自动生成兜底面板）")]
        [SerializeField] private GameObject m_unsavedConfirmDialog;
        [SerializeField] private TextMeshProUGUI m_unsavedConfirmMessage;
        [SerializeField] private Button m_discardAndExitButton;
        [SerializeField] private Button m_stayButton;

        private Text m_fallbackConfirmMessage;
        private Text m_fallbackFeedbackText;
        private Coroutine m_feedbackCoroutine;

        private const string LOG_MODULE = LogModules.SETTINGS + "View";

        private readonly List<BaseSettingsComponent> m_optionComponents = new();

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化面板
        /// </summary>
        protected override void Awake()
        {
            // 设置面板类型
            m_panelType = UIType.SettingsPanel;
            base.Awake();
            
            // 查找所有设置组件
            FindAllSettingsComponents();
        }

        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            if (m_backButton != null)
            {
                m_backButton.onClick.AddListener(OnBackButtonClick);
            }

            if (m_applyButton != null)
            {
                m_applyButton.onClick.AddListener(OnApplyButtonClick);
            }

            if (m_saveButton != null)
            {
                m_saveButton.onClick.AddListener(OnSaveButtonClick);
            }

            if (m_discardAndExitButton != null)
            {
                m_discardAndExitButton.onClick.AddListener(OnDiscardAndExitButtonClick);
            }

            if (m_stayButton != null)
            {
                m_stayButton.onClick.AddListener(OnStayButtonClick);
            }
        }

        /// <summary>
        /// 解绑按钮事件（与 BindButtonEvents 成对出现）
        /// </summary>
        private void UnbindButtonEvents()
        {
            if (m_backButton != null)
            {
                m_backButton.onClick.RemoveListener(OnBackButtonClick);
            }

            if (m_applyButton != null)
            {
                m_applyButton.onClick.RemoveListener(OnApplyButtonClick);
            }

            if (m_saveButton != null)
            {
                m_saveButton.onClick.RemoveListener(OnSaveButtonClick);
            }

            if (m_discardAndExitButton != null)
            {
                m_discardAndExitButton.onClick.RemoveListener(OnDiscardAndExitButtonClick);
            }

            if (m_stayButton != null)
            {
                m_stayButton.onClick.RemoveListener(OnStayButtonClick);
            }
        }

        /// <summary>
        /// 初始化面板：绑定按钮事件并初始化设置组件
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Log.Info(LOG_MODULE, "初始化设置面板");
            EnsureUnsavedConfirmDialog();
            EnsureFeedbackText();
            BindButtonEvents();
            InitializeAllSettingsComponents();
            HideUnsavedConfirmDialog();
            HideFeedbackText();
        }

        /// <summary>
        /// 清理面板资源：解绑按钮事件
        /// </summary>
        public override void Cleanup()
        {
            UnbindButtonEvents();
            base.Cleanup();
        }
        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 返回按钮点击事件处理
        /// </summary>
        private void OnBackButtonClick()
        {
            Log.Info(LOG_MODULE, "返回按钮被点击");
            if (m_controller != null)
            {
                m_controller.RequestBackToMainMenu();
            }
        }

        /// <summary>
        /// 应用按钮点击事件处理
        /// </summary>
        private void OnApplyButtonClick()
        {
            Log.Info(LOG_MODULE, "应用按钮被点击");
            if (m_controller == null)
            {
                ShowFeedback("设置控制器不可用", false);
                return;
            }

            bool success = m_controller.ApplySettings();
            ShowFeedback(success ? "设置已应用" : "应用设置失败", success);
        }

        /// <summary>
        /// 保存按钮点击事件处理
        /// </summary>
        private void OnSaveButtonClick()
        {
            Log.Info(LOG_MODULE, "保存按钮被点击");
            if (m_controller == null)
            {
                ShowFeedback("设置控制器不可用", false);
                return;
            }

            bool success = m_controller.SaveSettings();
            ShowFeedback(success ? "设置已保存" : "保存设置失败", success);
        }

        /// <summary>
        /// 未保存提示面板的“放弃并退出”
        /// </summary>
        private void OnDiscardAndExitButtonClick()
        {
            Log.Info(LOG_MODULE, "放弃未保存修改并退出设置");
            m_controller?.DiscardChangesAndExit();
        }

        /// <summary>
        /// 未保存提示面板的“继续编辑”
        /// </summary>
        private void OnStayButtonClick()
        {
            Log.Info(LOG_MODULE, "取消退出，继续编辑设置");
            m_controller?.CancelExit();
        }

        #endregion

        #region 面板控制

        /// <summary>
        /// 显示面板
        /// </summary>
        public override void Show()
        {
            Log.Info(LOG_MODULE, "显示设置面板");

            // 主菜单保持可见作为背景，仅禁止交互，避免空白真空期
            UIManager.Instance?.SetPanelInteractable(UIType.MainMenu, false);

            base.Show();
            HideUnsavedConfirmDialog();
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public override void Hide()
        {
            Log.Info(LOG_MODULE, "隐藏设置面板");

            // 恢复主菜单交互
            UIManager.Instance?.SetPanelInteractable(UIType.MainMenu, true);

            base.Hide();
            HideUnsavedConfirmDialog();
        }

        #endregion

        #region 未保存确认弹窗

        /// <summary>
        /// 显示“存在未保存修改”确认弹窗。
        /// </summary>
        public void ShowUnsavedConfirmDialog()
        {
            if (m_unsavedConfirmDialog == null)
            {
                return;
            }

            if (m_unsavedConfirmMessage != null)
            {
                m_unsavedConfirmMessage.text = "设置尚未保存，确定要退出吗？未保存的修改将丢失。";
            }
            if (m_fallbackConfirmMessage != null)
            {
                m_fallbackConfirmMessage.text = "设置尚未保存，确定要退出吗？\n未保存的修改将丢失。";
            }

            m_unsavedConfirmDialog.SetActive(true);
        }

        /// <summary>
        /// 隐藏未保存确认弹窗。
        /// </summary>
        public void HideUnsavedConfirmDialog()
        {
            if (m_unsavedConfirmDialog != null && m_unsavedConfirmDialog != gameObject)
            {
                m_unsavedConfirmDialog.SetActive(false);
            }
        }

        /// <summary>
        /// 场景/预制体未提供确认弹窗时，生成运行时兜底面板。
        /// </summary>
        private void EnsureUnsavedConfirmDialog()
        {
            if (m_unsavedConfirmDialog != null)
            {
                Log.Info(LOG_MODULE, "使用预制体中的 UnsavedConfirmDialog");
                return;
            }

            Log.Warning(LOG_MODULE, "预制体未配置 UnsavedConfirmDialog，生成运行时兜底面板");

            GameObject dialog = new("UnsavedConfirmDialog",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            dialog.transform.SetParent(transform, false);

            RectTransform rect = dialog.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(560f, 260f);

            dialog.GetComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.98f);

            m_fallbackConfirmMessage = CreateRuntimeText("Message",
                "设置尚未保存，确定要退出吗？\n未保存的修改将丢失。", dialog.transform);
            RectTransform messageRect = m_fallbackConfirmMessage.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0f, 0f);
            messageRect.anchorMax = new Vector2(1f, 1f);
            messageRect.offsetMin = new Vector2(24f, 80f);
            messageRect.offsetMax = new Vector2(-24f, -24f);

            m_discardAndExitButton = CreateRuntimeButton("DiscardAndExit", "放弃并退出", dialog.transform);
            RectTransform discardRect = m_discardAndExitButton.GetComponent<RectTransform>();
            discardRect.anchorMin = new Vector2(0.5f, 0f);
            discardRect.anchorMax = new Vector2(0.5f, 0f);
            discardRect.pivot = new Vector2(0.5f, 0f);
            discardRect.anchoredPosition = new Vector2(-130f, 24f);
            discardRect.sizeDelta = new Vector2(220f, 48f);

            m_stayButton = CreateRuntimeButton("Stay", "继续编辑", dialog.transform);
            RectTransform stayRect = m_stayButton.GetComponent<RectTransform>();
            stayRect.anchorMin = new Vector2(0.5f, 0f);
            stayRect.anchorMax = new Vector2(0.5f, 0f);
            stayRect.pivot = new Vector2(0.5f, 0f);
            stayRect.anchoredPosition = new Vector2(130f, 24f);
            stayRect.sizeDelta = new Vector2(220f, 48f);

            m_unsavedConfirmDialog = dialog;
            m_unsavedConfirmDialog.SetActive(false);
        }

        private Button CreateRuntimeButton(string name, string label, Transform parent)
        {
            GameObject buttonGO = new(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);

            buttonGO.GetComponent<UnityEngine.UI.Image>().color = new Color(0.25f, 0.25f, 0.28f, 0.98f);

            Button button = buttonGO.GetComponent<Button>();
            button.targetGraphic = buttonGO.GetComponent<UnityEngine.UI.Image>();

            Text text = CreateRuntimeText("Label", label, buttonGO.transform);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 20;

            return button;
        }

        private Text CreateRuntimeText(string name, string text, Transform parent)
        {
            GameObject textGO = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 2f);
            rect.offsetMax = new Vector2(-8f, -2f);

            Text label = textGO.GetComponent<Text>();
            label.text = text;
            label.color = Color.white;
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            return label;
        }

        #endregion

        #region 操作反馈

        /// <summary>
        /// 在左上角返回按钮旁显示小字反馈，2 秒后淡出。
        /// </summary>
        public void ShowFeedback(string message, bool success)
        {
            EnsureFeedbackText();

            if (m_feedbackText != null)
            {
                m_feedbackText.text = message;
                m_feedbackText.color = success ? new Color(0.45f, 0.85f, 0.45f, 1f) : new Color(0.95f, 0.45f, 0.45f, 1f);
                m_feedbackText.alpha = 1f;
                m_feedbackText.gameObject.SetActive(true);
            }
            else if (m_fallbackFeedbackText != null)
            {
                m_fallbackFeedbackText.text = message;
                m_fallbackFeedbackText.color = success ? new Color(0.45f, 0.85f, 0.45f, 1f) : new Color(0.95f, 0.45f, 0.45f, 1f);
                m_fallbackFeedbackText.gameObject.SetActive(true);
            }

            if (m_feedbackCoroutine != null)
            {
                StopCoroutine(m_feedbackCoroutine);
            }
            m_feedbackCoroutine = StartCoroutine(FadeFeedbackText());
        }

        private void HideFeedbackText()
        {
            if (m_feedbackCoroutine != null)
            {
                StopCoroutine(m_feedbackCoroutine);
                m_feedbackCoroutine = null;
            }

            if (m_feedbackText != null)
            {
                m_feedbackText.gameObject.SetActive(false);
            }
            if (m_fallbackFeedbackText != null)
            {
                m_fallbackFeedbackText.gameObject.SetActive(false);
            }
        }

        private IEnumerator FadeFeedbackText()
        {
            yield return new WaitForSecondsRealtime(1.6f);

            float elapsed = 0f;
            const float fadeDuration = 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

                if (m_feedbackText != null)
                {
                    m_feedbackText.alpha = alpha;
                }
                if (m_fallbackFeedbackText != null)
                {
                    Color c = m_fallbackFeedbackText.color;
                    c.a = alpha;
                    m_fallbackFeedbackText.color = c;
                }

                yield return null;
            }

            HideFeedbackText();
        }

        /// <summary>
        /// 场景/预制体未提供反馈文本时，在返回按钮旁生成运行时兜底小字。
        /// </summary>
        private void EnsureFeedbackText()
        {
            if (m_feedbackText != null)
            {
                Log.Info(LOG_MODULE, "使用预制体中的 FeedbackText");
                return;
            }

            if (m_fallbackFeedbackText != null)
            {
                return;
            }

            Log.Warning(LOG_MODULE, "预制体未配置 FeedbackText，生成运行时兜底文本");

            m_fallbackFeedbackText = CreateRuntimeText("FeedbackText", string.Empty, transform);
            m_fallbackFeedbackText.fontSize = 16;
            m_fallbackFeedbackText.alignment = TextAnchor.MiddleLeft;

            RectTransform rect = m_fallbackFeedbackText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(320f, -75f);
            rect.sizeDelta = new Vector2(360f, 40f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            m_fallbackFeedbackText.gameObject.SetActive(false);
        }

        #endregion

        #region 控制器绑定

        /// <summary>
        /// 尝试自动绑定控制器（标准模式：同物体查找，初始化并注入视图）
        /// </summary>
        protected override void TryBindController()
        {
            // 避免重复绑定（Initialize中可能再次调用）
            if (m_controller != null) return;

            // View与Controller挂载在同一GameObject上，直接从自身获取
            if (!TryGetComponent<SettingsPanelController>(out var controller))
            {
                Log.Error(LOG_MODULE, "未找到SettingsPanelController实例，请确保已将控制器脚本挂载到组件上");
                return;
            }

            // 初始化控制器（幂等，基类 IsInitialized 防重入）
            controller.Initialize();

            // 注入视图引用，完成View侧绑定
            controller.SetView(this);
            BindController(controller);
            Log.Info(LOG_MODULE, "已成功绑定SettingsPanelController");
        }

        #endregion

        #region 选项组件管理

        /// <summary>
        /// 查找所有设置组件
        /// </summary>
        private void FindAllSettingsComponents()
        {
            Log.Info(LOG_MODULE, "查找所有设置组件");
            
            // 清空现有列表
            m_optionComponents.Clear();
            
            // 使用基类查找所有派生组件
            FindSettingsComponentsByBaseClass();
        }
        
        /// <summary>
        /// 通过基类查找所有派生组件
        /// 这种方法可以找到所有继承自BaseSettingsComponent的组件，无需单独列出每个派生类
        /// </summary>
        private void FindSettingsComponentsByBaseClass()
        {
            // 获取所有继承自BaseSettingsComponent的组件。
            // 注意：多个设置组件可能挂在同一个 GameObject 上，因此必须保存组件引用本身，
            // 不能保存 GameObject（否则 TryGetComponent 只会反复取到第一个组件）。
            var settingsComponents = GetComponentsInChildren<BaseSettingsComponent>(true);
            
            int addedCount = 0;
            foreach (var component in settingsComponents)
            {
                if (component != null)
                {
                    m_optionComponents.Add(component);
                    Log.Info(LOG_MODULE,
                        $"发现设置组件: {component.GetType().Name} @ {component.gameObject.name}");
                    addedCount++;
                }
            }
            
            Log.Info(LOG_MODULE, $"成功添加 {addedCount} 个设置组件到集合");
        }

        /// <summary>
        /// 初始化所有设置组件
        /// </summary>
        private void InitializeAllSettingsComponents()
        { 
            if (m_controller == null)
            {
                Log.Error(LOG_MODULE, "控制器为空，无法初始化设置组件");
            }
            
            // 遍历并初始化每个设置组件
            int initializedCount = 0;
            
            foreach (BaseSettingsComponent settingsComponent in m_optionComponents)
            {
                if (settingsComponent == null)
                {
                    Log.Warning(LOG_MODULE, "发现空的设置组件，跳过初始化");
                    continue;
                }

                Log.Info(LOG_MODULE, "初始化设置组件: " + settingsComponent.GetType().Name);
                settingsComponent.Initialize(m_controller);
                // 立即更新视图以显示当前设置值
                settingsComponent.UpdateView();
                initializedCount++;
            }
            Log.Info(LOG_MODULE, $"所有设置组件初始化完成，成功初始化: {initializedCount}/{m_optionComponents.Count}");
        }

        /// <summary>
        /// 更新所有设置组件的视图
        /// </summary>
        public void UpdateAllSettingsComponents()
        {
            foreach (BaseSettingsComponent settingsComponent in m_optionComponents)
            {
                if (settingsComponent != null)
                {
                    settingsComponent.UpdateView();
                }
            }
        }

        #endregion
    }
}