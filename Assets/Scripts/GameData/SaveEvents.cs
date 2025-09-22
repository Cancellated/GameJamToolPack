using System;
using Logger;
using MyGame.Events;
using MyGame.Managers;

namespace MyGame.Data
{
    /// <summary>
    /// 存档系统专用事件系统，用于处理存档相关操作的通知。
    /// 支持新游戏创建、存档保存、加载和删除等事件的注册与触发。
    /// </summary>
    public static class SaveEvents
    {
        private const string LOG_MODULE = "SaveEvents";
        
        #region 存档相关事件
        
        /// <summary>
        /// 新游戏创建事件。
        /// 当玩家开始新游戏时触发。
        /// </summary>
        public static event Action OnNewGame;
        
        /// <summary>
        /// 触发新游戏创建事件。
        /// </summary>
        public static void TriggerNewGame()
        {
            Log.Info(LOG_MODULE, "触发新游戏创建事件");
            OnNewGame?.Invoke();
            
            // 将存档事件转发到全局事件系统
            GameEvents.TriggerNewGameCreated();
        }
        
        /// <summary>
        /// 游戏数据保存完成事件。
        /// 当游戏数据成功保存到文件后触发。
        /// </summary>
        public static event Action<string> OnSaveComplete;
        
        /// <summary>
        /// 触发游戏数据保存完成事件。
        /// </summary>
        /// <param name="slotName">保存的存档槽名称。</param>
        public static void TriggerSaveComplete(string slotName)
        {
            Log.Info(LOG_MODULE, $"触发游戏数据保存完成事件: {slotName}");
            OnSaveComplete?.Invoke(slotName);
            
            // 将存档事件转发到全局事件系统
            GameEvents.TriggerGameSaved(slotName);
        }
        
        /// <summary>
        /// 游戏数据加载完成事件。
        /// 当游戏数据成功从文件加载后触发。
        /// </summary>
        public static event Action<string> OnLoadComplete;
        
        /// <summary>
        /// 触发游戏数据加载完成事件。
        /// </summary>
        /// <param name="slotName">加载的存档槽名称。</param>
        public static void TriggerLoadComplete(string slotName)
        {
            Log.Info(LOG_MODULE, $"触发游戏数据加载完成事件: {slotName}");
            OnLoadComplete?.Invoke(slotName);
            
            // 将存档事件转发到全局事件系统
            GameEvents.TriggerGameLoaded(slotName);
        }
        
        /// <summary>
        /// 游戏数据删除完成事件。
        /// 当游戏数据成功从文件删除后触发。
        /// </summary>
        public static event Action<string> OnDeleteComplete;
        
        /// <summary>
        /// 触发游戏数据删除完成事件。
        /// </summary>
        /// <param name="slotName">删除的存档槽名称。</param>
        public static void TriggerDeleteComplete(string slotName)
        {
            Log.Info(LOG_MODULE, $"触发游戏数据删除完成事件: {slotName}");
            OnDeleteComplete?.Invoke(slotName);
            
            // 将存档事件转发到全局事件系统
            GameEvents.TriggerGameDeleted(slotName);
        }
        
        /// <summary>
        /// 存档操作失败事件。
        /// 当存档、加载或删除操作失败时触发。
        /// </summary>
        public static event Action<string, string> OnOperationFailed;
        
        /// <summary>
        /// 触发存档操作失败事件。
        /// </summary>
        /// <param name="operationType">操作类型（Save/Load/Delete）。</param>
        /// <param name="errorMessage">错误信息。</param>
        public static void TriggerOperationFailed(string operationType, string errorMessage)
        {
            Log.Error(LOG_MODULE, $"触发{operationType}操作失败事件: {errorMessage}");
            OnOperationFailed?.Invoke(operationType, errorMessage);
        }
        
        /// <summary>
        /// 自动保存触发事件。
        /// 当系统触发自动保存操作时触发。
        /// </summary>
        public static event Action OnAutoSaveTriggered;
        
        /// <summary>
        /// 触发自动保存事件。
        /// </summary>
        public static void TriggerAutoSaveTriggered()
        {
            Log.Info(LOG_MODULE, "触发自动保存事件");
            OnAutoSaveTriggered?.Invoke();
        }
        
        #endregion
    }
}