using System;
using UnityEngine;
using Logger;
using MyGame.UI.Settings.Model;
using MyGame.UI.Settings.View;

namespace MyGame.UI.Settings.Controller
{
    /// <summary>
    /// 设置面板控制器
    /// 负责处理设置面板的逻辑和设置更新
    /// </summary>
    public class SettingsPanelController : BaseController<SettingsPanelView, SettingsModel>
    {
        #region 字段

        [Header("Settings Panel View")]
        [Tooltip("设置面板视图引用")]
        [SerializeField] private SettingsPanelView m_settingsPanelView;

        private const string LOG_MODULE = LogModules.SETTINGS;

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 控制器的Awake方法
        /// 初始化组件和绑定事件
        /// </summary>
        protected virtual void Awake()
        {
            Log.Info(LOG_MODULE, "设置面板控制器初始化");
            base.CreateAndInitializeModel();
            // 设置视图引用
            if (m_settingsPanelView != null)
            {
                SetView(m_settingsPanelView);
                Log.DebugLog(LOG_MODULE, "设置面板视图: 存在");
            }
            else
            {
                Log.DebugLog(LOG_MODULE, "设置面板视图: 不存在");
            }
        }

        /// <summary>
        /// 当控制器被启用时调用
        /// </summary>
        protected virtual void OnEnable()
        {
            Log.DebugLog(LOG_MODULE, "设置面板控制器启用");
        }
        
        /// <summary>
        /// 模型设置后的回调
        /// 在模型设置完成后执行绑定事件和初始化面板的操作
        /// </summary>
        protected override void OnModelSet()
        {
            Log.DebugLog(LOG_MODULE, "设置面板控制器: 模型已设置");
            BindModelEvents();
            InitializePanel();
        }

        /// <summary>
        /// 当游戏对象禁用时调用
        /// </summary>
        private void OnDisable()
        {
            Log.Info(LOG_MODULE, "设置面板控制器禁用");
            UnbindModelEvents();
        }

        /// <summary>
        /// 解绑设置模型事件
        /// </summary>
        private void UnbindModelEvents()
        {
            Log.Info(LOG_MODULE, "解绑设置模型事件");
            if (m_model != null)
            {
                m_model.OnPropertyChanged -= HandleModelPropertyChanged;
                Log.DebugLog(LOG_MODULE, "已解绑设置模型属性变化事件");
            }
        }

        #endregion

        #region 初始化和事件绑定

        /// <summary>
        /// 初始化设置面板
        /// </summary>
        private void InitializePanel()
        {
            Log.Info(LOG_MODULE, "初始化设置面板");
            if (m_settingsPanelView != null)
            {
                m_settingsPanelView.Initialize();
                UpdateViewWithCurrentSettings();
            }
            else
            {
                Log.Error(LOG_MODULE, "设置面板视图为空，无法初始化");
            }
        }

        /// <summary>
        /// 绑定设置模型事件
        /// </summary>
        private void BindModelEvents()
        {
            Log.Info(LOG_MODULE, "绑定设置模型事件");
            if (m_model != null)
            {
                m_model.OnPropertyChanged += HandleModelPropertyChanged;
                Log.DebugLog(LOG_MODULE, "已绑定设置模型属性变化事件");
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法绑定事件");
            }
        }

        #endregion

        #region 属性变化处理

        /// <summary>
        /// 处理设置模型属性变化
        /// </summary>
        /// <param name="propertyName">变化的属性名称</param>
        private void HandleModelPropertyChanged(string propertyName)
        {
            Log.Info(LOG_MODULE, "检测到设置模型属性变化: " + propertyName);
            UpdateViewWithCurrentSettings();
        }

        #endregion

        #region 页面切换


        #endregion

        #region 设置更新方法

        /// <summary>
        /// 更新音乐音量设置
        /// </summary>
        /// <param name="volume">新的音量值</param>
        public void UpdateMusicVolume(float volume)
        {
            Log.Info(LOG_MODULE, "更新音乐音量: " + volume);
            if (m_model != null)
            {
                Log.DebugLog(LOG_MODULE, "设置模型: 更新前音乐音量=" + m_model.MusicVolume + ", 更新后音量=" + volume);
                m_model.MusicVolume = volume;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法更新音乐音量");
            }
        }

        /// <summary>
        /// 更新音效音量设置
        /// </summary>
        /// <param name="volume">新的音量值</param>
        public void UpdateSfxVolume(float volume)
        {
            Log.Info(LOG_MODULE, "更新音效音量: " + volume);
            if (m_model != null)
            {
                Log.DebugLog(LOG_MODULE, "设置模型: 更新前音效音量=" + m_model.SfxVolume + ", 更新后音量=" + volume);
                m_model.SfxVolume = volume;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法更新音效音量");
            }
        }

        /// <summary>
        /// 更新画质级别设置
        /// </summary>
        /// <param name="qualityLevel">新的画质级别</param>
        public void UpdateQualityLevel(int qualityLevel)
        {
            Log.Info(LOG_MODULE, "更新画质级别: " + qualityLevel);
            if (m_model != null)
            {
                Log.DebugLog(LOG_MODULE, "设置模型: 更新前画质级别=" + m_model.QualityLevel + ", 更新后画质级别=" + qualityLevel);
                m_model.QualityLevel = qualityLevel;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法更新画质级别");
            }
        }

        /// <summary>
        /// 更新全屏状态设置
        /// </summary>
        /// <param name="isFullscreen">是否全屏</param>
        public void UpdateFullscreen(bool isFullscreen)
        {
            Log.Info(LOG_MODULE, "更新全屏状态: " + (isFullscreen ? "全屏" : "窗口"));
            if (m_model != null)
            {
                Log.DebugLog(LOG_MODULE, "设置模型: 更新前全屏状态=" + (m_model.Fullscreen ? "全屏" : "窗口") + ", 更新后全屏状态=" + (isFullscreen ? "全屏" : "窗口"));
                m_model.Fullscreen = isFullscreen;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法更新全屏状态");
            }
        }

        /// <summary>
        /// 更新分辨率索引设置
        /// </summary>
        /// <param name="resolutionIndex">新的分辨率索引</param>
        public void UpdateResolutionIndex(int resolutionIndex)
        {
            Log.Info(LOG_MODULE, "更新分辨率索引: " + resolutionIndex);
            if (m_model != null)
            {
                Log.DebugLog(LOG_MODULE, "设置模型: 更新前分辨率索引=" + m_model.ResolutionIndex + ", 更新后分辨率索引=" + resolutionIndex);
                m_model.ResolutionIndex = resolutionIndex;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法更新分辨率索引");
            }
        }

        /// <summary>
        /// 更新Y轴反转设置
        /// </summary>
        /// <param name="invertYAxis">是否反转Y轴</param>
        public void UpdateInvertYAxis(bool invertYAxis)
        {
            Log.Info(LOG_MODULE, "更新Y轴反转设置: " + (invertYAxis ? "反转" : "不反转"));
            if (m_model != null)
            {
                Log.DebugLog(LOG_MODULE, "设置模型: 更新前Y轴反转=" + (m_model.InvertYAxis ? "反转" : "不反转") + ", 更新后Y轴反转=" + (invertYAxis ? "反转" : "不反转"));
                m_model.InvertYAxis = invertYAxis;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法更新Y轴反转设置");
            }
        }

        #endregion

        #region 设置操作方法

        /// <summary>
        /// 应用当前设置
        /// </summary>
        public void ApplySettings()
        {
            Log.Info(LOG_MODULE, "应用设置");
            if (m_model != null)
            {
                m_model.ApplySettings();
                Log.DebugLog(LOG_MODULE, "设置已成功应用");
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法应用设置");
            }
        }

        /// <summary>
        /// 保存当前设置
        /// </summary>
        public void SaveSettings()
        {
            Log.Info(LOG_MODULE, "保存设置");
            if (m_model != null)
            {
                m_model.SaveSettings();
                Log.DebugLog(LOG_MODULE, "设置已成功保存");
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，无法保存设置");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新视图以显示当前设置
        /// </summary>
        private void UpdateViewWithCurrentSettings()
        {
            Log.Info(LOG_MODULE, "更新视图显示当前设置");
            if (m_settingsPanelView != null)
            {
                // 调用视图的UpdateAllSettingsComponents方法更新所有设置组件
                m_settingsPanelView.UpdateAllSettingsComponents();
                Log.DebugLog(LOG_MODULE, "视图更新完成");
            }
            else
            {
                Log.Error(LOG_MODULE, "设置面板视图为空，无法更新视图");
            }
        }

        #endregion

        #region 获取设置值方法

        /// <summary>
        /// 获取音乐音量
        /// </summary>
        /// <returns>音乐音量值</returns>
        public float GetMusicVolume()
        {
            if (m_model != null)
            {
                float volume = m_model.MusicVolume;
                Log.DebugLog(LOG_MODULE, "获取音乐音量: " + volume);
                return volume;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回默认音乐音量");
                return 1f; // 默认最大音量
            }
        }

        /// <summary>
        /// 获取音效音量
        /// </summary>
        /// <returns>音效音量值</returns>
        public float GetSfxVolume()
        {
            if (m_model != null)
            {
                float volume = m_model.SfxVolume;
                Log.DebugLog(LOG_MODULE, "获取音效音量: " + volume);
                return volume;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回默认音效音量");
                return 1f; // 默认最大音量
            }
        }

        /// <summary>
        /// 获取画质级别
        /// </summary>
        /// <returns>画质级别索引</returns>
        public int GetQualityLevel()
        {
            if (m_model != null)
            {
                int qualityLevel = m_model.QualityLevel;
                Log.DebugLog(LOG_MODULE, "获取画质级别: " + qualityLevel + ", 名称: " + QualitySettings.names[qualityLevel]);
                return qualityLevel;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回系统当前画质级别");
                return QualitySettings.GetQualityLevel();
            }
        }

        /// <summary>
        /// 获取全屏状态
        /// </summary>
        /// <returns>是否全屏</returns>
        public bool IsFullscreen()
        {
            if (m_model != null)
            {
                bool isFullscreen = m_model.Fullscreen;
                Log.DebugLog(LOG_MODULE, "获取全屏状态: " + (isFullscreen ? "全屏" : "窗口"));
                return isFullscreen;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回系统当前全屏状态");
                return Screen.fullScreen;
            }
        }

        /// <summary>
        /// 获取分辨率索引
        /// </summary>
        /// <returns>分辨率索引</returns>
        public int GetResolutionIndex()
        {
            if (m_model != null)
            {
                int resolutionIndex = m_model.ResolutionIndex;
                Log.DebugLog(LOG_MODULE, "获取分辨率索引: " + resolutionIndex);
                return resolutionIndex;
            }
            else
            {
                Log.Error(LOG_MODULE, "设置模型为空，返回默认分辨率索引");
                return 0;
            }
        }

        #endregion
    }
}