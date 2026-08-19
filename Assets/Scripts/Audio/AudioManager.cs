using Logger;
using MyGame.Data;
using UnityEngine;

namespace MyGame.Audio
{
    /// <summary>
    /// 最小音频管理器。
    /// 当前实现不依赖任何音频资源：启动时创建持久 Music/SFX 两个 AudioSource，
    /// 负责真实保存并应用音量；玩法需要播放音频时调用 PlayMusic/PlaySfx。
    /// 后续可替换为 AudioMixer 实现，只需保持 IAudioService 契约不变。
    /// </summary>
    public class AudioManager : Singleton<AudioManager>, IAudioService
    {
        private const string LOG_MODULE = LogModules.AUDIO;

        private AudioSource m_musicSource;
        private AudioSource m_sfxSource;
        private float m_musicVolume = 1f;
        private float m_sfxVolume = 1f;

        public float MusicVolume
        {
            get { return m_musicVolume; }
        }

        public float SfxVolume
        {
            get { return m_sfxVolume; }
        }

        protected override void Awake()
        {
            base.Awake();
            CreateAudioSources();

            // 启动时从 PlayerPrefs 恢复上次保存的音量
            GameSettings settings = new();
            settings.LoadFromPlayerPrefs();
            SetMusicVolume(settings.MusicVolume);
            SetSfxVolume(settings.SfxVolume);

            // 注册为当前音频服务
            AudioServiceLocator.Current = this;
            Log.Info(LOG_MODULE, "音频管理器已初始化并注册为当前 IAudioService");
        }

        protected override void OnDestroy()
        {
            if (ReferenceEquals(AudioServiceLocator.Current, this))
            {
                AudioServiceLocator.Current = null;
            }

            StopMusic();
            base.OnDestroy();
        }

        // 创建音乐和音效源 GameObject 并添加 AudioSource 组件
        // 音乐源循环播放，音效源不循环播放
        private void CreateAudioSources()
        {
            GameObject musicGO = new("MusicSource");
            musicGO.transform.SetParent(transform, false);
            m_musicSource = musicGO.AddComponent<AudioSource>();
            m_musicSource.playOnAwake = false;
            m_musicSource.loop = true;
            m_musicSource.spatialBlend = 0f;

            GameObject sfxGO = new("SfxSource");
            sfxGO.transform.SetParent(transform, false);
            m_sfxSource = sfxGO.AddComponent<AudioSource>();
            m_sfxSource.playOnAwake = false;
            m_sfxSource.loop = false;
            m_sfxSource.spatialBlend = 0f;
        }

        // 设置音乐音量（clamp01将音量限制在0-1之间）
        public void SetMusicVolume(float volume01)
        {
            m_musicVolume = Mathf.Clamp01(volume01);
            if (m_musicSource != null)
            {
                m_musicSource.volume = m_musicVolume;
            }
        }
        
        // 设置音效音量（clamp01将音量限制在0-1之间）
        public void SetSfxVolume(float volume01)
        {
            m_sfxVolume = Mathf.Clamp01(volume01);
            if (m_sfxSource != null)
            {
                m_sfxSource.volume = m_sfxVolume;
            }
        }

        // 播放音乐（默认循环播放）
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (m_musicSource == null)
            {
                return;
            }

            if (clip == null)
            {
                StopMusic();
                return;
            }

            m_musicSource.clip = clip;
            m_musicSource.loop = loop;
            m_musicSource.Play();
        }

        // 停止音乐播放
        public void StopMusic()
        {
            if (m_musicSource != null)
            {
                m_musicSource.Stop();
            }
        }

        // 播放音效
        public void PlaySfx(AudioClip clip)
        {
            if (m_sfxSource == null || clip == null)
            {
                return;
            }

            // PlayOneShot 使用独立音量缩放，保证使用当前 SfxVolume
            m_sfxSource.PlayOneShot(clip, m_sfxVolume);
        }
    }
}
