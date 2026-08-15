using UnityEngine;
using Logger;
using MyGame.UI;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试控制台控制器，连接模型和视图，处理用户输入和命令执行。
    /// 继承 BaseController 以遵循统一生命周期契约（Initialize/Cleanup）。
    /// </summary>
    public class DebugConsoleController : BaseController
    {
        private const string LOG_MODULE = LogModules.DEBUGCONSOLE;

        // 注入的依赖
        public DebugConsole view;
        private DebugCommandModel model;

        /// <summary>
        /// 设置控制器的视图引用
        /// </summary>
        /// <param name="consoleView">调试控制台视图实例</param>
        public void SetView(DebugConsole consoleView)
        {
            view = consoleView;
        }

        /// <summary>
        /// 初始化控制器（由 DebugConsole.TryBindController 调用）：创建命令模型
        /// </summary>
        public override void Initialize()
        {
            if (!IsInitialized)
            {
                // 创建命令模型实例
                model = new DebugCommandModel();
                model.InitializeCommands();

                base.Initialize();
            }
        }

        /// <summary>
        /// 清理控制器资源
        /// </summary>
        public override void Cleanup()
        {
            if (IsInitialized)
            {
                model = null;
                view = null;
                base.Cleanup();
            }
        }

        /// <summary>
        /// 处理用户输入的命令
        /// </summary>
        /// <param name="commandText">命令文本</param>
        public void HandleCommand(string commandText)
        {
            if (model == null)
            {
                Log.Error(LOG_MODULE, "命令模型未初始化");
                return;
            }

            if (model.ExecuteCommand(commandText))
            {
                // 命令执行成功
                Log.Info(LOG_MODULE, "执行命令: " + commandText);
            }
            else if (!string.IsNullOrEmpty(commandText))
            {
                // 命令不存在
                if (view != null)
                {
                    view.Print("未知命令，输入 help 查看可用命令。");
                }
                Log.Warning(LOG_MODULE, "未知命令: " + commandText);
            }
        }

        /// <summary>
        /// 输出信息到控制台
        /// </summary>
        /// <param name="message">消息内容</param>
        public void PrintToConsole(string message)
        {
            if (view != null)
            {
                view.Print(message);
            }
        }
    }
}
