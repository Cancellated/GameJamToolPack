using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Data
{
    /// <summary>
    /// 存档数据类
    /// 包含游戏进度和设置信息，用于统一管理和持久化
    /// </summary>
    [Serializable]
    public class SaveData
    {
        #region 进度数据

        /// <summary>
        /// 游戏进度信息
        /// </summary>
        public GameProgress gameProgress;

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
        /// 当前存档数据结构版本。结构变更时 +1，并在 JsonSaveSystem.MigrateSaveData 中补充迁移逻辑。
        /// </summary>
        public const int CURRENT_DATA_VERSION = 1;

        /// <summary>
        /// 存档版本号（游戏发布版本）
        /// </summary>
        public string version;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public SaveData()
        {
            gameProgress = new GameProgress();
            gameProgress.EnsureInitialized();
            SetSaveTimeMetadata();
            dataVersion = CURRENT_DATA_VERSION;
            version = "1.0.0"; // 使用默认版本号，避免在序列化期间调用Application.version
        }

        /// <summary>
        /// 构造函数，从游戏进度和设置创建存档数据
        /// </summary>
        /// <param name="progress">游戏进度</param>
        /// <param name="settings">游戏设置</param>
        public SaveData(GameProgress progress, GameSettings settings)
        {
            gameProgress = progress ?? new GameProgress();
            gameProgress.EnsureInitialized();
            UpdateSettings(settings);
            SetSaveTimeMetadata();
            dataVersion = CURRENT_DATA_VERSION;
            version = "1.0.0";
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

        #endregion
    }
}