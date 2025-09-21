using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Logger;
using MyGame.UI.Settings.Controller;
using MyGame.UI.Components;
using System.Collections.Generic;

namespace MyGame.UI.Settings.View
{
    /// <summary>
    /// 设置面板视图，负责显示设置界面和处理用户输入
    /// </summary>
    public class SettingsPanelView : BaseView<SettingsPanelController>
    {
        #region 枚举

        /// <summary>
        /// 设置页面类型
        /// </summary>
        public enum SettingsPage
        {
            Graphics,
            Audio,
            Controls
        }

        #endregion

        #region 字段

        [Header("UI References")]
        [Tooltip("设置面板根对象")]
        [SerializeField] private GameObject m_settingsPanel;

        [Tooltip("返回按钮")]
        [SerializeField] private Button m_backButton;

        #region 分页相关UI

        [Header("Page Navigation")]
        [Tooltip("页面容器")]
        [SerializeField] private Transform m_pagesContainer;

        [Tooltip("图形设置页面")]
        [SerializeField] private GameObject m_graphicsPage;

        [Tooltip("声音设置页面")]
        [SerializeField] private GameObject m_audioPage;

        [Tooltip("控制设置页面")]
        [SerializeField] private GameObject m_controlsPage;

        [Tooltip("图形设置按钮")]
        [SerializeField] private Button m_graphicsButton;

        [Tooltip("声音设置按钮")]
        [SerializeField] private Button m_audioButton;

        [Tooltip("控制设置按钮")]
        [SerializeField] private Button m_controlsButton;

        [Tooltip("当前选中的页面指示器")]
        [SerializeField] private Image m_pageIndicator;

        [Tooltip("页面指示器相对于按钮的垂直偏移量（正值向下移动）")]
        [SerializeField] private float m_indicatorVerticalOffset = 40f;

        #endregion

        [Header("Audio Settings")]
        [Tooltip("音乐音量滑块")]
        [SerializeField] private Slider m_musicVolumeSlider;

        [Tooltip("音效音量滑块")]
        [SerializeField] private Slider m_sfxVolumeSlider;

        [Header("Graphics Settings")]
        [Tooltip("画质等级下拉框")]
        [SerializeField] private TMP_Dropdown m_qualityDropdown;

        [Tooltip("全屏开关")]
        [SerializeField] private ToggleSwitch m_fullscreenToggle;

        [Tooltip("分辨率下拉框")]
        [SerializeField] private TMP_Dropdown m_resolutionDropdown;

        [Header("Gameplay Settings")]
        [Tooltip("Y轴反转开关")]
        [SerializeField] private ToggleSwitch m_invertYAxisToggle;

        [Header("Action Buttons")]
        [Tooltip("应用按钮")]
        [SerializeField] private Button m_applyButton;

        [Tooltip("保存按钮")]
        [SerializeField] private Button m_saveButton;

        private const string LOG_MODULE = LogModules.SETTINGS;
        private SettingsPage m_currentPage = SettingsPage.Graphics;

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
            
            // 绑定按钮事件
            BindButtonEvents();
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

            // 绑定分页按钮事件
            BindPageNavigationEvents();

            // 绑定设置控件事件
            BindSettingsEvents();
        }

        /// <summary>
        /// 绑定分页导航事件
        /// </summary>
        private void BindPageNavigationEvents()
        {
            if (m_graphicsButton != null)
            {
                m_graphicsButton.onClick.AddListener(() => SwitchPage(SettingsPage.Graphics));
            }

            if (m_audioButton != null)
            {
                m_audioButton.onClick.AddListener(() => SwitchPage(SettingsPage.Audio));
            }

            if (m_controlsButton != null)
            {
                m_controlsButton.onClick.AddListener(() => SwitchPage(SettingsPage.Controls));
            }
        }

        /// <summary>
        /// 绑定设置控件事件
        /// </summary>
        private void BindSettingsEvents()
        {
            // 音量设置
            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            // 画质设置
            if (m_qualityDropdown != null)
            {
                m_qualityDropdown.onValueChanged.AddListener(OnQualityLevelChanged);
            }

            if (m_fullscreenToggle != null)
            {
                m_fullscreenToggle.OnValueChanged += OnFullscreenChanged;
            }

            if (m_resolutionDropdown != null)
            {
                m_resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // 游戏设置
            if (m_invertYAxisToggle != null)
            {
                m_invertYAxisToggle.OnValueChanged += OnInvertYAxisChanged;
            }
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 返回按钮点击事件处理
        /// </summary>
        private void OnBackButtonClick()
        {
            Log.Info(LOG_MODULE, "返回按钮被点击");
            Hide();
        }

        /// <summary>
        /// 应用按钮点击事件处理
        /// </summary>
        private void OnApplyButtonClick()
        {
            Log.Info(LOG_MODULE, "应用按钮被点击");
            if (m_controller != null)
            {
                m_controller.ApplySettings();
            }
        }

        /// <summary>
        /// 保存按钮点击事件处理
        /// </summary>
        private void OnSaveButtonClick()
        {
            Log.Info(LOG_MODULE, "保存按钮被点击");
            if (m_controller != null)
            {
                m_controller.SaveSettings();
            }
        }

        #endregion

        #region 设置控件事件处理

        /// <summary>
        /// 音乐音量变化事件处理
        /// </summary>
        /// <param name="value">音量值</param>
        private void OnMusicVolumeChanged(float value)
        {
            if (m_controller != null)
            {
                m_controller.UpdateMusicVolume(value);
            }
        }

        /// <summary>
        /// 音效音量变化事件处理
        /// </summary>
        /// <param name="value">音量值</param>
        private void OnSfxVolumeChanged(float value)
        {
            if (m_controller != null)
            {
                m_controller.UpdateSfxVolume(value);
            }
        }

        /// <summary>
        /// 画质等级变化事件处理
        /// </summary>
        /// <param name="value">画质等级索引</param>
        private void OnQualityLevelChanged(int value)
        {
            if (m_controller != null)
            {
                m_controller.UpdateQualityLevel(value);
            }
        }

        /// <summary>
        /// 全屏设置变化事件处理
        /// </summary>
        /// <param name="value">是否全屏</param>
        private void OnFullscreenChanged(bool value)
        {
            if (m_controller != null)
            {
                m_controller.UpdateFullscreen(value);
            }
        }

        /// <summary>
        /// 分辨率变化事件处理
        /// </summary>
        /// <param name="value">分辨率索引</param>
        private void OnResolutionChanged(int value)
        {
            if (m_controller != null)
            {
                m_controller.UpdateResolutionIndex(value);
            }
        }

        /// <summary>
        /// Y轴反转设置变化事件处理
        /// </summary>
        /// <param name="value">是否反转Y轴</param>
        private void OnInvertYAxisChanged(bool value)
        {
            if (m_controller != null)
            {
                m_controller.UpdateInvertYAxis(value);
            }
        }

        #endregion

        #region 面板控制

        /// <summary>
        /// 显示面板
        /// </summary>
        public override void Show()
        {
            Log.Info(LOG_MODULE, "显示设置面板");
            base.Show();
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public override void Hide()
        {
            Log.Info(LOG_MODULE, "隐藏设置面板");
            base.Hide();
        }

        /// <summary>
        /// 初始化面板
        /// </summary>
        public override void Initialize()
        {
            Log.Info(LOG_MODULE, "初始化设置面板");
            // 初始化分辨率选项
            InitializeResolutionDropdown();
            // 初始化画质选项
            InitializeQualityDropdown();
            // 初始化分页
            InitializePages();
        }

        /// <summary>
        /// 初始化分页
        /// </summary>
        private void InitializePages()
        {
            // 默认显示图形设置页面
            SwitchPage(SettingsPage.Graphics);
        }

        /// <summary>
        /// 切换页面
        /// </summary>
        /// <param name="page">要切换的页面类型</param>
        public void SwitchPage(SettingsPage page)
        {
            Log.Info(LOG_MODULE, $"切换到页面: {page}");
            
            // 隐藏所有页面
            if (m_graphicsPage != null)
                m_graphicsPage.SetActive(false);
            if (m_audioPage != null)
                m_audioPage.SetActive(false);
            if (m_controlsPage != null)
                m_controlsPage.SetActive(false);

            // 显示选中的页面
            switch (page)
            {
                case SettingsPage.Graphics:
                    if (m_graphicsPage != null)
                        m_graphicsPage.SetActive(true);
                    break;
                case SettingsPage.Audio:
                    if (m_audioPage != null)
                        m_audioPage.SetActive(true);
                    break;
                case SettingsPage.Controls:
                    if (m_controlsPage != null)
                        m_controlsPage.SetActive(true);
                    break;
            }

            // 更新当前页面
            m_currentPage = page;
            
            // 更新页面指示器位置
            UpdatePageIndicator();
        }

        /// <summary>
        /// 更新页面指示器位置
        /// </summary>
        /// <summary>
        /// 更新页面指示器的位置，根据当前选中的页面
        /// </summary>
        private void UpdatePageIndicator()
        {
            if (m_pageIndicator == null)
                return;

            switch (m_currentPage)
            {
                case SettingsPage.Graphics:
                    SetIndicatorPosition(m_graphicsButton);
                    break;

                case SettingsPage.Audio:
                    SetIndicatorPosition(m_audioButton);
                    break;

                case SettingsPage.Controls:
                    SetIndicatorPosition(m_controlsButton);
                    break;
            }
        }

        /// <summary>
        /// 设置页面指示器相对于指定按钮的位置
        /// </summary>
        /// <param name="targetButton">目标按钮</param>
        private void SetIndicatorPosition(Button targetButton)
        {
            if (targetButton == null || m_pageIndicator == null)
                return;

            Vector3 indicatorPosition = targetButton.transform.position;
            indicatorPosition.y -= m_indicatorVerticalOffset; // 将指示器移到按钮下方
            m_pageIndicator.transform.position = indicatorPosition;
        }

        #endregion

        #region UI更新方法

        /// <summary>
        /// 更新音乐音量滑块
        /// </summary>
        /// <param name="volume">音量值</param>
        public void UpdateMusicVolumeSlider(float volume)
        {
            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.value = volume;
            }
        }

        /// <summary>
        /// 更新音效音量滑块
        /// </summary>
        /// <param name="volume">音量值</param>
        public void UpdateSfxVolumeSlider(float volume)
        {
            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.value = volume;
            }
        }

        /// <summary>
        /// 更新画质等级下拉框
        /// </summary>
        /// <param name="qualityLevel">画质等级</param>
        public void UpdateQualityDropdown(int qualityLevel)
        {
            if (m_qualityDropdown != null)
            {
                m_qualityDropdown.value = qualityLevel;
            }
        }

        /// <summary>
        /// 更新全屏开关
        /// </summary>
        /// <param name="isFullscreen">是否全屏</param>
        public void UpdateFullscreenToggle(bool isFullscreen)
        {
            if (m_fullscreenToggle != null)
            {
                m_fullscreenToggle.IsOn = isFullscreen;
            }
        }

        /// <summary>
        /// 更新分辨率下拉框
        /// </summary>
        /// <param name="resolutionIndex">分辨率索引</param>
        public void UpdateResolutionDropdown(int resolutionIndex)
        {
            if (m_resolutionDropdown != null)
            {
                m_resolutionDropdown.value = resolutionIndex;
            }
        }

        /// <summary>
        /// 更新Y轴反转开关
        /// </summary>
        /// <param name="invertYAxis">是否反转Y轴</param>
        public void UpdateInvertYAxisToggle(bool invertYAxis)
        {
            if (m_invertYAxisToggle != null)
            {
                m_invertYAxisToggle.IsOn = invertYAxis;
            }
        }

        #endregion

        #region 初始化辅助方法

        /// <summary>
        /// 初始化分辨率下拉框
        /// </summary>
        private void InitializeResolutionDropdown()
        {
            if (m_resolutionDropdown == null)
                return;

            m_resolutionDropdown.ClearOptions();
            List<string> resolutionOptions = new();
            Resolution[] resolutions = Screen.resolutions;

            int currentResolutionIndex = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = $"{resolutions[i].width}x{resolutions[i].height}@{resolutions[i].refreshRateRatio.value}Hz";
                resolutionOptions.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            m_resolutionDropdown.AddOptions(resolutionOptions);
            m_resolutionDropdown.value = currentResolutionIndex;
            m_resolutionDropdown.RefreshShownValue();
        }

        /// <summary>
        /// 初始化画质等级下拉框
        /// </summary>
        private void InitializeQualityDropdown()
        {
            if (m_qualityDropdown == null)
                return;

            m_qualityDropdown.ClearOptions();
            string[] qualityNames = QualitySettings.names;
            m_qualityDropdown.AddOptions(new List<string>(qualityNames));
            m_qualityDropdown.value = QualitySettings.GetQualityLevel();
            m_qualityDropdown.RefreshShownValue();
        }

        #endregion
    }
}