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
            Log.Info(module, $"开始加载场景: {sceneName}");
            OnSceneLoadStart?.Invoke(sceneName);
        }
        
        /// <summary>
        /// 场景加载完成事件
        /// </summary>
        public static event Action<string> OnSceneLoadComplete;
        
        public static void TriggerSceneLoadComplete(string sceneName)
        {
            Log.Info(module, $"场景加载完成: {sceneName}");
            OnSceneLoadComplete?.Invoke(sceneName);
        }
        
        /// <summary>
        /// 场景卸载事件
        /// </summary>
        public static event Action<string> OnSceneUnload;
        
        public static void TriggerSceneUnload(string sceneName)
        {
            Log.Info(module, $"卸载场景: {sceneName}");
            OnSceneUnload?.Invoke(sceneName);
        }

        #endregion

        #region UI事件

        /// <summary>
        /// 显示或隐藏主菜单
        /// </summary>
        public static event Action<bool> OnMainMenuShow;
        
        public static void TriggerMainMenuShow(bool show)
        {
            Log.Info(module, $"主菜单显示：{show}");
            OnMainMenuShow?.Invoke(show);
        }

        /// <summary>
        /// 显示或隐藏暂停菜单
        /// </summary>
        public static event Action<bool> OnPauseMenuShow;
        
        public static void TriggerPauseMenuShow(bool show)
        {
            Log.Info(module, $"暂停菜单显示：{show}");
            OnPauseMenuShow?.Invoke(show);
        }

        /// <summary>
        /// 显示或隐藏HUD
        /// </summary>
        public static event Action<bool> OnHUDShow;
        
        public static void TriggerHUDShow(bool show)
        {
            Log.Info(module, $"HUD显示：{show}");
            OnHUDShow?.Invoke(show);
        }
        
        /// <summary>
        /// 显示或隐藏控制台
        /// </summary>
        public static event Action<bool> OnConsoleShow;
        
        public static void TriggerConsoleShow(bool show)
        {
            Log.Info(module, $"控制台显示：{show}");
            OnConsoleShow?.Invoke(show);
        }
        
        /// <summary>
        /// 显示或隐藏背包
        /// </summary>
        public static event Action<bool> OnInventoryShow;
        
        public static void TriggerInventoryShow(bool show)
        {
            Log.Info(module, $"背包显示：{show}");
            OnInventoryShow?.Invoke(show);
        }

        /// <summary>
        /// 显示或隐藏设置面板
        /// </summary>
        public static event Action<bool> OnSettingsPanelShow;
        
        public static void TriggerSettingsPanelShow(bool show)
        {
            Log.Info(module, $"设置面板显示：{show}");
            OnSettingsPanelShow?.Invoke(show);
        }

        /// <summary>
        /// 显示或隐藏关于面板
        /// </summary>
        public static event Action<bool> OnAboutPanelShow;
        
        public static void TriggerAboutPanelShow(bool show)
        {
            Log.Info(module, $"关于面板显示：{show}");
            OnAboutPanelShow?.Invoke(show);
        }

            #region UI切换事件

        /// <summary>
        /// UI状态切换事件（互斥显示）
        /// </summary>
        public static event Action<UIType, bool> OnMenuShow;
        
        public static void TriggerMenuShow(UIType state, bool show)
        {
            Log.Info(module, $"菜单切换：{state} 显示：{show}");
            OnMenuShow?.Invoke(state, show);
        }

            #endregion
        #endregion
    }
}