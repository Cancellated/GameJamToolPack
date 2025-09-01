using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using System.Linq;
using MyGame.Managers;
using MyGame.Events;
using Logger;
using UnityEngine.InputSystem;
using MyGame.UI;

namespace MyGame.DevTool
{   
    public class DebugConsole : BaseUI, IUIPanel
    {
        public static DebugConsole Instance { get; private set; }
        
        private const string LOG_MODULE = LogModules.DEBUGCONSOLE;
        //最大日志保留数
        private const int MAX_LOG_LINES = 100;
        //按键监听
        private GameControl inputActions;
        #region 特性定义
        
        /// <summary>
        /// 调试命令特性，用于标记命令方法
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public class DebugCommandAttribute : Attribute
        {
            /// <summary>命令名称</summary>
            public string CommandName { get; }
            
            /// <summary>命令描述</summary> 
            public string Description { get; set; }
            
            /// <summary>
            /// 创建调试命令特性
            /// </summary>
            /// <param name="name">命令名称</param>
            public DebugCommandAttribute(string name)
            {
                CommandName = name;
            }
        }
        
        #endregion

        #region UI引用
        
        [Header("UI组件引用")]
        [Tooltip("命令输入框")]
        public TMP_InputField inputField;
        
        [Tooltip("输出文本框")]
        public TMP_Text outputText;
        
        [Tooltip("滚动视图组件")]
        public UnityEngine.UI.ScrollRect scrollRect;

        [Header("层级设置")]
        [Tooltip("控制台Canvas的Sorting Order。值越高，显示层级越高，不易被其他UI遮挡。")]
        public int canvasSortingOrder = 1000; // 设置较高的默认值，确保控制台显示在大多数UI上层
        #endregion

        #region 命令系统
        
        // 命令字典：键为命令名称，值为(执行方法, 描述)
        private readonly Dictionary<string, (Action action, string description)> _commands = new();

        /// <summary>
        /// 获取命令字典（供DebugCommands类使用）
        /// </summary>
        /// <returns>命令名称和描述的字典</returns>
        public Dictionary<string, string> GetCommands()
        {
            Dictionary<string, string> commandDescriptions = new();
            foreach (var cmd in _commands)
            {
                commandDescriptions[cmd.Key] = cmd.Value.description;
            }
            return commandDescriptions;
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            
            // 单例模式实现
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCommands();
                inputActions = new GameControl();
                // 确保canvasGroup已初始化
                if (m_canvasGroup == null)
                {
                    m_canvasGroup = GetComponent<CanvasGroup>();
                }

                if (m_canvasGroup != null)
                {
                    m_canvasGroup.alpha = 0;
                    m_canvasGroup.interactable = false;
                    m_canvasGroup.blocksRaycasts = false;
                }
                
                if (outputText != null)
                    outputText.text = "调试控制台已启动。输入 help 查看命令。";
                
                // 绑定输入框事件处理器
                if (inputField != null)
                {
                    inputField.onEndEdit.AddListener(delegate { OnCommandEntered(); });
                    // 设置输入行为模式为提交时结束编辑
                    inputField.lineType = TMP_InputField.LineType.SingleLine;
                }
                // 设置Canvas排序层级
                SetCanvasSortingOrder();
                
                // 初始隐藏状态
                Hide();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// 设置Canvas的Sorting Order，确保控制台显示在其他UI上层
        /// </summary>
        private void SetCanvasSortingOrder()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = canvasSortingOrder;
            }
            else
            {
                Log.Warning(LOG_MODULE, "未找到父级Canvas组件，无法设置排序层级。", this);
            }
        }

        /// <summary>
        /// 初始化所有命令
        /// </summary>
        void InitializeCommands()
        {
            // 查找场景中的DebugCommands实例
            DebugCommands debugCommands = FindObjectOfType<DebugCommands>();
            if (debugCommands == null)
            {
                // 如果没有找到实例，创建一个新的
                GameObject commandsObj = new("DebugCommands");
                debugCommands = commandsObj.AddComponent<DebugCommands>();
                DontDestroyOnLoad(commandsObj);
            }
            
            // 使用反射获取DebugCommands类中所有标记了DebugCommand特性的方法
            foreach (var method in typeof(DebugCommands).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<DebugCommandAttribute>();
                if (attr != null)
                {
                    // 将方法添加到命令字典
                    _commands[attr.CommandName] = (
                        () => method.Invoke(debugCommands, null),
                        attr.Description
                    );
                }
            }
        }
        
        #endregion

        #region 按键监听
        void Update()
        {
            if (inputActions != null && inputActions.GamePlay.Console.triggered)
            {
                ToggleShowConsole();
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 监听按键显隐控制台
        /// </summary>
        public void ToggleShowConsole()
        {
            // 不再直接操作CanvasGroup，而是触发事件让UIManager处理
            GameEvents.TriggerConsoleShow(!IsVisible);
        }
        
        // 重写BaseUI的Show方法
        public override void Show()
        {
            m_canvasGroup.alpha = 1;
            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
            IsVisible = true;
        }
        
        // 重写BaseUI的Hide方法
        public override void Hide()
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
            IsVisible = false;
        }
        
        // 生命周期回调
        protected override void OnShow()
        {
            base.OnShow();
            // 控制台显示时的逻辑
            Log.Info(LOG_MODULE, "显示DebugConsole", this);
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            // 控制台隐藏时的逻辑
            Log.Info(LOG_MODULE, "隐藏DebugConsole", this);
        }
        
        /// <summary>
        /// 当输入命令时调用
        /// </summary>
        /// <param name="input">输入框的当前文本内容（TMP_InputField的onEndEdit事件会传入）</param>
        public void OnCommandEntered(string input = null)
        {
            // 获取输入文本
            string cmd = inputField.text.Trim().ToLower();
            if (string.IsNullOrEmpty(input))
            {
                // 清除输入框
                inputField.text = "";
                
                if (_commands.TryGetValue(cmd, out var command))
                {
                    command.action();
                }
                else if (!string.IsNullOrEmpty(cmd))
                {
                    Print("未知命令，输入 help 查看可用命令。");
                }
                
                // 重新激活输入框以便继续输入
                inputField.ActivateInputField();
            }
        }

        /// <summary>
        /// 输出信息到控制台
        /// </summary>
        /// <param name="msg">要输出的消息</param>
        internal void Print(string msg)
        {
            if (outputText != null) 
            {
                // 分割现有日志为行数组
                var lines = outputText.text.Split('\n');

                
                // 如果超过最大行数，移除最早的行
                if (lines.Length >= MAX_LOG_LINES) 
                {
                    lines = lines.Skip(1).ToArray();
                }
                
                // 添加新日志并重新组合
                outputText.text = string.Join("\n", lines) + $"\n{msg}";
                
                // 强制布局更新
                Canvas.ForceUpdateCanvases();
                
                // 当Scroll Rect存在且内容需要滚动时执行滚动到底部
                if (scrollRect != null && outputText.preferredHeight > outputText.rectTransform.rect.height)
                {
                    scrollRect.verticalNormalizedPosition = 0f; // 0f 表示滚动到底部
                }
            }
            Log.Info(LOG_MODULE, msg, this);
        }
        
        #endregion
    }
}