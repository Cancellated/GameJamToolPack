using MyGame.Managers; // 引入游戏状态枚举
using System;
using UnityEngine;
using Logger;
using MyGame.UI;

namespace MyGame.Events
{

    /// <summary>
    /// 全局静态事件系统，支持类型安全的事件注册与触发。
    /// 用于模块间解耦通信。
    /// </summary>
    public static class GameEvents
    {
        const string module = LogModules.GAMEEVENTS;
        #region 游戏流程事件

        /// <summary>
        /// 游戏开始事件。
        /// </summary>
        public static event Action OnGameStart;
        
        public static void TriggerGameStart()
        {
            Log.Info(module, "触发游戏开始事件");
            OnGameStart?.Invoke();
        }

        /// <summary>
        /// 游戏暂停事件。
        /// </summary>
        public static event Action OnGamePause;
        
        public static void TriggerGamePause()
        {
            Log.Info(module, "触发游戏暂停事件");
            OnGamePause?.Invoke();
        }

        /// <summary>
        /// 游戏继续事件。
        /// </summary>
        public static event Action OnGameResume;
        
        public static void TriggerGameResume()
        {
            Log.Info(module, "触发游戏继续事件");
            OnGameResume?.Invoke();
        }

        /// <summary>
        /// 游戏结束事件，参数为true表示胜利，false表示失败。
        /// </summary>
        public static event Action<bool> OnGameOver;
        
        public static void TriggerGameOver(bool isWin)
        {
            Log.Info(module, $"触发游戏结束事件，胜利：{isWin}");
            OnGameOver?.Invoke(isWin);
        }

        /// <summary>
        /// 游戏重新开始事件（结算面板的重新开始按钮触发）。
        /// 订阅方负责复位本局状态（分数/时间流速/玩家/敌人生成器等）
        /// </summary>
        public static event Action OnGameRestart;
        
        public static void TriggerGameRestart()
        {
            Log.Info(module, "触发游戏重新开始事件");
            OnGameRestart?.Invoke();
        }

        /// <summary>
        /// 游戏状态变更事件。
        /// </summary>
        public static event Action<GameState, GameState> OnGameStateChanged;
        
        public static void TriggerGameStateChanged(GameState from, GameState to)
        {
            Log.Info(module, $"游戏状态变更：{from} -> {to}");
            OnGameStateChanged?.Invoke(from, to);
        }

        #endregion

        #region 场景管理事件

        /// <summary>
        /// 场景加载开始事件
        /// </summary>
        public static event Action<string> OnSceneLoadStart;
        
        public static void TriggerSceneLoadStart(string sceneName)
        {
            Log.Info(module, $"触发开始加载场景事件: {sceneName}");
            OnSceneLoadStart?.Invoke(sceneName);
        }
        
        /// <summary>
        /// 场景加载完成事件
        /// </summary>
        public static event Action<string> OnSceneLoadComplete;
        
        public static void TriggerSceneLoadComplete(string sceneName)
        {
            Log.Info(module, $"触发场景加载完成事件: {sceneName}");
            OnSceneLoadComplete?.Invoke(sceneName);
        }
        
        /// <summary>
        /// 加载界面准备就绪事件
        /// 当加载界面完全显示后触发，用于开始实际的场景加载
        /// </summary>
        public static event Action<string> OnLoadingScreenReady;
        
        public static void TriggerLoadingScreenReady(string sceneName)
        {
            Log.Info(module, $"触发加载界面准备就绪事件: {sceneName}");
            OnLoadingScreenReady?.Invoke(sceneName);
        }
        
        /// <summary>
        /// 场景卸载事件
        /// </summary>
        public static event Action<string> OnSceneUnload;
        
        public static void TriggerSceneUnload(string sceneName)
        {
            Log.Info(module, $"触发场景卸载事件: {sceneName}");
            OnSceneUnload?.Invoke(sceneName);
        }

        #endregion

        #region 存档相关事件

        /// <summary>
        /// 新游戏创建事件
        /// </summary>
        public static event Action OnCreateNewGame;
        
        public static void TriggerCreateNewGame()
        {
            Log.Info(module, "触发新游戏创建事件");
            OnCreateNewGame?.Invoke();
        }

        /// <summary>
        /// 游戏数据保存完成事件
        /// </summary>
        public static event Action<string> OnSaveGame;
        
        public static void TriggerSaveGame(string slotName)
        {
            Log.Info(module, $"触发游戏数据保存完成事件: {slotName}");
            OnSaveGame?.Invoke(slotName);
        }

        /// <summary>
        /// 自动保存事件
        /// </summary>
        public static event Action<string> OnAutoSave;
        
        /// <summary>
        /// 触发自动保存事件。
        /// 槽位名称由调用方显式指定（如 SaveManager.DEFAULT_SAVE_SLOT），
        /// 不再在事件层内置默认值，避免与存档管理器/菜单的槽名约定分叉。
        /// </summary>
        public static void TriggerAutoSave(string slotName)
        {
            Log.Info(module, $"触发自动保存事件: {slotName}");
            OnAutoSave?.Invoke(slotName);
        }

        /// <summary>
        /// 游戏数据加载完成事件
        /// </summary>
        public static event Action<string> OnLoadGame;
        
        public static void TriggerLoadGame(string slotName)
        {
            Log.Info(module, $"触发游戏数据加载完成事件: {slotName}");
            OnLoadGame?.Invoke(slotName);
        }

        /// <summary>
        /// 玩法数据变更事件（无参数）。
        /// 玩法系统写入新状态后触发，可用于"存在未保存进度"标记、手动自动存档等场景。
        /// 事件不携带任何玩法字段：具体数据始终由玩法系统通过 IGameplaySaveProvider 自己持有。
        /// </summary>
        public static event Action OnGameplayDataChanged;

        public static void TriggerGameplayDataChanged()
        {
            Log.Info(module, "触发玩法数据变更事件");
            OnGameplayDataChanged?.Invoke();
        }

        /// <summary>
        /// 玩法存档数据已加载事件。
        /// 读取存档成功后触发，参数为玩法自定义 JSON（无玩法数据时为空字符串），
        /// 由关卡/玩家等系统自行反序列化并恢复游戏世界。
        /// </summary>
        public static event Action<string> OnGameplayDataLoaded;

        public static void TriggerGameplayDataLoaded(string gameplayDataJson)
        {
            if (gameplayDataJson == null)
            {
                Log.Warning(module, "触发玩法数据加载事件失败：数据为空");
                return;
            }

            Log.Info(module, "触发玩法数据加载事件");
            OnGameplayDataLoaded?.Invoke(gameplayDataJson);
        }

        /// <summary>
        /// 游戏数据删除事件
        /// </summary>
        public static event Action<string> OnDeleteSave;
        
        public static void TriggerDeleteSave(string slotName)
        {
            Log.Info(module, $"触发游戏数据删除事件: {slotName}");
            OnDeleteSave?.Invoke(slotName);
        }

        #endregion

        #region 设置相关事件

        /// <summary>
        /// 设置应用完成事件
        /// 当游戏设置被应用到游戏时触发
        /// </summary>
        public static event Action OnSettingsApplied;
        
        public static void TriggerSettingsApplied()
        {
            Log.Info(module, "触发设置应用完成事件");
            OnSettingsApplied?.Invoke();
        }
        
        /// <summary>
        /// 设置保存完成事件
        /// 当游戏设置被保存时触发
        /// </summary>
        public static event Action OnSettingsSaved;
        
        public static void TriggerSettingsSaved()
        {
            Log.Info(module, "触发设置保存完成事件");
            OnSettingsSaved?.Invoke();
        }
        
        #endregion

        #region UI事件

        /// <summary>
        /// UI状态切换事件（统一管理所有UI的显示/隐藏）
        /// </summary>
        public static event Action<UIType, bool> OnMenuShow;
        
        public static void TriggerMenuShow(UIType menu, bool show)
        {
            Log.Info(module, $"UI切换：{menu} 显示：{show}");
            OnMenuShow?.Invoke(menu, show);
        }

        #endregion
    }
}