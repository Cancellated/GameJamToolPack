using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Data
{
    [Serializable]
    public class GameProgress
    {
        public Vector3 playerPosition;
        public int currentLevel;
        public List<QuestStatus> activeQuests;
    }

    [Serializable]
    // 任务状态
    public struct QuestStatus
    {
        public string questId;
        public int progressStep;
    }

    [Serializable]
    // 关卡状态
    public struct LevelStatus
    {
        public int levelId;
        public bool isCompleted;
    }
}
