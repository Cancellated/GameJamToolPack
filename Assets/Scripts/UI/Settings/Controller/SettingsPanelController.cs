using Logger;
using MyGame.UI;
using MyGame.Events;
using MyGame.UI.Settings.Model;
using UnityEngine;
using MyGame.UI.Settings.View;

namespace MyGame.UI.Settings.Controller
{
    /// <summary>
    /// 设置面板控制器
    /// 负责处理设置面板的业务逻辑和数据管理
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
            base.Initialize();
            
            if (m_model != null)
            {
                m_model.Initialize();
                // 初始化视图显示当前设置
                UpdateViewFromModel();
            }
            
            Log.Info(LOG_MODULE, "设置面板控制器初始化完成");
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            Log.Info(LOG_MODULE, "设置面板控制器清理资源");
            base.Cleanup();
        }

        #endregion

        #region 视图和模型设置

        /// <summary>
        /// 设置视图引用
        /// </summary>
        /// <param name="view">设置面板视图</param>
        public override void SetView(SettingsPanelView view)
        {
            if (m_view != null && m_view != view)
            {
                m_view.UnbindController();
            }
            
            m_view = view;
            
            if (m_view != null)
            {
                m_view.BindController(this);
                if (m_model != null)
                {
                    UpdateViewFromModel();
                }
            }
        }

        /// <summary>
        /// 设置模型引用
        /// </summary>
        /// <param name="model">设置面板模型</param>
        public override void SetModel(SettingsModel model)
        {
            if (m_model != null && m_model != model)
            {
                // 移除旧模型的事件监听
                m_model.OnPropertyChanged -= OnModelPropertyChanged;
            }
            
            m_model = model;
            
            if (m_model != null)
            {
                // 添加新模型的事件监听
                m_model.OnPropertyChanged += OnModelPropertyChanged;
                m_model.Initialize();
                if (m_view != null)
                {
                    UpdateViewFromModel();
                }
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 模型属性变更事件处理
        /// </summary>
        /// <param name="propertyName">变更的属性名称</param>
        private void OnModelPropertyChanged(string propertyName)
        {
            // 这里可以根据具体的属性变更来更新视图
            Log.Info(LOG_MODULE, $"设置模型属性变更: {propertyName}");
        }

        #endregion

        #region 业务逻辑

        /// <summary>
        /// 关闭设置面板
        /// </summary>
        public void ClosePanel()
        {
            Log.Info(LOG_MODULE, "关闭设置面板");
            // 触发返回主菜单事件
            GameEvents.TriggerMenuShow(UIType.MainMenu, true);
        }

        /// <summary>
        /// 更新音乐音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
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
        /// <param name="volume">音量值 (0-1)</param>
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

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            if (m_model != null)
            {
                m_model.SaveSettings();
                Log.Info(LOG_MODULE, "设置已保存");
            }
        }

        /// <summary>
        /// 应用设置
        /// </summary>
        public void ApplySettings()
        {
            if (m_model != null)
            {
                m_model.ApplySettings();
                Log.Info(LOG_MODULE, "设置已应用");
            }
        }

        /// <summary>
        /// 从模型更新视图
        /// </summary>
        private void UpdateViewFromModel()
        {
            if (m_view == null || m_model == null)
                return;

            m_view.UpdateMusicVolumeSlider(m_model.MusicVolume);
            m_view.UpdateSfxVolumeSlider(m_model.SfxVolume);
            m_view.UpdateQualityDropdown(m_model.QualityLevel);
            m_view.UpdateFullscreenToggle(m_model.Fullscreen);
            m_view.UpdateResolutionDropdown(m_model.ResolutionIndex);
            m_view.UpdateInvertYAxisToggle(m_model.InvertYAxis);
        }

        #endregion
    }
}