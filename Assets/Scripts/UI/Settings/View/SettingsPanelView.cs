using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Logger;
using MyGame.UI.Settings.Controller;

using MyGame.UI.Settings.Components;
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
        
        [Header("Settings Components")]
        [Tooltip("图形设置组件")]
        [SerializeField] private GraphicsSettingsComponent m_graphicsSettingsComponent;
        
        [Tooltip("音频设置组件")]
        [SerializeField] private AudioSettingsComponent m_audioSettingsComponent;
        
        [Tooltip("控制设置组件")]
        [SerializeField] private ControlsSettingsComponent m_controlsSettingsComponent;

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
            // 现在通过组件来处理事件绑定，这里可以留空或添加全局事件
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
            
            // 初始化子组件
            InitializeSettingsComponents();
            
            // 初始化分页
            InitializePages();
        }
        
        /// <summary>
        /// 初始化设置组件
        /// </summary>
        private void InitializeSettingsComponents()
        {
            // 初始化图形设置组件
            if (m_graphicsSettingsComponent != null)
            {
                m_graphicsSettingsComponent.Initialize(m_controller);
            }
            
            // 初始化音频设置组件
            if (m_audioSettingsComponent != null)
            {
                m_audioSettingsComponent.Initialize(m_controller);
            }
            
            // 初始化控制设置组件
            if (m_controlsSettingsComponent != null)
            {
                m_controlsSettingsComponent.Initialize(m_controller);
            }
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
        /// 更新所有设置组件的视图
        /// </summary>
        public void UpdateAllSettingsViews()
        {
            if (m_graphicsSettingsComponent != null)
            {
                m_graphicsSettingsComponent.UpdateView();
            }
            
            if (m_audioSettingsComponent != null)
            {
                m_audioSettingsComponent.UpdateView();
            }
            
            if (m_controlsSettingsComponent != null)
            {
                m_controlsSettingsComponent.UpdateView();
            }
        }

        #endregion

        #region 初始化辅助方法



        #endregion
    }
}