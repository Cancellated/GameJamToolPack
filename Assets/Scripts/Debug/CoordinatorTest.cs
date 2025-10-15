using UnityEngine;
using Logger;
using MyGame.Core;
using MyGame.Events;

namespace MyGame.DevTools
{
    /// <summary>
    /// 游戏流程协调器测试脚本
    /// 用于验证GameFlowCoordinator的功能是否正常工作
    /// 特别是验证事件是否被正确地触发和管理
    /// </summary>
    public class CoordinatorTest : MonoBehaviour
    {
        private const string LOG_MODULE = "COORDINATOR_TEST";
        
        // 用于跟踪协调器触发的事件次数
        private int coordinatedEventCount = 0;
        
        void Start()
        {
            Log.Info(LOG_MODULE, "协调器测试脚本已启动");
            
            // 注册CreateNewGame事件监听，仅用于验证协调器是否正常工作
            GameEvents.OnCreateNewGame += OnCreateNewGameFromCoordinator;
        }
        
        void OnDestroy()
        {
            // 注销事件监听
            GameEvents.OnCreateNewGame -= OnCreateNewGameFromCoordinator;
        }
        
        /// <summary>
        /// 当通过协调器触发CreateNewGame事件时调用
        /// </summary>
        private void OnCreateNewGameFromCoordinator(string slotName)
        {
            coordinatedEventCount++;
            Log.Info(LOG_MODULE, $"通过协调器触发的CreateNewGame事件已接收: 存档槽={slotName}, 次数={coordinatedEventCount}");
        }
        
        void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.wordWrap = true;
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            
            // 绘制测试信息
            GUI.Box(new Rect(350, 20, 300, 150), "GameFlowCoordinator测试", style);
            
            // 显示协调器状态
            string status = "未初始化";
            if (GameFlowCoordinator.Instance != null)
            {
                status = "正常运行中";
            }
            GUI.Label(new Rect(370, 50, 260, 20), $"协调器状态: {status}", style);
            GUI.Label(new Rect(370, 70, 260, 20), $"协调器触发事件次数: {coordinatedEventCount}", style);
            
            // 测试按钮 - 直接触发CreateNewGame事件
            if (GUI.Button(new Rect(370, 100, 260, 30), "直接触发CreateNewGame (不推荐)", buttonStyle))
            {
                Log.Info(LOG_MODULE, "直接触发CreateNewGame事件");
                GameEvents.TriggerCreateNewGame("TestSlot_Direct");
            }
            
            // 测试按钮 - 通过协调器触发CreateNewGame事件
            if (GUI.Button(new Rect(370, 140, 260, 30), "通过协调器触发CreateNewGame", buttonStyle))
            {
                if (GameFlowCoordinator.Instance != null)
                {
                    Log.Info(LOG_MODULE, "通过GameFlowCoordinator触发CreateNewGame事件");
                    GameFlowCoordinator.Instance.StartNewGame("TestSlot_Coordinated");
                }
                else
                {
                    Log.Error(LOG_MODULE, "GameFlowCoordinator实例不存在，无法触发事件");
                }
            }
        }
    }
}