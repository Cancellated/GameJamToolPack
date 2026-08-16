using MyGame.Events;
using MyGame.Managers;
using MyGame.UI;

namespace MyGame.DevTool.Commands
{
    /// <summary>
    /// UI 显隐调试命令：ui <面板> [on|off]
    /// 面板取值：hud / pause / mainMenu / result / all（all 为全部非控制台面板）。
    /// 取值匹配大小写不敏感（内部统一转小写），帮助页展示驼峰规范名 mainMenu。
    /// 第二参数可选：on 强制显示、off 强制隐藏，缺省时按当前可见状态切换。
    /// </summary>
    public static class UiCommands
    {
        private const string USAGE = "ui <面板> [on|off]（面板: hud/pause/mainMenu/result/all）";

        [DebugCommand("ui", Category = "UI", Description = "面板显隐控制", Usage = USAGE,
            Params = "string 面板 | string? 开关")]
        public static CommandResult UiCommand(DebugCommandContext ctx, string[] args)
        {
            string target = args[0].ToLower().Trim();

            bool? force = null;
            if (args.Length > 1)
            {
                string mode = args[1].ToLower().Trim();
                if (mode == "on")
                {
                    force = true;
                }
                else if (mode == "off")
                {
                    force = false;
                }
                else
                {
                    return CommandResult.InvalidArgs(USAGE, $"未知开关值: {args[1]}（可选 on/off）");
                }
            }

            switch (target)
            {
                case "hud":
                    ToggleHud(ctx, force);
                    return CommandResult.Ok();
                case "pause":
                    ToggleMenu(ctx, UIType.PauseMenu, force, "暂停菜单");
                    return CommandResult.Ok();
                case "mainmenu":
                    ToggleMenu(ctx, UIType.MainMenu, force, "主菜单");
                    return CommandResult.Ok();
                case "result":
                    ToggleMenu(ctx, UIType.ResultPanel, force, "结算面板");
                    return CommandResult.Ok();
                case "all":
                    ToggleAll(ctx, force);
                    return CommandResult.Ok();
                default:
                    return CommandResult.InvalidArgs(USAGE, $"未知面板: {args[0]}（可选: hud/pause/mainMenu/result/all）");
            }
        }

        /// <summary>
        /// 判断面板当前是否应该显示：已注册时取反面板可见状态；
        /// 未注册（尚未加载）时默认请求显示，走 UIManager 的自动加载路径
        /// </summary>
        private static bool ShouldShow(UIType panelType)
        {
            var uiManager = UIManager.Instance;
            if (uiManager != null && uiManager.PanelMap.TryGetValue(panelType, out var panel))
            {
                return !panel.IsVisible;
            }
            return true;
        }

        /// <summary>
        /// 菜单类面板（暂停/主菜单/结算）走事件路径：SetUIState 负责互斥、
        /// 输入模式切换与关闭时的恢复
        /// </summary>
        private static void ToggleMenu(DebugCommandContext ctx, UIType panelType, bool? force, string label)
        {
            bool shouldShow = force ?? ShouldShow(panelType);
            GameEvents.TriggerMenuShow(panelType, shouldShow);
            ctx.Print($"{label}已{(shouldShow ? "显示" : "隐藏")}");
        }

        /// <summary>
        /// HUD 切换：关闭不能走事件路径（SetUIState 关闭 HUD 时会自动恢复 HUD，
        /// 导致无法真正隐藏），直接隐藏并同步 currentState（等价于 HidePanelSilently）
        /// </summary>
        private static void ToggleHud(DebugCommandContext ctx, bool? force)
        {
            bool shouldShow = force ?? ShouldShow(UIType.HUD);

            if (shouldShow)
            {
                GameEvents.TriggerMenuShow(UIType.HUD, true);
            }
            else
            {
                var uiManager = UIManager.Instance;
                if (uiManager != null && uiManager.PanelMap.TryGetValue(UIType.HUD, out var hud))
                {
                    hud.Hide();
                    if (uiManager.currentState == UIType.HUD)
                    {
                        uiManager.currentState = UIType.None;
                    }
                }
            }

            ctx.Print($"HUD已{(shouldShow ? "显示" : "隐藏")}");
        }

        /// <summary>
        /// 全部非控制台面板切换（控制台豁免，与 UIManager.HideAllUI 一致）。
        /// 隐藏时直接操作面板并手动恢复玩法输入，显示时走事件路径弹主菜单。
        /// </summary>
        private static void ToggleAll(DebugCommandContext ctx, bool? force)
        {
            var uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                ctx.PrintError("UIManager 不可用");
                return;
            }

            bool anyVisible = false;
            foreach (var kvp in uiManager.PanelMap)
            {
                if (kvp.Key != UIType.Console && kvp.Value.IsVisible)
                {
                    anyVisible = true;
                    break;
                }
            }

            bool show = force ?? !anyVisible;
            if (show)
            {
                GameEvents.TriggerMenuShow(UIType.MainMenu, true);
                ctx.Print("所有UI已显示");
                return;
            }

            // 直接隐藏所有非控制台面板：SetUIState 关闭面板时会自动恢复 HUD/输入模式，
            // 无法真正隐藏全部 UI
            foreach (var kvp in uiManager.PanelMap)
            {
                if (kvp.Key != UIType.Console && kvp.Value.IsVisible)
                {
                    kvp.Value.Hide();
                }
            }

            // 同步 UIManager 状态：控制台仍可见时保持 Console，否则回到 None
            bool consoleVisible = uiManager.PanelMap.TryGetValue(UIType.Console, out var consolePanel)
                && consolePanel.IsVisible;
            uiManager.currentState = consoleVisible ? UIType.Console : UIType.None;

            // 直接 Hide 绕过了 SetUIState 的输入模式恢复逻辑，手动切回玩法模式避免输入卡死
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SwitchToGamePlayMode();
            }

            ctx.Print("所有UI已隐藏");
        }
    }
}
