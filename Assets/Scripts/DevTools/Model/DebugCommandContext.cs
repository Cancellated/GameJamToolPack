namespace MyGame.DevTool
{
    /// <summary>
    /// 命令执行上下文：命令通过它输出信息、清空控制台与访问注册表。
    /// 由 Controller 在每次执行时创建并注入，取代此前每执行一次命令
    /// 就 FindObjectOfType 全场景扫描控制器的方式。
    /// </summary>
    public class DebugCommandContext
    {
        /// <summary>当前控制台控制器（可用于需要更广能力的命令）</summary>
        public DebugConsoleController Controller { get; }

        /// <summary>命令注册表（help 等命令需要枚举全部命令）</summary>
        public DebugCommandRegistry Registry { get; }

        public DebugCommandContext(DebugConsoleController controller, DebugCommandRegistry registry)
        {
            Controller = controller;
            Registry = registry;
        }

        /// <summary>输出一行信息到控制台</summary>
        public void Print(string message)
        {
            Controller?.PrintToConsole(message);
        }

        /// <summary>以错误样式输出一行信息到控制台</summary>
        public void PrintError(string message)
        {
            Controller?.PrintToConsole("错误: " + message);
        }

        /// <summary>清空控制台输出区</summary>
        public void ClearOutput()
        {
            Controller?.ClearConsoleOutput();
        }
    }
}
