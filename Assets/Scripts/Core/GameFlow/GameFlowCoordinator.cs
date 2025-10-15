using UnityEngine;
using Logger;
using MyGame.Events;
using System.Collections.Generic;

namespace MyGame.Core
{
    /// <summary>
    /// 游戏流程协调器
    /// 负责统一管理游戏的主要流程，如开始新游戏、加载游戏等
    /// 确保游戏流程的一致性和避免重复触发
    /// </summary>
    public class GameFlowCoordinator : Singleton<GameFlowCoordinator>
    {

        
        #region 常量
    
        private const string LOG_MODULE = "GAME_FLOW";
        
        #endregion
        
        #region 字段
        
        // 配置文件引用
        [Header("配置")]
        [Tooltip("游戏流程协调器配置文件")]
        [SerializeField] private GameFlowCoordinatorConfig config;
        
        // 场景加载状态标记
        private bool isSceneLoading = false;
        
        // 事件源追踪
        private Dictionary<string, int> eventSourceCounts = new();
        
        #endregion
        
        #region 生命周期
        
        /// <summary>
        /// 初始化组件和加载配置
        /// </summary>
        protected override void Awake()
        {
            base.Awake(); // 调用基类的Awake方法处理单例逻辑
            
            // 尝试加载默认配置文件
            if (config == null)
            {
                config = Resources.Load<GameFlowCoordinatorConfig>("GameFlowCoordinatorConfig");
                if (config == null)
                {
                    Log.Warning(LOG_MODULE, "未找到配置文件，将使用默认值");
                    // 创建临时配置对象作为回退
                    config = ScriptableObject.CreateInstance<GameFlowCoordinatorConfig>();
                }
            }
            
            Log.Info(LOG_MODULE, "游戏流程协调器初始化成功");
            DebugLog("配置参数: 调试模式={0}", config.debugMode);
        }
        
        /// <summary>
        /// 当对象启用时，注册事件监听
        /// </summary>
        private void OnEnable()
        {
            RegisterEvents();
        }
        
        /// <summary>
        /// 当对象禁用时，注销事件监听
        /// </summary>
        private void OnDisable()
        {
            UnregisterEvents();
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 统一的开始新游戏方法
        /// 这是推荐的触发新游戏流程的入口点
        /// </summary>
        /// <param name="slotName">存档槽名称，默认为"AutoSave"</param>
        /// <param name="source">事件触发源，用于追踪</param>
        public void StartNewGame(string slotName = "AutoSave", string source = "Unknown")
        {
            // 检查场景加载状态
            if (isSceneLoading && !config.allowEventsDuringSceneTransition)
            {
                Log.Warning(LOG_MODULE, "场景加载中，不允许触发新游戏流程");
                return;
            }
            
            // 记录事件源，用于追踪和调试
            TrackEventSource(source);
            
            Log.Info(LOG_MODULE, $"开始新游戏流程，存档槽: {slotName}，触发源: {source}");
            DebugLog("新游戏流程参数: 存档槽={0}, 触发源={1}", slotName, source);
            
            // 触发创建新游戏事件
            // 注意：游戏开始和场景加载会在新游戏创建过程中自动触发
            GameEvents.TriggerCreateNewGame(slotName);
        }
        
        /// <summary>
        /// 统一的加载游戏方法
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="source">事件触发源，用于追踪</param>
        public void LoadGame(string slotName, string source = "Unknown")
        {
            // 检查场景加载状态
            if (isSceneLoading && !config.allowEventsDuringSceneTransition)
            {
                Log.Warning(LOG_MODULE, "场景加载中，不允许触发加载游戏流程");
                return;
            }
            
            // 记录事件源，用于追踪和调试
            TrackEventSource(source);
            
            Log.Info(LOG_MODULE, $"开始加载游戏流程，存档槽: {slotName}，触发源: {source}");
            DebugLog("加载游戏流程参数: 存档槽={0}, 触发源={1}", slotName, source);
            
            GameEvents.TriggerLoadGame(slotName);
        }
        
        /// <summary>
        /// 统一的保存游戏方法
        /// </summary>
        /// <param name="slotName">存档槽名称</param>
        /// <param name="source">事件触发源，用于追踪</param>
        public void SaveGame(string slotName, string source = "Unknown")
        {
            // 检查场景加载状态
            if (isSceneLoading && !config.allowEventsDuringSceneTransition)
            {
                Log.Warning(LOG_MODULE, "场景加载中，不允许触发保存游戏流程");
                return;
            }
            
            // 记录事件源，用于追踪和调试
            TrackEventSource(source);
            
            Log.Info(LOG_MODULE, $"开始保存游戏流程，存档槽: {slotName}，触发源: {source}");
            DebugLog("保存游戏流程参数: 存档槽={0}, 触发源={1}", slotName, source);
            
            GameEvents.TriggerSaveGame(slotName);
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 注册游戏事件监听
        /// </summary>
        private void RegisterEvents()
        {
            // 注册场景加载事件
            GameEvents.OnSceneLoadStart += OnSceneLoadStart;
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
        }
        
        /// <summary>
        /// 注销游戏事件监听
        /// </summary>
        private void UnregisterEvents()
        {
            // 注销场景加载事件
            GameEvents.OnSceneLoadStart -= OnSceneLoadStart;
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;
        }
        
        #endregion
        
        #region 私有辅助方法
        
        /// <summary>
        /// 场景加载开始时的处理方法
        /// </summary>
        private void OnSceneLoadStart(string sceneName)
        {
            isSceneLoading = true;
            DebugLog("场景加载开始: {0}", sceneName);
        }
        
        /// <summary>
        /// 场景加载完成时的处理方法
        /// </summary>
        private void OnSceneLoadComplete(string sceneName)
        {
            isSceneLoading = false;
            DebugLog("场景加载完成: {0}", sceneName);
        }
        
        /// <summary>
        /// 调试日志输出，仅在调试模式下输出
        /// </summary>
        private void DebugLog(string format, params object[] args)
        {
            if (config != null && config.debugMode)
            {
                Log.DebugLog(LOG_MODULE, string.Format(format, args));
            }
        }
        
        /// <summary>
        /// 追踪事件触发源
        /// 用于调试和识别重复触发问题
        /// </summary>
        /// <param name="source">事件触发源</param>
        private void TrackEventSource(string source)
        {
            if (eventSourceCounts.ContainsKey(source))
            {
                eventSourceCounts[source]++;
            }
            else
            {
                eventSourceCounts[source] = 1;
            }
            
            // 在调试模式下记录每个来源的触发次数
            if (config != null && config.debugMode)
            {
                DebugLog("事件触发源统计: {0} - 次数: {1}", source, eventSourceCounts[source]);
            }
        }
        
        #endregion
    }
}