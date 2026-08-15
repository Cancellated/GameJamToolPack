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

        private const string LOG_MODULE = LogModules.SETTINGS + "View";

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
        }

        /// <summary>
        /// 初始化面板：绑定按钮事件并初始化设置组件
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Log.Info(LOG_MODULE, "初始化设置面板");
            BindButtonEvents();
            InitializeAllSettingsComponents();
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
            
            // 获取所有继承自BaseSettingsComponent的组件
            var settingsComponents = GetComponentsInChildren<BaseSettingsComponent>(true);
            
            // 遍历并记录每个组件的详细信息
            int addedCount = 0;
            foreach (var component in settingsComponents)
            {
                if (component != null && component.gameObject != null)
                {
                    m_optionComponents.Add(component.gameObject);
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
            
            foreach (var componentObj in m_optionComponents)
            {
                if (componentObj == null)
                {
                    Log.Warning(LOG_MODULE, "发现空的设置组件对象，跳过初始化");
                    continue;
                }
                
                if (componentObj.TryGetComponent<BaseSettingsComponent>(out var settingsComponent))
                {
                    settingsComponent.Initialize(m_controller);
                    // 立即更新视图以显示当前设置值
                    settingsComponent.UpdateView();
                    initializedCount++;
                }
                else
                {
                    Log.Warning(LOG_MODULE, $"对象 {componentObj.name} 不包含 BaseSettingsComponent 组件");
                }
            }
            Log.Info(LOG_MODULE, $"所有设置组件初始化完成，成功初始化: {initializedCount}/{m_optionComponents.Count}");
        }

        /// <summary>
        /// 更新所有设置组件的视图
        /// </summary>
        public void UpdateAllSettingsComponents()
        {
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