using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using System.Linq;
using MyGame.Managers;
using MyGame.Events;
using Logger;

namespace MyGame.DevTool
{   
    public class DebugConsole : Singleton<DebugConsole>
    {
        private const string LOG_MODULE = LogModules.DEBUGCONSOLE;
        //最大日志保留数
        private const int MAX_LOG_LINES = 100;
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
        /// 初始化组件
        /// </summary>
        override protected void Awake()
        {
            base.Awake();
            InitializeCommands();
            
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
            // 使用反射获取所有标记了DebugCommand特性的方法
            foreach (var method in GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<DebugCommandAttribute>();
                if (attr != null)
                {
                    // 将方法添加到命令字典
                    _commands[attr.CommandName] = (
                        () => method.Invoke(this, null),
                        attr.Description
                    );
                }
            }
        }
        
        #endregion

        #region 命令方法
        
        /// <summary>
        /// 切换控制台显示状态
        /// </summary>
        public void ToggleConsole()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            
            if (gameObject.activeSelf)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }
        
        #region 帮助命令
        [DebugCommand("help", Description = "显示帮助信息")]
        private void HelpCommand()
        {
            var helpText = "可用命令:\n";
            foreach (var cmd in _commands)
            {
                helpText += $"{cmd.Key}: {cmd.Value.description}\n";
            }
            Print(helpText);
        }
        #endregion
        #region 游戏核心状态调试命令
        // 重新开始游戏命令
        [DebugCommand("restart", Description = "重新开始游戏")]
        private void RestartCommand()
        {
            GameEvents.TriggerGameStart();
            Print("已重开游戏");
        }

        // 直接胜利命令
        [DebugCommand("win", Description = "直接胜利")]
        private void WinCommand()
        {
            GameEvents.TriggerGameOver(true);
            Print("已触发胜利");
        }

        // 直接失败命令
        [DebugCommand("lose", Description = "直接失败")]
        private void LoseCommand()
        {
            GameEvents.TriggerGameOver(false);
            Print("已触发失败");
        }
        #endregion

        #region UI调试命令
        [DebugCommand("toggleallui", Description = "切换所有UI界面显隐")]
        private void ToggleAllUI()
        {
            bool shouldShow = UIManager.Instance.currentState == UIManager.UIState.None;
            GameEvents.TriggerMenuShow(shouldShow ? UIManager.UIState.MainMenu : UIManager.UIState.None, shouldShow);
            Print($"所有UI已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("togglemainmenu", Description = "切换主菜单显隐")]
        private void ToggleMainMenu()
        {
            bool shouldShow = UIManager.Instance.currentState != UIManager.UIState.MainMenu;
            GameEvents.TriggerMenuShow(shouldShow ? UIManager.UIState.MainMenu : UIManager.UIState.None, shouldShow);
            Print($"主菜单已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("togglepausemenu", Description = "切换暂停菜单显隐")]
        private void TogglePauseMenu()
        {
            bool shouldShow = UIManager.Instance.currentState != UIManager.UIState.PauseMenu;
            GameEvents.TriggerMenuShow(shouldShow ? UIManager.UIState.PauseMenu : UIManager.UIState.None, shouldShow);
            Print($"暂停菜单已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("toggleresultpanel", Description = "切换结算面板显隐")]
        private void ToggleResultPanel()
        {
            bool shouldShow = UIManager.Instance.currentState != UIManager.UIState.ResultPanel;
            GameEvents.TriggerMenuShow(shouldShow ? UIManager.UIState.ResultPanel : UIManager.UIState.None, shouldShow);
            Print($"结算面板已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("togglehud", Description = "切换HUD显隐")]
        private void ToggleHUD()
        {
            bool shouldShow = UIManager.Instance.currentState != UIManager.UIState.HUD;
            GameEvents.TriggerMenuShow(shouldShow ? UIManager.UIState.HUD : UIManager.UIState.None, shouldShow);
            Print($"HUD已{(shouldShow ? "显示" : "隐藏")}");
        }
        #endregion
        
        #endregion

        #region 公共方法
        
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
        void Print(string msg)
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