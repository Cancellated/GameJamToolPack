using UnityEngine;

namespace MyGame.DevTool.Commands
{
    /// <summary>
    /// 玩法调试命令：timeScale（时间缩放）
    /// </summary>
    public static class GameplayCommands
    {
        [DebugCommand("timeScale", Category = "调试", Description = "设置时间缩放", Usage = "timeScale <值>",
            Params = "float 值")]
        public static CommandResult SetTimeScale(DebugCommandContext ctx, string[] args)
        {
            // 参数已由注册表按签名校验，可安全解析
            float value = float.Parse(args[0]);
            Time.timeScale = value;
            ctx.Print($"Time.timeScale = {value}");
            return CommandResult.Ok();
        }
    }
}
