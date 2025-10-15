using MyGame.Events;
using MyGame.Managers;
using UnityEngine;
using Logger;
using System.Text;
using MyGame.Core;

namespace MyGame.DevTools
{
    /// <summary>
    /// 游戏流程测试脚本，用于验证游戏创建和开始流程是否正常工作
    /// 特别是验证是否存在重复执行的问题，并检测GameFlowCoordinator的工作状态
    /// </summary>
    public class GameFlowTest : MonoBehaviour
    {
        private const string LOG_MODULE = "GAME_FLOW_TEST";
        
        // 用于跟踪事件触发次数
        private int gameStartEventCount = 0;
        private int createNewGameEventCount = 0;
        private int sceneLoadRequestCount = 0;
        
        // 用于记录事件调用栈
        private StringBuilder createNewGameCallStack = new StringBuilder();
        
        // 记录GameFlowCoordinator的状态
        private bool isCoordinatorActive = false;
        private string coordinatorStatus = "未初始化";

        private void Awake()
        {
            // 检查是否存在多个GameFlowTest实例
            int instanceCount = FindObjectsOfType<GameFlowTest>().Length;
            if (instanceCount > 1)
            {
                Log.Warning(LOG_MODULE, $"警告: 场景中存在多个GameFlowTest实例 ({instanceCount}个)，这可能导致计数不准确");
            }
            else
            {
                Log.Info(LOG_MODULE, "GameFlowTest实例初始化成功");
            }
        }

        private void OnEnable()
        {
            // 注册事件监听以跟踪触发次数
            Log.Info(LOG_MODULE, "注册游戏流程事件监听");
            GameEvents.OnGameStart += OnGameStart;
            GameEvents.OnCreateNewGame += OnCreateNewGame;
            GameEvents.OnSceneLoadStart += OnSceneLoadStart;
        }

        private void OnDisable()
        {
            // 注销事件监听
            Log.Info(LOG_MODULE, "注销游戏流程事件监听");
            GameEvents.OnGameStart -= OnGameStart;
            GameEvents.OnCreateNewGame -= OnCreateNewGame;
            GameEvents.OnSceneLoadStart -= OnSceneLoadStart;
        }

        private void Start()
        {
            Log.Info(LOG_MODULE, "游戏流程测试开始");
            Log.Info(LOG_MODULE, "请通过主菜单或存档菜单测试创建新游戏功能");
        }

        private void Update()
        {
            // 按R键重置计数器
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCounters();
            }
        }

        private void OnGameStart()
        {
            gameStartEventCount++;
            Log.Info(LOG_MODULE, $"OnGameStart事件触发次数: {gameStartEventCount}");
            
            // 如果事件触发次数超过1，可能存在重复执行问题
            if (gameStartEventCount > 1)
            {
                Log.Warning(LOG_MODULE, "警告: GameStart事件被多次触发，可能存在重复执行问题");
            }
        }

        private void OnCreateNewGame(string slotName)
        {
            createNewGameEventCount++;
            string callStack = System.Environment.StackTrace;
            
            // 获取当前时间戳
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            
            // 检查是否通过GameFlowCoordinator触发
            bool isFromCoordinator = callStack.Contains("GameFlowCoordinator");
            string sourceInfo = isFromCoordinator ? "[GameFlowCoordinator触发]" : "[直接触发]";
            
            // 记录调用栈信息，但限制长度以避免日志过大
            string truncatedCallStack = callStack.Substring(0, Mathf.Min(callStack.Length, 800));
            
            createNewGameCallStack.AppendLine($"[{createNewGameEventCount}] 触发时间: {timestamp}");
            createNewGameCallStack.AppendLine($"  存档槽: {slotName}");
            createNewGameCallStack.AppendLine($"  来源: {sourceInfo}");
            createNewGameCallStack.AppendLine($"  调用栈:");
            createNewGameCallStack.AppendLine(truncatedCallStack);
            createNewGameCallStack.AppendLine("------------------------");
            
            Log.Info(LOG_MODULE, $"OnCreateNewGame事件触发次数: {createNewGameEventCount} (存档槽: {slotName}) {sourceInfo}");
            
            // 如果事件触发次数超过1，记录警告信息
            if (createNewGameEventCount > 1)
            {
                Log.Warning(LOG_MODULE, $"警告: CreateNewGame事件被多次触发 ({createNewGameEventCount}次)，可能存在重复执行问题");
            }
        }

        private void OnSceneLoadStart(string sceneName)
        {
            sceneLoadRequestCount++;
            Log.Info(LOG_MODULE, $"场景加载请求次数: {sceneLoadRequestCount} (场景: {sceneName})");
        }

        public void ResetCounters()
        {
            gameStartEventCount = 0;
            createNewGameEventCount = 0;
            sceneLoadRequestCount = 0;
            Log.Info(LOG_MODULE, "所有计数器已重置");
        }

        private bool showCallStackDetails = false;
        private Vector2 scrollPosition = Vector2.zero;

        private void OnGUI()
        {
            // 检查GameFlowCoordinator状态
            CheckCoordinatorStatus();
            
            // 创建样式
            GUIStyle normalStyle = new GUIStyle(GUI.skin.label);
            GUIStyle coordinatorActiveStyle = new GUIStyle(GUI.skin.label);
            coordinatorActiveStyle.normal.textColor = Color.green;
            GUIStyle coordinatorInactiveStyle = new GUIStyle(GUI.skin.label);
            coordinatorInactiveStyle.normal.textColor = Color.red;
            
            // 简单的GUI显示当前计数
            GUI.Box(new Rect(10, 10, 400, 150), "游戏流程测试信息");
            GUI.Label(new Rect(20, 30, 380, 20), $"GameStart事件触发次数: {gameStartEventCount}", normalStyle);
            GUI.Label(new Rect(20, 50, 380, 20), $"CreateNewGame事件触发次数: {createNewGameEventCount}", normalStyle);
            GUI.Label(new Rect(20, 70, 380, 20), $"场景加载请求次数: {sceneLoadRequestCount}", normalStyle);
            GUI.Label(new Rect(20, 90, 380, 20), "按R键重置计数器", normalStyle);
            GUI.Label(new Rect(20, 110, 380, 20), 
                $"GameFlowCoordinator状态: {coordinatorStatus}", 
                isCoordinatorActive ? coordinatorActiveStyle : coordinatorInactiveStyle);
            
            // 显示/隐藏调用栈详情按钮
            if (GUI.Button(new Rect(10, 170, 300, 30), showCallStackDetails ? "隐藏CreateNewGame调用栈详情" : "显示CreateNewGame调用栈详情"))
            {
                showCallStackDetails = !showCallStackDetails;
            }
            
            // 导出调用栈信息按钮
            if (GUI.Button(new Rect(10, 210, 300, 30), "导出CreateNewGame调用栈信息"))
            {
                ExportCallStackInfo();
            }
            
            // 显示调用栈详情
            if (showCallStackDetails && createNewGameCallStack.Length > 0)
            {
                GUI.Box(new Rect(10, 250, 800, 400), "CreateNewGame调用栈详情");
                scrollPosition = GUI.BeginScrollView(new Rect(20, 270, 780, 360), scrollPosition, new Rect(0, 0, 750, createNewGameCallStack.Length / 20));
                GUI.TextArea(new Rect(0, 0, 750, createNewGameCallStack.Length / 20), createNewGameCallStack.ToString());
                GUI.EndScrollView();
            }
        }
        
        /// <summary>
        /// 导出调用栈信息到日志文件
        /// </summary>
        private void ExportCallStackInfo()
        {
            try
            {
                string filePath = Application.dataPath + "/../CreateNewGameCallStack_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                System.IO.File.WriteAllText(filePath, createNewGameCallStack.ToString());
                Log.Info(LOG_MODULE, $"调用栈信息已导出到: {filePath}");
                
                // 显示提示信息
                GUIUtility.systemCopyBuffer = filePath;
                Debug.LogWarning("调用栈信息已导出并复制到剪贴板: " + filePath);
            }
            catch (System.Exception e)
            {
                Log.Error(LOG_MODULE, $"导出调用栈信息失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 检查GameFlowCoordinator的状态
        /// </summary>
        private void CheckCoordinatorStatus()
        {
            if (GameFlowCoordinator.Instance != null)
            {
                isCoordinatorActive = true;
                coordinatorStatus = "正常运行中";
            }
            else
            {
                isCoordinatorActive = false;
                coordinatorStatus = "未找到实例";
            }
        }
    }
}