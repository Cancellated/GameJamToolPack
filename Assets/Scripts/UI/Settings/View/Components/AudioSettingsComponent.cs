using UnityEngine;
using Logger;
using MyGame.UI.Components.SettingSlider;

namespace MyGame.UI.Settings.Components
{
    /// <summary>
    /// 音频设置组件。
    /// 只依赖 SettingSliderComponent 包装组件，避免 Inspector 中出现原始 Slider 与包装组件两套重复字段。
    /// 包装组件负责百分比显示与事件转发。
    /// </summary>
    public class AudioSettingsComponent : BaseSettingsComponent
    {
        #region 字段

        [Header("Audio Settings")]
        [Tooltip("音乐音量滑块包装组件")]
        [SerializeField] private SettingSliderComponent m_musicSettingSlider;

        [Tooltip("音效音量滑块包装组件")]
        [SerializeField] private SettingSliderComponent m_sfxSettingSlider;

        private const string LOG_MODULE = LogModules.SETTINGS;

        private bool m_eventsBound;

        #endregion

        /// <summary>
        /// 生命周期标记：确认新代码已运行。
        /// </summary>
        private void Start()
        {
            Log.Info(LOG_MODULE, "AudioSettingsComponent Start（包装组件版本）");
        }

        /// <summary>
        /// 兜底绑定：若 SettingsPanelView 因初始化顺序尚未注入控制器，
        /// 在控制器可用后的第一帧补做解析/绑定/回显。
        /// </summary>
        private void Update()
        {
            if (m_eventsBound || m_controller == null)
            {
                return;
            }

            ResolveSliderWrappers();
            BindEvents();
            UpdateView();
            Log.Info(LOG_MODULE, "AudioSettingsComponent 延迟绑定完成");
        }

        #region 抽象方法实现

        /// <summary>
        /// 初始化音频设置组件的UI和数据
        /// </summary>
        protected override void InitializeComponent()
        {
            ResolveSliderWrappers();

            m_musicSettingSlider?.SetValueWithoutNotify(1f);
            m_sfxSettingSlider?.SetValueWithoutNotify(1f);

            if (m_musicSettingSlider == null)
            {
                Log.Warning(LOG_MODULE, "未找到音乐音量滑块包装组件");
            }
            if (m_sfxSettingSlider == null)
            {
                Log.Warning(LOG_MODULE, "未找到音效音量滑块包装组件");
            }
        }

        /// <summary>
        /// 绑定音频设置相关的用户交互事件
        /// </summary>
        protected override void BindEvents()
        {
            if (m_eventsBound)
            {
                return;
            }

            m_eventsBound = true;

            if (m_musicSettingSlider != null)
            {
                m_musicSettingSlider.OnValueChanged += OnMusicVolumeChanged;
                Log.Info(LOG_MODULE, "音乐音量已绑定 SettingSliderComponent 事件");
            }
            else
            {
                Log.Error(LOG_MODULE, "音乐音量滑块引用为空，无法绑定事件");
            }

            if (m_sfxSettingSlider != null)
            {
                m_sfxSettingSlider.OnValueChanged += OnSfxVolumeChanged;
                Log.Info(LOG_MODULE, "音效音量已绑定 SettingSliderComponent 事件");
            }
            else
            {
                Log.Error(LOG_MODULE, "音效音量滑块引用为空，无法绑定事件");
            }
        }

        /// <summary>
        /// 更新音频设置组件的显示状态
        /// </summary>
        public override void UpdateView()
        {
            if (m_controller == null)
                return;

            m_musicSettingSlider?.SetValueWithoutNotify(m_controller.GetMusicVolume());
            m_sfxSettingSlider?.SetValueWithoutNotify(m_controller.GetSfxVolume());
        }

        /// <summary>
        /// 清理音频设置组件资源，解绑事件
        /// </summary>
        protected override void Cleanup()
        {
            if (!m_eventsBound)
            {
                return;
            }

            m_eventsBound = false;

            if (m_musicSettingSlider != null)
            {
                m_musicSettingSlider.OnValueChanged -= OnMusicVolumeChanged;
            }
            if (m_sfxSettingSlider != null)
            {
                m_sfxSettingSlider.OnValueChanged -= OnSfxVolumeChanged;
            }
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 音乐音量变化事件处理
        /// </summary>
        private void OnMusicVolumeChanged(float value)
        {
            Log.InfoWithCooldown(LOG_MODULE, "音乐音量滑块变化: " + value, "audio_music_volume", 0.5f);
            m_controller?.UpdateMusicVolume(value);
        }

        /// <summary>
        /// 音效音量变化事件处理
        /// </summary>
        private void OnSfxVolumeChanged(float value)
        {
            Log.InfoWithCooldown(LOG_MODULE, "音效音量滑块变化: " + value, "audio_sfx_volume", 0.5f);
            m_controller?.UpdateSfxVolume(value);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// Inspector 未拖入时，按 GameObject 名称自动查找 MusicSlider / SfxSlider 包装组件。
        /// </summary>
        private void ResolveSliderWrappers()
        {
            if (m_musicSettingSlider == null || m_sfxSettingSlider == null)
            {
                SettingSliderComponent[] sliders = GetComponentsInChildren<SettingSliderComponent>(true);
                foreach (SettingSliderComponent slider in sliders)
                {
                    if (slider == null)
                    {
                        continue;
                    }

                    string objectName = slider.gameObject.name;
                    if (m_musicSettingSlider == null && objectName.Contains("Music", System.StringComparison.OrdinalIgnoreCase))
                    {
                        m_musicSettingSlider = slider;
                    }
                    else if (m_sfxSettingSlider == null && objectName.Contains("Sfx", System.StringComparison.OrdinalIgnoreCase))
                    {
                        m_sfxSettingSlider = slider;
                    }
                }

                Log.Info(LOG_MODULE,
                    $"音频滑块解析: 子物体SettingSlider数量={sliders.Length}, music={(m_musicSettingSlider != null ? "OK" : "NULL")}, sfx={(m_sfxSettingSlider != null ? "OK" : "NULL")}");
            }
        }

        #endregion
    }
}
