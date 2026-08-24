using System;
using System.Linq;
using System.Text;
using MyGame.DevTools;

namespace MyGame.DevTool.Commands
{
    /// <summary>
    /// 控制台自身命令：help（帮助）、clear（清空输出）、devmode（开发者模式开关）
    /// </summary>
    public static class CoreCommands
    {
        [DebugCommand("help", Category = "控制台", Description = "显示帮助信息")]
        public static CommandResult HelpCommand(DebugCommandContext ctx, string[] args)
        {
            if (ctx.Registry == null)
            {
                ctx.PrintError("命令注册表不可用");
                return CommandResult.Error("命令注册表不可用");
            }

            var sb = new StringBuilder("可用命令:\n");
            foreach (var group in ctx.Registry.Commands.GroupBy(c => c.Category))
            {
                sb.AppendLine($"[{group.Key}]");
                foreach (var cmd in group)
                {
                    string signature = string.IsNullOrEmpty(cmd.Usage) ? cmd.Name : cmd.Usage;
                    sb.AppendLine($"  {signature} - {cmd.Description}");
                }
            }

            ctx.Print(sb.ToString());
            return CommandResult.Ok();
        }

        [DebugCommand("clear", Category = "控制台", Description = "清空控制台输出")]
        public static CommandResult ClearCommand(DebugCommandContext ctx, string[] args)
        {
            ctx.ClearOutput();
            return CommandResult.Ok();
        }

        [DebugCommand("devmode", Category = "控制台", Usage = "devmode [on|off]",
            Description = "查看或持久化开启调试控制台的开发者模式开关（对 modder 友好）")]
        public static CommandResult DevModeCommand(DebugCommandContext ctx, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                ctx.Print($"开发者模式: {(DeveloperMode.IsDeveloperModeEnabled ? "开启" : "关闭")}\n" +
                          $"调试控制台可用: {(DeveloperMode.IsDebugConsoleEnabled ? "是" : "否")}\n" +
                          "用法: devmode on|off（写入 PlayerPrefs 并保存）");
                return CommandResult.Ok();
            }

            string value = args[0].Trim().ToLowerInvariant();
            if (value == "on" || value == "1" || value == "true")
            {
                DeveloperMode.SetDeveloperModeEnabled(true);
                ctx.Print("开发者模式已开启：调试控制台将在后续会话中可用。");
                return CommandResult.Ok();
            }

            if (value == "off" || value == "0" || value == "false")
            {
                DeveloperMode.SetDeveloperModeEnabled(false);
                ctx.Print("开发者模式已关闭：当前会话中控制台仍保持打开，重开游戏后生效。");
                return CommandResult.Ok();
            }

            ctx.PrintError("无效参数，请使用: devmode on|off");
            return CommandResult.Error("无效参数");
        }
    }
}
