using UnityEngine;
using Logger;
using MyGame.Core;
using MyGame.Events;

namespace MyGame.DevTools
{
    /// <summary>
    /// 游戏流程测试场景辅助工具
    /// 用于快速测试和验证游戏流程协调器的功能
    /// 提供了一键设置测试环境的功能
    /// </summary>
    public class FlowTestSceneHelper : MonoBehaviour
    {
        private const string LOG_MODULE = "FLOW_TEST_SCENE";
        
        [Header("组件引用")]
        [Tooltip("游戏流程协调器预设")]
        public GameObject gameFlowCoordinatorPrefab;
        
        [Tooltip("游戏流程测试脚本预设")]
        public GameObject gameFlowTestPrefab;
        
        [Tooltip("协调器测试脚本预设")]
        public GameObject coordinatorTestPrefab;
        
        void Start()
        {
            Log.Info(LOG_MODULE, "测试场景辅助工具已启动");
        }
        
        void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.6f, 0.8f));
            
            // 绘制测试场景辅助工具界面
            GUI.Box(new Rect(20, 20, 300, 250), "游戏流程测试场景辅助工具", style);
            
            // 环境设置按钮
            if (GUI.Button(new Rect(40, 50, 260, 30), "一键设置测试环境", buttonStyle))
            {
                SetupTestEnvironment();
            }
            
            // 检查协调器状态按钮
            if (GUI.Button(new Rect(40, 90, 260, 30), "检查游戏流程协调器状态", buttonStyle))
            {
                CheckCoordinatorStatus();
            }
            
            // 触发测试按钮
            if (GUI.Button(new Rect(40, 130, 260, 30), "通过协调器触发新游戏", buttonStyle))
            {
                if (GameFlowCoordinator.Instance != null)
                {
                    GameFlowCoordinator.Instance.StartNewGame("Test_Slot_" + System.DateTime.Now.Ticks);
                }
                else
                {
                    Log.Error(LOG_MODULE, "游戏流程协调器未初始化");
                }
            }
            
            // 直接触发按钮（用于测试对比）
            if (GUI.Button(new Rect(40, 170, 260, 30), "直接触发新游戏（不推荐）", buttonStyle))
            {
                GameEvents.TriggerCreateNewGame("Direct_Test_Slot_" + System.DateTime.Now.Ticks);
            }
            
            // 重置计数器按钮
            if (GUI.Button(new Rect(40, 210, 260, 30), "重置所有测试计数器", buttonStyle))
            {
                ResetTestCounters();
            }
        }
        
        /// <summary>
        /// 一键设置测试环境
        /// 自动创建所需的测试组件
        /// </summary>
        private void SetupTestEnvironment()
        {
            Log.Info(LOG_MODULE, "开始设置测试环境");
            
            // 创建游戏流程协调器（如果不存在）
            if (GameFlowCoordinator.Instance == null && gameFlowCoordinatorPrefab != null)
            {
                Instantiate(gameFlowCoordinatorPrefab);
                Log.Info(LOG_MODULE, "游戏流程协调器已创建");
            }
            
            // 创建游戏流程测试脚本（如果不存在）
            if (FindObjectOfType<GameFlowTest>() == null && gameFlowTestPrefab != null)
            {
                Instantiate(gameFlowTestPrefab);
                Log.Info(LOG_MODULE, "游戏流程测试脚本已创建");
            }
            
            // 创建协调器测试脚本（如果不存在）
            if (FindObjectOfType<CoordinatorTest>() == null && coordinatorTestPrefab != null)
            {
                Instantiate(coordinatorTestPrefab);
                Log.Info(LOG_MODULE, "协调器测试脚本已创建");
            }
            
            Log.Info(LOG_MODULE, "测试环境设置完成");
        }
        
        /// <summary>
        /// 检查游戏流程协调器状态
        /// </summary>
        private void CheckCoordinatorStatus()
        {
            if (GameFlowCoordinator.Instance != null)
            {
                Log.Info(LOG_MODULE, "游戏流程协调器状态: 正常运行中");
            }
            else
            {
                Log.Warning(LOG_MODULE, "游戏流程协调器状态: 未初始化");
            }
        }
        
        /// <summary>
        /// 重置所有测试计数器
        /// </summary>
        private void ResetTestCounters()
        {
            GameFlowTest flowTest = FindObjectOfType<GameFlowTest>();
            if (flowTest != null)
            {
                flowTest.ResetCounters();
                Log.Info(LOG_MODULE, "游戏流程测试计数器已重置");
            }
        }
        
        /// <summary>
        /// 创建简单的纹理用于GUI按钮
        /// </summary>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}