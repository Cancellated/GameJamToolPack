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

        /// <summary>
        /// 音乐音量
        /// </summary>
        public float musicVolume;

        /// <summary>
        /// 音效音量
        /// </summary>
        public float sfxVolume;

        /// <summary>
        /// 画质等级
        /// </summary>
        public int qualityLevel;

        /// <summary>
        /// 是否全屏
        /// </summary>
        public bool fullscreen;

        /// <summary>
        /// 分辨率索引
        /// </summary>
        public int resolutionIndex;

        /// <summary>
        /// 是否反转Y轴
        /// </summary>
        public bool invertYAxis;

        #endregion

        #region 元数据

        /// <summary>
        /// 存档时间戳
        /// </summary>
        public string saveTime;

        /// <summary>
        /// 存档版本号
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
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            version = Application.version;
        }

        /// <summary>
        /// 构造函数，从游戏进度和设置创建存档数据
        /// </summary>
        /// <param name="progress">游戏进度</param>
        /// <param name="settings">游戏设置</param>
        public SaveData(GameProgress progress, GameSettings settings)
        {
            gameProgress = progress ?? new GameProgress();
            UpdateSettings(settings);
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            version = Application.version;
        }

        #endregion

        #region 公共方法

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
