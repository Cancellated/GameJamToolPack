using MyGame.Managers;
using MyGame.System;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试控制台组件，提供游戏内命令输入功能
    /// </summary>
    [AddComponentMenu("MyGame/Debug/DebugConsole")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InputField))]
    [RequireComponent(typeof(Text))]
    public class DebugConsole : Singleton<DebugConsole>
    {
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
        public InputField inputField;
        
        [Tooltip("输出文本框")]
        public Text outputText;
        
        #endregion

        #region 命令系统
        
        // 命令字典：键为命令名称，值为(执行方法, 描述)
        private Dictionary<string, (Action action, string description)> _commands = new();

        /// <summary>
        /// 初始化组件
        /// </summary>
        override protected void Awake()
        {
            base.Awake();
            InitializeCommands();
            
            if (outputText != null)
                outputText.text = "调试控制台已启动。输入 help 查看命令。";
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
        /// 重新开始游戏命令
        /// </summary>
        [DebugCommand("restart", Description = "重新开始游戏")]
        private void RestartCommand()
        {
            GameManager.Instance.TestRestartGame();
            Print("已重开游戏");
        }

        /// <summary>
        /// 直接胜利命令
        /// </summary>
        [DebugCommand("win", Description = "直接胜利")]
        private void WinCommand()
        {
            GameManager.Instance.TestWinGame();
            Print("已触发胜利");
        }

        /// <summary>
        /// 帮助命令
        /// </summary>
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

        #region 公共方法
        
        /// <summary>
        /// 当输入命令时调用
        /// </summary>
        public void OnCommandEntered()
        {
            string cmd = inputField.text.Trim().ToLower();
            inputField.text = "";
            
            if (_commands.TryGetValue(cmd, out var command))
            {
                command.action();
            }
            else
            {
                Print("未知命令，输入 help 查看可用命令。");
            }
        }

        /// <summary>
        /// 输出信息到控制台
        /// </summary>
        /// <param name="msg">要输出的消息</param>
        void Print(string msg)
        {
            if (outputText != null)
                outputText.text = msg;
            Debug.Log($"[控制台] {msg}");
        }
        
        #endregion
    }
}
