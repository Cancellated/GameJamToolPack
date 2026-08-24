using System;

namespace MyGame.Data
{
    /// <summary>
    /// 存档数据类（中立版）。
    /// 包含游戏设置、元数据，以及一段玩法自有的 JSON 字符串。
    /// 框架核心不再内置任何玩法进度字段（关卡/任务/数值等），
    /// 玩法数据通过 IGameplaySaveProvider 注入，核心只透传、不解析。
    /// </summary>
    [Serializable]
    public class SaveData
    {
        #region 玩法数据（不透明负载）

        /// <summary>
        /// 玩法自定义数据 JSON（由 IGameplaySaveProvider 生成）。
        /// 核心只负责保存与回传，不解析其内部结构；具体 Schema 与迁移由玩法负责。
        /// </summary>
        public string gameplayDataJson;

        /// <summary>
        /// 存档槽位显示的进度摘要（由 IGameplaySaveProvider 提供，纯文本透传）。
        /// 例如"第3章 码头"；为空时 UI 显示"无进度信息"。
        /// </summary>
        public string progressSummary;

        #endregion

        #region 游戏设置

        public float musicVolume; // 音乐音量
        public float sfxVolume; // 音效音量
        public int qualityLevel; // 画质等级
        public bool fullscreen; // 是否全屏
        public int resolutionIndex; // 分辨率索引
        public bool invertYAxis; // 是否反转Y轴 

        #endregion

        #region 元数据

        /// <summary>
        /// 存档时间戳（本地时间，用于 UI 显示）
        /// </summary>
        public string saveTime;

        /// <summary>
        /// 存档时间戳（UTC ISO-8601，用于排序与跨时区比较）
        /// </summary>
        public string saveTimeUtc;

        /// <summary>
        /// 存档结构版本号，用于数据迁移；与 Application.version（游戏发布版本）相互独立
        /// </summary>
        public int dataVersion;

        /// <summary>
        /// 当前存档数据结构版本。
        /// v1：内置 GameProgress（关卡/任务/数值字段），已废弃并从代码中移除，不再支持；
        /// v2：移除内置玩法字段，改为 gameplayDataJson + progressSummary 中立负载。
        /// 结构变更时 +1，并在 JsonSaveSystem 中补充迁移逻辑。
        /// </summary>
        public const int CURRENT_DATA_VERSION = 2;

        /// <summary>
        /// 存档版本号（游戏发布版本）
        /// </summary>
        public string version;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数：创建一份只含设置默认值与元数据的空存档。
        /// </summary>
        public SaveData()
        {
            gameplayDataJson = string.Empty;
            progressSummary = string.Empty;
            SetSaveTimeMetadata();
            dataVersion = CURRENT_DATA_VERSION;
            version = "1.0.0"; // 使用默认版本号，避免在序列化期间调用Application.version
        }

        /// <summary>
        /// 构造函数：从游戏设置创建存档数据。
        /// </summary>
        /// <param name="settings">游戏设置</param>
        public SaveData(GameSettings settings) : this()
        {
            UpdateSettings(settings);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 刷新存档时间元数据：本地显示时间 + UTC ISO-8601 时间。
        /// </summary>
        public void SetSaveTimeMetadata()
        {
            DateTime now = DateTime.Now;
            saveTime = now.ToString("yyyy-MM-dd HH:mm:ss");
            saveTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        /// <summary>
        /// 写入玩法数据负载与槽位摘要。空值统一归一化为空字符串，避免 UI/玩法层判空负担。
        /// </summary>
        /// <param name="gameplayJson">玩法自定义 JSON（可为 null/空串）</param>
        /// <param name="summary">槽位显示摘要（可为 null/空串）</param>
        public void SetGameplayData(string gameplayJson, string summary)
        {
            gameplayDataJson = string.IsNullOrEmpty(gameplayJson) ? string.Empty : gameplayJson;
            progressSummary = string.IsNullOrEmpty(summary) ? string.Empty : summary;
        }

        /// <summary>
        /// 更新存档中的设置信息
        /// </summary>
        /// <param name="settings">游戏设置</param>
        public void UpdateSettings(GameSettings settings)
        {
            if (settings == null) return;

            musicVolume = settings.MusicVolume;
            sfxVolume = settings.SfxVolume;
            qualityLevel = settings.QualityLevel;
            fullscreen = settings.Fullscreen;
            resolutionIndex = settings.ResolutionIndex;
            invertYAxis = settings.InvertYAxis;
        }

        /// <summary>
        /// 创建游戏设置对象
        /// </summary>
        /// <returns>游戏设置对象</returns>
        public GameSettings CreateGameSettings()
        {
            return new GameSettings
            {
                MusicVolume = musicVolume,
                SfxVolume = sfxVolume,
                QualityLevel = qualityLevel,
                Fullscreen = fullscreen,
                ResolutionIndex = resolutionIndex,
                InvertYAxis = invertYAxis
            };
        }

        /// <summary>
        /// 归一化：确保字符串字段非空。
        /// JsonUtility 反序列化旧存档时，缺失字段可能为 null，
        /// 统一补齐，避免 UI/玩法层出现 NullReferenceException。
        /// </summary>
        public void EnsureInitialized()
        {
            gameplayDataJson ??= string.Empty;
            progressSummary ??= string.Empty;
        }

        #endregion
    }
}
