using System.Linq;
using System.Text;

namespace MyGame.DevTool.Commands
{
    /// <summary>
    /// 控制台自身命令：help（帮助）、clear（清空输出）
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
    }
}
