using Logger;
using MyGame.Audio;
using MyGame.Data;
using MyGame.DevTools;
using MyGame.UI;
using MyGame.Utils;
using UnityEngine;
using MyGame.Events;

namespace MyGame.UI.Settings.Model
{
    /// <summary>
    /// 设置面板数据模型
    /// 负责存储和管理设置数据
    /// </summary>
    public class SettingsModel : ObservableModel
    {
        #region 字段

        // 音量设置
        private float m_musicVolume = 1.0f;
        private float m_sfxVolume = 1.0f;

        // 画质设置
        private int m_qualityLevel = 2;
        private bool m_fullscreen = true;
        private int m_resolutionIndex = 0;

        // 游戏设置
        private bool m_invertYAxis = false;

        // 开发者模式：随设置面板保存流程持久化
        private bool m_developerModeEnabled = false;

        /// <summary>是否正在从 PlayerPrefs 加载：加载期间不标记脏状态</summary>
        private bool m_isLoading;

        /// <summary>是否存在尚未保存到 PlayerPrefs 的修改</summary>
        private bool m_hasUnsavedChanges;

        private const string LOG_MODULE = LogModules.SETTINGS + "Model";

        #endregion

        #region 属性

        /// <summary>
        /// 音乐音量
        /// </summary>
        public float MusicVolume
        {
            get { return m_musicVolume; }
            set
            {
                if (SetProperty(ref m_musicVolume, Mathf.Clamp01(value), nameof(MusicVolume)))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 音效音量
        /// </summary>
        public float SfxVolume
        {
            get { return m_sfxVolume; }
            set
            {
                if (SetProperty(ref m_sfxVolume, Mathf.Clamp01(value), nameof(SfxVolume)))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 画质等级
        /// </summary>
        public int QualityLevel
        {
            get { return m_qualityLevel; }
            set
            {
                int maxLevel = Mathf.Max(0, QualitySettings.names.Length - 1);
                int clampedValue = Mathf.Clamp(value, 0, maxLevel);
                if (SetProperty(ref m_qualityLevel, clampedValue, nameof(QualityLevel)))
                {
                    MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// 是否全屏
        /// </summary>
        public bool Fullscreen
        {
            get { return m_fullscreen; }
            set
            {
                if (SetProperty(ref m_fullscreen, value, nameof(Fullscreen)))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 分辨率索引
        /// </summary>
        public int ResolutionIndex
        {
            get { return m_resolutionIndex; }
            set
            {
                int clampedValue = ResolutionUtility.ClampResolutionIndex(value);
                if (SetProperty(ref m_resolutionIndex, clampedValue, nameof(ResolutionIndex)))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 是否反转Y轴
        /// </summary>
        public bool InvertYAxis
        {
            get { return m_invertYAxis; }
            set
            {
                if (SetProperty(ref m_invertYAxis, value, nameof(InvertYAxis)))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 开发者模式：仅在点击“保存”后持久化生效。
        /// </summary>
        public bool DeveloperModeEnabled
        {
            get { return m_developerModeEnabled; }
            set
            {
                if (SetProperty(ref m_developerModeEnabled, value, nameof(DeveloperModeEnabled)))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 是否存在未保存的设置修改。
        /// </summary>
        public bool HasUnsavedChanges
        {
            get { return m_hasUnsavedChanges; }
            private set { SetProperty(ref m_hasUnsavedChanges, value, nameof(HasUnsavedChanges)); }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化设置数据
        /// 从PlayerPrefs加载保存的设置
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // 从 PlayerPrefs 加载设置；加载期间不触发“未保存修改”标记
            m_isLoading = true;
            try
            {
                LoadValuesFromPlayerPrefs();
            }
            finally
            {
                m_isLoading = false;
                HasUnsavedChanges = false;
            }
        }

        /// <summary>
        /// 重新从 PlayerPrefs 读取设置，用于放弃未保存修改。
        /// </summary>
        public void ReloadFromPlayerPrefs()
        {
            m_isLoading = true;
            try
            {
                LoadValuesFromPlayerPrefs();
            }
            finally
            {
                m_isLoading = false;
                HasUnsavedChanges = false;
            }
        }

        private void LoadValuesFromPlayerPrefs()
        {
            MusicVolume = PlayerPrefs.GetFloat(SettingsKeys.MusicVolume, 1.0f);
            SfxVolume = PlayerPrefs.GetFloat(SettingsKeys.SfxVolume, 1.0f);
            QualityLevel = PlayerPrefs.GetInt(SettingsKeys.QualityLevel, 2);
            Fullscreen = PlayerPrefs.GetInt(SettingsKeys.Fullscreen, 1) == 1;
            ResolutionIndex = PlayerPrefs.GetInt(SettingsKeys.ResolutionIndex, 0);
            InvertYAxis = PlayerPrefs.GetInt(SettingsKeys.InvertYAxis, 0) == 1;
            DeveloperModeEnabled = DeveloperMode.GetSavedDeveloperModeEnabled();
        }
        
        /// <summary>
        /// 保存设置到PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(SettingsKeys.MusicVolume, MusicVolume);
            PlayerPrefs.SetFloat(SettingsKeys.SfxVolume, SfxVolume);
            PlayerPrefs.SetInt(SettingsKeys.QualityLevel, QualityLevel);
            PlayerPrefs.SetInt(SettingsKeys.Fullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.ResolutionIndex, ResolutionIndex);
            PlayerPrefs.SetInt(SettingsKeys.InvertYAxis, InvertYAxis ? 1 : 0);
            DeveloperMode.SetDeveloperModeEnabled(DeveloperModeEnabled);
            
            PlayerPrefs.Save();

            HasUnsavedChanges = false;
            
            // 触发设置保存完成事件
            GameEvents.TriggerSettingsSaved();
        }

        /// <summary>
        /// 应用设置到游戏
        /// </summary>
        public void ApplySettings()
        {
            // 应用音量设置：通过 IAudioService 抽象，不依赖具体音频实现
            if (AudioServiceLocator.Current != null)
            {
                AudioServiceLocator.Current.SetMusicVolume(MusicVolume);
                AudioServiceLocator.Current.SetSfxVolume(SfxVolume);
            }

            // 应用画质设置
            QualitySettings.SetQualityLevel(QualityLevel);
            
            // 应用分辨率和全屏设置：使用与设置界面相同的去重分辨率列表，避免索引错位
            if (ResolutionUtility.TryGetResolution(ResolutionIndex, out Resolution selectedResolution))
            {
                Screen.SetResolution(selectedResolution.width, selectedResolution.height, Fullscreen);
                Log.Info(LOG_MODULE, $"应用分辨率设置: {selectedResolution.width}x{selectedResolution.height}, 全屏: {Fullscreen}");
            }
            
            // 触发设置应用完成事件
            GameEvents.TriggerSettingsApplied();
        }

        private void MarkDirty()
        {
            if (m_isLoading)
            {
                return;
            }

            HasUnsavedChanges = true;
        }

        #endregion
    }
}