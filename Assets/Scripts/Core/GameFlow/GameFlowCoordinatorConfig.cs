using UnityEngine;

namespace MyGame.Core
{
    /// <summary>
    /// 游戏流程协调器配置类
    /// 包含GameFlowCoordinator的各项配置参数
    /// 可在Unity编辑器中直接修改这些参数
    /// </summary>
    [CreateAssetMenu(fileName = "GameFlowCoordinatorConfig", menuName = "Core/GameFlowCoordinator Config", order = 0)]
    public class GameFlowCoordinatorConfig : ScriptableObject
    {
        /// <summary>
        /// 调试模式开关
        /// 开启后会输出更详细的日志信息
        /// </summary>
        [Header("调试设置")]
        [Tooltip("是否开启调试模式，输出更详细的日志信息")]
        public bool debugMode = false;
        
        /// <summary>
        /// 场景切换时是否允许触发游戏流程事件
        /// 通常设置为false以避免场景加载过程中的问题
        /// </summary>
        [Header("场景设置")]
        [Tooltip("场景切换期间是否允许触发游戏流程事件")]
        public bool allowEventsDuringSceneTransition = false;
        
        /// <summary>
        /// 加载游戏时的最大重试次数
        /// 防止加载失败时的无限重试
        /// </summary>
        [Header("加载设置")]
        [Tooltip("加载游戏时的最大重试次数")]
        [Range(1, 5)]
        public int maxLoadRetries = 3;
    }
}