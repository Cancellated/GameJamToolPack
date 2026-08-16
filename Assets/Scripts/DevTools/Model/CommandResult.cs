namespace MyGame.DevTool
{
    /// <summary>
    /// 命令执行状态：区分成功、未知命令、参数错误与执行异常
    /// </summary>
    public enum CommandStatus
    {
        /// <summary>执行成功</summary>
        Ok,

        /// <summary>命令不存在</summary>
        NotFound,

        /// <summary>参数个数或类型不合法</summary>
        InvalidArgs,

        /// <summary>执行过程中抛出异常</summary>
        Error,
    }

    /// <summary>
    /// 命令执行结果：状态 + 反馈消息（+ 用法说明）。
    /// Controller 按 Status 输出不同的反馈文案，避免"异常"与"未知命令"混淆。
    /// </summary>
    public class CommandResult
    {
        /// <summary>执行状态</summary>
        public CommandStatus Status { get; }

        /// <summary>反馈消息：NotFound 时为命令名，Error 时为异常信息，InvalidArgs 时为具体原因</summary>
        public string Message { get; }

        /// <summary>用法说明（InvalidArgs 时回显给用户）</summary>
        public string Usage { get; }

        private CommandResult(CommandStatus status, string message = "", string usage = "")
        {
            Status = status;
            Message = message ?? "";
            Usage = usage ?? "";
        }

        /// <summary>执行成功（message 为空时表示命令已自行输出，Controller 不额外打印）</summary>
        public static CommandResult Ok(string message = "")
        {
            return new CommandResult(CommandStatus.Ok, message);
        }

        /// <summary>命令不存在</summary>
        public static CommandResult NotFound(string commandName)
        {
            return new CommandResult(CommandStatus.NotFound, commandName);
        }

        /// <summary>参数错误（usage 为正确用法，message 为具体原因，可为空）</summary>
        public static CommandResult InvalidArgs(string usage, string message = "")
        {
            return new CommandResult(CommandStatus.InvalidArgs, message, usage);
        }

        /// <summary>执行异常</summary>
        public static CommandResult Error(string message)
        {
            return new CommandResult(CommandStatus.Error, message);
        }
    }
}
