namespace MyGame.Data
{
    /// <summary>
    /// 设置项 PlayerPrefs key 的唯一事实来源。
    /// SettingsModel / GameSettings / SaveManager 均使用此常量，避免字符串散落导致不同步。
    /// </summary>
    public static class SettingsKeys
    {
        public const string MusicVolume = "MusicVolume";
        public const string SfxVolume = "SfxVolume";
        public const string QualityLevel = "QualityLevel";
        public const string Fullscreen = "Fullscreen";
        public const string ResolutionIndex = "ResolutionIndex";
        public const string InvertYAxis = "InvertYAxis";
    }
}
