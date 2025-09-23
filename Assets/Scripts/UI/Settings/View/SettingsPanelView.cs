using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Logger;
using MyGame.UI.Settings.Controller;
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

        private const string LOG_MODULE = LogModules.SETTINGS;

        private readonly List<GameObject> m_optionComponents = new();

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
        }

        /// <summary>
        /// 当面板被启用时调用
        /// </summary>
        override protected void OnEnable()
        {
            // 面板启用时不需要自动初始化，初始化由控制器统一管理
        }

        /// <summary>
        /// 初始化面板
        /// </summary>
        public override void Initialize()
        {
            Log.Info(LOG_MODULE, "初始化设置面板");
            TryBindController();
            BindButtonEvents();
            InitializeAllSettingsComponents();
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

        #endregion

        #region 控制器绑定

        /// <summary>
        /// 尝试绑定控制器
        /// </summary>
        protected override void TryBindController()
        {
            if (m_controller == null)
            {
                m_controller = FindObjectOfType<SettingsPanelController>();
                if (m_controller == null)
                {
                    Log.Error(LOG_MODULE, "未找到SettingsPanelController实例");
                }
            }
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
            
            // 获取设置面板下的所有BaseSettingsComponent派生组件
            var settingsComponents = GetComponentsInChildren<BaseSettingsComponent>(true);
            
            foreach (var component in settingsComponents)
            {
                m_optionComponents.Add(component.gameObject);
                Log.Info(LOG_MODULE, $"找到设置组件: {component.name}");
            }
        }

        /// <summary>
        /// 初始化所有设置组件
        /// </summary>
        private void InitializeAllSettingsComponents()
        {
            Log.Info(LOG_MODULE, "初始化所有设置组件");
            
            if (m_controller == null)
            {
                Log.Error(LOG_MODULE, "控制器为空，无法初始化设置组件");
                return;
            }
            
            foreach (var componentObj in m_optionComponents)
            {
                if (componentObj != null)
                {
                    if (componentObj.TryGetComponent<BaseSettingsComponent>(out var settingsComponent))
                    {
                        settingsComponent.Initialize(m_controller);
                        Log.DebugLog(LOG_MODULE, $"已初始化设置组件: {componentObj.name}");
                    }
                }
            }
        }

        /// <summary>
        /// 更新所有设置组件的视图
        /// </summary>
        public void UpdateAllSettingsComponents()
        {
            Log.Info(LOG_MODULE, "更新所有设置组件视图");
            
            foreach (var componentObj in m_optionComponents)
            {
                if (componentObj != null)
                {
                    if (componentObj.TryGetComponent<BaseSettingsComponent>(out var settingsComponent))
                    {
                        settingsComponent.UpdateView();
                    }
                }
            }
        }

        #endregion
    }
}