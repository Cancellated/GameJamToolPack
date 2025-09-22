using UnityEngine;
using MyGame.UI.Settings.View;
using MyGame.UI.Settings.Model;
using MyGame.UI;
using Logger;

namespace MyGame.UI.Settings.Controller
{
    /// <summary>
    /// 设置面板控制器
    /// 负责处理设置面板的业务逻辑
    /// </summary>
    public class SettingsPanelController : BaseController<SettingsPanelView, SettingsModel>
    {
        #region 字段

        private const string LOG_MODULE = LogModules.SETTINGS;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化控制器
        /// </summary>
        public override void Initialize()
        {
            Log.Info(LOG_MODULE, "初始化设置面板控制器");
            base.Initialize();
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            Log.Info(LOG_MODULE, "清理设置面板控制器");
            UnbindModelEvents();
            base.Cleanup();
        }

        /// <summary>
        /// 当视图设置后调用
        /// </summary>
        protected override void OnViewSet()
        {
            base.OnViewSet();
            Log.Info(LOG_MODULE, "视图已设置");
        }

        /// <summary>
        /// 当模型设置后调用
        /// </summary>
        protected override void OnModelSet()
        {
            base.OnModelSet();
            Log.Info(LOG_MODULE, "模型已设置");
            
            if (m_model != null)
            {
                BindModelEvents();
                
                // 初始化视图显示当前设置
                if (m_view != null)
                {
                    UpdateViewWithCurrentSettings();
                }
            }
        }

        #endregion

        #region 模型事件绑定

        /// <summary>
        /// 绑定模型事件
        /// </summary>
        private void BindModelEvents()
        {
            if (m_model != null)
            {
                // 使用ObservableModel的通用OnPropertyChanged事件
                m_model.OnPropertyChanged += HandleModelPropertyChanged;
            }
        }

        /// <summary>
        /// 解绑模型事件
        /// </summary>
        private void UnbindModelEvents()
        {
            if (m_model != null)
            {
                m_model.OnPropertyChanged -= HandleModelPropertyChanged;
            }
        }

        /// <summary>
        /// 处理模型属性变化事件
        /// </summary>
        /// <param name="propertyName">变化的属性名</param>
        private void HandleModelPropertyChanged(string propertyName)
        {
            if (m_view == null)
                return;

            Log.Info(LOG_MODULE, $"检测到模型属性变化: {propertyName}");
            
            // 调用视图的更新方法，该方法会通知所有设置组件更新其视图
            m_view.UpdateAllSettingsViews();
        }

        #endregion

        #region 页面切换方法

        /// <summary>
        /// 切换到指定页面
        /// </summary>
        /// <param name="page">页面类型</param>
        public void SwitchToPage(SettingsPanelView.SettingsPage page)
        {
            if (m_view != null)
            {
                m_view.SwitchPage(page);
            }
        }

        /// <summary>
        /// 切换到图形设置页面
        /// </summary>
        public void SwitchToGraphicsPage()
        {
            SwitchToPage(SettingsPanelView.SettingsPage.Graphics);
        }

        /// <summary>
        /// 切换到声音设置页面
        /// </summary>
        public void SwitchToAudioPage()
        {
            SwitchToPage(SettingsPanelView.SettingsPage.Audio);
        }

        /// <summary>
        /// 切换到控制设置页面
        /// </summary>
        public void SwitchToControlsPage()
        {
            SwitchToPage(SettingsPanelView.SettingsPage.Controls);
        }

        #endregion

        #region 设置更新方法

        /// <summary>
        /// 更新音乐音量
        /// </summary>
        /// <param name="volume">音量值</param>
        public void UpdateMusicVolume(float volume)
        {
            if (m_model != null)
            {
                m_model.MusicVolume = volume;
                Log.Info(LOG_MODULE, $"更新音乐音量: {volume}");
            }
        }

        /// <summary>
        /// 更新音效音量
        /// </summary>
        /// <param name="volume">音量值</param>
        public void UpdateSfxVolume(float volume)
        {
            if (m_model != null)
            {
                m_model.SfxVolume = volume;
                Log.Info(LOG_MODULE, $"更新音效音量: {volume}");
            }
        }

        /// <summary>
        /// 更新画质等级
        /// </summary>
        /// <param name="qualityLevel">画质等级</param>
        public void UpdateQualityLevel(int qualityLevel)
        {
            if (m_model != null)
            {
                m_model.QualityLevel = qualityLevel;
                Log.Info(LOG_MODULE, $"更新画质等级: {qualityLevel}");
            }
        }

        /// <summary>
        /// 更新全屏设置
        /// </summary>
        /// <param name="isFullscreen">是否全屏</param>
        public void UpdateFullscreen(bool isFullscreen)
        {
            if (m_model != null)
            {
                m_model.Fullscreen = isFullscreen;
                Log.Info(LOG_MODULE, $"更新全屏设置: {isFullscreen}");
            }
        }

        /// <summary>
        /// 更新分辨率索引
        /// </summary>
        /// <param name="resolutionIndex">分辨率索引</param>
        public void UpdateResolutionIndex(int resolutionIndex)
        {
            if (m_model != null)
            {
                m_model.ResolutionIndex = resolutionIndex;
                Log.Info(LOG_MODULE, $"更新分辨率索引: {resolutionIndex}");
            }
        }

        /// <summary>
        /// 更新Y轴反转设置
        /// </summary>
        /// <param name="invertYAxis">是否反转Y轴</param>
        public void UpdateInvertYAxis(bool invertYAxis)
        {
            if (m_model != null)
            {
                m_model.InvertYAxis = invertYAxis;
                Log.Info(LOG_MODULE, $"更新Y轴反转设置: {invertYAxis}");
            }
        }

        #endregion

        #region 设置操作方法

        /// <summary>
        /// 应用设置
        /// </summary>
        public void ApplySettings()
        {
            Log.Info(LOG_MODULE, "应用设置");
            m_model?.ApplySettings();
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            Log.Info(LOG_MODULE, "保存设置");
            if (m_model != null)
            {
                m_model.SaveSettings();
                m_model.ApplySettings();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 用当前设置更新视图
        /// </summary>
        private void UpdateViewWithCurrentSettings()
        {
            if (m_model == null || m_view == null)
                return;

            Log.Info(LOG_MODULE, "用当前设置更新视图");
            
            // 调用视图的更新方法，该方法会通知所有设置组件更新其视图
            m_view.UpdateAllSettingsViews();
        }
        #endregion
    }
}