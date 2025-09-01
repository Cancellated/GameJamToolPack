using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Managers;
using MyGame.Events;
using MyGame.UI;
using Logger;
using static MyGame.DevTool.DebugConsole;

namespace MyGame.DevTool
{
    /// <summary>
    /// 调试命令集，集中管理所有调试命令
    /// </summary>
    public class DebugCommands : MonoBehaviour
    {
        
        // 引用控制台实例
        private DebugConsole m_debugConsole;
        
        private void Awake()
        {
            m_debugConsole = DebugConsole.Instance;
        }
        
        #region 帮助命令
        [DebugCommand("help", Description = "显示帮助信息")]
        internal void HelpCommand()
        {
            var helpText = "可用命令:\n";
            if (m_debugConsole != null)
            {
                foreach (var cmd in m_debugConsole.GetCommands())
                {
                    helpText += $"{cmd.Key}: {cmd.Value}\n";
                }
            }
            Print(helpText);
        }
        #endregion
        
        #region 游戏核心状态调试命令
        // 重新开始游戏命令
        [DebugCommand("restart", Description = "重新开始游戏")]
        internal void RestartCommand()
        {
            GameEvents.TriggerGameStart();
            Print("已重开游戏");
        }

        // 直接胜利命令
        [DebugCommand("win", Description = "直接胜利")]
        internal void WinCommand()
        {
            GameEvents.TriggerGameOver(true);
            Print("已触发胜利");
        }

        // 直接失败命令
        [DebugCommand("lose", Description = "直接失败")]
        internal void LoseCommand()
        {
            GameEvents.TriggerGameOver(false);
            Print("已触发失败");
        }
        #endregion

        #region UI调试命令
        [DebugCommand("toggleallui", Description = "切换所有UI界面显隐")]
        internal void ToggleAllUI()
        {
            bool shouldShow = UIManager.Instance.currentState == UIType.None;
            GameEvents.TriggerMenuShow(shouldShow ? UIType.MainMenu : UIType.None, shouldShow);
            Print($"所有UI已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("togglemainmenu", Description = "切换主菜单显隐")]
        internal void ToggleMainMenu()
        {
            bool shouldShow = UIManager.Instance.currentState != UIType.MainMenu;
            GameEvents.TriggerMenuShow(shouldShow ? UIType.MainMenu : UIType.None, shouldShow);
            Print($"主菜单已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("togglepausemenu", Description = "切换暂停菜单显隐")]
        internal void TogglePauseMenu()
        {
            bool shouldShow = UIManager.Instance.currentState != UIType.PauseMenu;
            GameEvents.TriggerMenuShow(shouldShow ? UIType.PauseMenu : UIType.None, shouldShow);
            Print($"暂停菜单已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("toggleresultpanel", Description = "切换结算面板显隐")]
        internal void ToggleResultPanel()
        {
            bool shouldShow = UIManager.Instance.currentState != UIType.ResultPanel;
            GameEvents.TriggerMenuShow(shouldShow ? UIType.ResultPanel : UIType.None, shouldShow);
            Print($"结算面板已{(shouldShow ? "显示" : "隐藏")}");
        }

        [DebugCommand("togglehud", Description = "切换HUD显隐")]
        internal void ToggleHUD()
        {
            bool shouldShow = UIManager.Instance.currentState != UIType.HUD;
            GameEvents.TriggerMenuShow(shouldShow ? UIType.HUD : UIType.None, shouldShow);
            Print($"HUD已{(shouldShow ? "显示" : "隐藏")}");
        }
        #endregion
        
        #region 工具方法
        /// <summary>
        /// 输出信息到控制台
        /// </summary>
        /// <param name="msg">要输出的消息</param>
        private void Print(string msg)
        {
            if (m_debugConsole != null)
            {
                m_debugConsole.Print(msg);
            }
        }
        #endregion
    }
}
