using UnityEngine;
using TMPro;
using Logger;
using System.Collections.Generic;
using UnityEngine.UI;
using MyGame.UI.Components;

namespace MyGame.UI.Settings.Components
{
    /// <summary>
    /// 图形设置组件
    /// 负责处理图形相关设置的UI和交互
    /// </summary>
    public class GraphicsSettingsComponent : BaseSettingsComponent
    {
        #region 字段

        [Header("Graphics Settings")]
        [Tooltip("画质等级下拉框")]
        [SerializeField] private TMP_Dropdown m_qualityDropdown;

        [Tooltip("全屏开关")]
        [SerializeField] private ToggleSwitch m_fullscreenToggle;

        [Tooltip("分辨率下拉框")]
        [SerializeField] private TMP_Dropdown m_resolutionDropdown;

        private const string LOG_MODULE = LogModules.SETTINGS;
        private Resolution[] m_resolutions;

        #endregion

        #region 抽象方法实现

        /// <summary>
        /// 初始化图形设置组件的UI和数据
        /// </summary>
        protected override void InitializeComponent()
        {
            Log.Info(LOG_MODULE, "初始化图形设置组件");
            InitializeResolutionDropdown();
            InitializeQualityDropdown();
        }

        /// <summary>
        /// 绑定图形设置相关的用户交互事件
        /// </summary>
        protected override void BindEvents()
        {
            // 绑定画质设置事件
            if (m_qualityDropdown != null)
            {
                m_qualityDropdown.onValueChanged.AddListener(OnQualityLevelChanged);
            }

            // 绑定全屏设置事件
            if (m_fullscreenToggle != null)
            {
                m_fullscreenToggle.OnValueChanged += OnFullscreenChanged;
            }

            // 绑定分辨率设置事件
            if (m_resolutionDropdown != null)
            {
                m_resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }
        }

        /// <summary>
        /// 更新图形设置组件的显示状态
        /// </summary>
        public override void UpdateView()
        {
            if (m_controller == null)
                return;

            Log.Info(LOG_MODULE, "更新图形设置组件视图");
            // 当前架构中，设置组件无法直接访问控制器的模型数据
            // 后续可以考虑在SettingsPanelController中添加获取设置的方法
        }

        /// <summary>
        /// 清理图形设置组件资源，解绑事件
        /// </summary>
        protected override void Cleanup()
        {
            // 解绑事件监听
            if (m_qualityDropdown != null)
            {
                m_qualityDropdown.onValueChanged.RemoveListener(OnQualityLevelChanged);
            }

            if (m_fullscreenToggle != null)
            {
                m_fullscreenToggle.OnValueChanged -= OnFullscreenChanged;
            }

            if (m_resolutionDropdown != null)
            {
                m_resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            }
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 画质等级变化事件处理
        /// </summary>
        /// <param name="value">新的画质等级索引</param>
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
        /// <param name="value">新的全屏状态</param>
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
        /// <param name="value">新的分辨率索引</param>
        private void OnResolutionChanged(int value)
        {
            if (m_controller != null)
            {
                m_controller.UpdateResolutionIndex(value);
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

            // 清空现有选项
            m_resolutionDropdown.ClearOptions();

            // 获取系统支持的所有分辨率
            m_resolutions = Screen.resolutions;
            
            // 移除重复的分辨率
            List<Resolution> uniqueResolutions = new();
            HashSet<string> resolutionKeys = new();

            foreach (Resolution resolution in m_resolutions)
            {
                string key = $"{resolution.width}x{resolution.height}@{resolution.refreshRateRatio.value}Hz";
                if (!resolutionKeys.Contains(key))
                {
                    resolutionKeys.Add(key);
                    uniqueResolutions.Add(resolution);
                }
            }

            // 更新分辨率数组为去重后的数组
            m_resolutions = uniqueResolutions.ToArray();

            // 生成分辨率选项文本
            List<string> options = new();
            for (int i = 0; i < m_resolutions.Length; i++)
            {
                string resolutionText = $"{m_resolutions[i].width}x{m_resolutions[i].height} ({m_resolutions[i].refreshRateRatio.value}Hz)";
                options.Add(resolutionText);
            }

            // 添加选项到下拉框
            m_resolutionDropdown.AddOptions(options);

            // 设置当前分辨率为默认选中项
            SetCurrentResolution();
        }

        /// <summary>
        /// 设置当前分辨率为选中项
        /// </summary>
        private void SetCurrentResolution()
        {
            if (m_resolutionDropdown == null || m_resolutions == null || m_resolutions.Length == 0)
                return;

            // 查找当前分辨率在数组中的索引
            Resolution currentResolution = Screen.currentResolution;
            int currentResolutionIndex = 0;

            for (int i = 0; i < m_resolutions.Length; i++)
            {
                if (m_resolutions[i].width == currentResolution.width &&
                    m_resolutions[i].height == currentResolution.height)
                {
                    currentResolutionIndex = i;
                    break;
                }
            }

            // 设置选中项
            m_resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        }

        /// <summary>
        /// 初始化画质下拉框
        /// </summary>
        private void InitializeQualityDropdown()
        {
            if (m_qualityDropdown == null)
                return;

            // 清空现有选项
            m_qualityDropdown.ClearOptions();

            // 获取所有画质等级名称
            string[] qualityNames = QualitySettings.names;

            // 添加到下拉框
            m_qualityDropdown.AddOptions(new List<string>(qualityNames));

            // 设置当前画质等级为默认选中项
            m_qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
        }

        #endregion
    }
}