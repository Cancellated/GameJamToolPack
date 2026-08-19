namespace MyGame.Audio
{
    /// <summary>
    /// 音频服务抽象接口。
    /// 设置模块只依赖此接口，不依赖具体音频实现；
    /// 将来替换为 AudioMixer / Wwise 等实现时，设置链路无需修改。
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// 当前音乐音量（0~1）。
        /// </summary>
        float MusicVolume { get; }

        /// <summary>
        /// 当前音效音量（0~1）。
        /// </summary>
        float SfxVolume { get; }

        /// <summary>
        /// 设置音乐音量（自动钳制到 0~1）。
        /// </summary>
        void SetMusicVolume(float volume01);

        /// <summary>
        /// 设置音效音量（自动钳制到 0~1）。
        /// </summary>
        void SetSfxVolume(float volume01);

        /// <summary>
        /// 播放背景音乐；传 null 则停止音乐通道。
        /// </summary>
        void PlayMusic(UnityEngine.AudioClip clip, bool loop = true);

        /// <summary>
        /// 停止背景音乐。
        /// </summary>
        void StopMusic();

        /// <summary>
        /// 播放一次音效（使用当前 SfxVolume）。
        /// </summary>
        void PlaySfx(UnityEngine.AudioClip clip);
    }

    /// <summary>
    /// 当前音频服务的轻量定位器。
    /// 由 AudioManager 在 Awake 注册、OnDestroy 注销。
    /// </summary>
    public static class AudioServiceLocator
    {
        public static IAudioService Current { get; set; }
    }
}
