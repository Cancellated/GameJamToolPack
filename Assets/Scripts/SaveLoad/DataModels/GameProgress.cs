using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Data
{
    /// <summary>
    /// 游戏进度数据结构，存储玩家的游戏进度信息
    /// </summary>
    [Serializable]
    public class GameProgress
    {
        public Vector3 playerPosition;
        public int currentLevel;
        public List<QuestStatus> activeQuests;
        public List<LevelStatus> completedLevels;

        /// <summary>
        /// 玩家统计数据（如生命、体力等）。
        /// 注意：不能使用 Dictionary——Unity 的 JsonUtility 不支持 Dictionary 序列化，
        /// 字典字段会被静默序列化为空对象导致存档数据丢失，因此使用可序列化的列表结构。
        /// </summary>
        public List<StatEntry> playerStats;

        /// <summary>
        /// 构造函数
        /// </summary>
        public GameProgress()
        {
            playerPosition = Vector3.zero;
            currentLevel = 1;
            activeQuests = new List<QuestStatus>();
            completedLevels = new List<LevelStatus>();
            playerStats = new List<StatEntry>();
        }

        /// <summary>
        /// 更新玩家位置
        /// </summary>
        /// <param name="position">新的玩家位置</param>
        public void UpdatePlayerPosition(Vector3 position)
        {
            playerPosition = position;
        }

        /// <summary>
        /// 确保所有集合字段非空。
        /// JsonUtility 反序列化旧存档时，缺失的列表字段可能为 null，
        /// 统一在此补齐，避免外部遍历时出现 NullReferenceException。
        /// </summary>
        public void EnsureInitialized()
        {
            activeQuests ??= new List<QuestStatus>();
            completedLevels ??= new List<LevelStatus>();
            playerStats ??= new List<StatEntry>();
        }

        /// <summary>
        /// 更新当前关卡
        /// </summary>
        /// <param name="level">新的关卡编号</param>
        public void UpdateCurrentLevel(int level)
        {
            currentLevel = level;
        }

        /// <summary>
        /// 添加或更新任务进度
        /// </summary>
        /// <param name="questId">任务ID</param>
        /// <param name="step">任务进度步骤</param>
        public void UpdateQuestProgress(string questId, int step)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return;
            }

            EnsureInitialized();

            // 按索引查找并移除旧条目（不使用 Find + 默认值判断：
            // struct 默认值的 questId 为 null，作为"未找到"标志不可靠）
            for (int i = 0; i < activeQuests.Count; i++)
            {
                if (activeQuests[i].questId == questId)
                {
                    activeQuests.RemoveAt(i);
                    break;
                }
            }
            activeQuests.Add(new QuestStatus { questId = questId, progressStep = step });
        }

        /// <summary>
        /// 标记关卡为已完成
        /// </summary>
        /// <param name="levelId">关卡ID</param>
        public void MarkLevelAsCompleted(int levelId)
        {
            EnsureInitialized();

            // 按索引查找并移除旧条目（原实现用 levelStatus.levelId != 0 判断"未找到"，
            // levelId 为 0 时会把已有条目误判为不存在，产生重复数据）
            for (int i = 0; i < completedLevels.Count; i++)
            {
                if (completedLevels[i].levelId == levelId)
                {
                    completedLevels.RemoveAt(i);
                    break;
                }
            }
            completedLevels.Add(new LevelStatus { levelId = levelId, isCompleted = true });
        }

        /// <summary>
        /// 设置玩家统计数据
        /// </summary>
        /// <param name="statName">统计数据名称</param>
        /// <param name="value">统计数据值</param>
        public void SetPlayerStat(string statName, int value)
        {
            EnsureInitialized();

            for (int i = 0; i < playerStats.Count; i++)
            {
                if (playerStats[i].name == statName)
                {
                    playerStats[i] = new StatEntry { name = statName, value = value };
                    return;
                }
            }
            playerStats.Add(new StatEntry { name = statName, value = value });
        }

        /// <summary>
        /// 获取玩家统计数据
        /// </summary>
        /// <param name="statName">统计数据名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>统计数据值</returns>
        public int GetPlayerStat(string statName, int defaultValue = 0)
        {
            EnsureInitialized();

            for (int i = 0; i < playerStats.Count; i++)
            {
                if (playerStats[i].name == statName)
                {
                    return playerStats[i].value;
                }
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// 任务状态结构，存储任务的ID和进度步骤
    /// </summary>
    [Serializable]
    public struct QuestStatus
    {
        public string questId;
        public int progressStep;
    }

    /// <summary>
    /// 关卡状态结构，存储关卡的ID和完成状态
    /// </summary>
    [Serializable]
    public struct LevelStatus
    {
        public int levelId;
        public bool isCompleted;
    }

    /// <summary>
    /// 玩家统计条目（可序列化），替代不受 JsonUtility 支持的 Dictionary
    /// </summary>
    [Serializable]
    public struct StatEntry
    {
        public string name;
        public int value;
    }
}