using MyGame.UI;
using UnityEngine;

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

        #endregion

        #region 属性

        /// <summary>
        /// 音乐音量
        /// </summary>
        public float MusicVolume
        {
            get { return m_musicVolume; }
            set { SetProperty(ref m_musicVolume, value, nameof(MusicVolume)); }
        }

        /// <summary>
        /// 音效音量
        /// </summary>
        public float SfxVolume
        {
            get { return m_sfxVolume; }
            set { SetProperty(ref m_sfxVolume, value, nameof(SfxVolume)); }
        }

        /// <summary>
        /// 画质等级
        /// </summary>
        public int QualityLevel
        {
            get { return m_qualityLevel; }
            set { SetProperty(ref m_qualityLevel, value, nameof(QualityLevel)); }
        }

        /// <summary>
        /// 是否全屏
        /// </summary>
        public bool Fullscreen
        {
            get { return m_fullscreen; }
            set { SetProperty(ref m_fullscreen, value, nameof(Fullscreen)); }
        }

        /// <summary>
        /// 分辨率索引
        /// </summary>
        public int ResolutionIndex
        {
            get { return m_resolutionIndex; }
            set { SetProperty(ref m_resolutionIndex, value, nameof(ResolutionIndex)); }
        }

        /// <summary>
        /// 是否反转Y轴
        /// </summary>
        public bool InvertYAxis
        {
            get { return m_invertYAxis; }
            set { SetProperty(ref m_invertYAxis, value, nameof(InvertYAxis)); }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化设置数据
        /// 从PlayerPrefs加载保存的设置
        /// </summary>
        public override void Initialize()
        {
            // 从PlayerPrefs加载设置
            MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
            SfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1.0f);
            QualityLevel = PlayerPrefs.GetInt("QualityLevel", 2);
            Fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            ResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
            InvertYAxis = PlayerPrefs.GetInt("InvertYAxis", 0) == 1;
        }

        /// <summary>
        /// 保存设置到PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("SfxVolume", SfxVolume);
            PlayerPrefs.SetInt("QualityLevel", QualityLevel);
            PlayerPrefs.SetInt("Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("ResolutionIndex", ResolutionIndex);
            PlayerPrefs.SetInt("InvertYAxis", InvertYAxis ? 1 : 0);
            
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 应用设置到游戏
        /// </summary>
        public void ApplySettings()
        {
            // 应用画质设置
            QualitySettings.SetQualityLevel(QualityLevel);
            
            // 应用分辨率和全屏设置
            // 这里可以添加分辨率的具体实现
            
            // 应用音量设置
            // 这里可以添加音量的具体实现
        }

        #endregion
    }
}