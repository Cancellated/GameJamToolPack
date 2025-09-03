using System.Collections;
using System.Collections.Generic;
using MyGame.Events;
using MyGame.Managers;
using MyGame.UI;
using MyGame.UI.Loading;
using UnityEngine;

namespace MyGame.Managers
{
    /// <summary>
    /// 全局UI管理器，负责调度和管理所有UI界面。
    /// 通过事件系统与其他模块通信，实现解耦。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        #region UI引用

        [Header("UI面板引用")]
        public List<IUIPanel> uiPanels = new();

        [Header("动画设置")]
        [Tooltip("UI淡入淡出动画时长(秒)")]
        public float fadeDuration = 0.3f;

        #endregion

        #region 状态管理

        public UIType currentState = UIType.None;
        private Dictionary<UIType, IUIPanel> _panelMap = new();

        #endregion

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            
            // 初始化面板映射
            InitializePanelMap();
            
            // 初始隐藏所有UI
            HideAllUI();

            // 注册UI相关事件监听
            GameEvents.OnMenuShow += OnMenuShow;    //这个是通用窗口显隐处理
            GameEvents.OnMainMenuShow += ShowMainMenu;
            GameEvents.OnPauseMenuShow += ShowPauseMenu;
            GameEvents.OnHUDShow += ShowHUD;
            GameEvents.OnConsoleShow += ShowConsole;
            GameEvents.OnInventoryShow += ShowInventory;
            GameEvents.OnSettingsPanelShow += ShowSettingsPanel;
            GameEvents.OnAboutPanelShow += ShowAboutPanel;
            // 加载界面显隐处理方法
            GameEvents.OnSceneLoadStart += ShowLoading;
            GameEvents.OnSceneLoadComplete += HideLoading;
        }

        private void InitializePanelMap()
        {
            _panelMap.Clear();
            foreach (var panel in uiPanels)
            {
                if (panel != null && !_panelMap.ContainsKey(panel.PanelType))
                {
                    _panelMap.Add(panel.PanelType, panel);
                    panel.Initialize();
                }
            }
        }

        #region 面板显示处理方法

        /// <summary>
        /// 显示或隐藏设置面板
        /// </summary>
        private void ShowSettingsPanel(bool show)
        {
            SetUIState(UIType.SettingsPanel, show);
        }

        /// <summary>
        /// 显示或隐藏关于面板
        /// </summary>
        private void ShowAboutPanel(bool show)
        {
            SetUIState(UIType.AboutPanel, show);
        }
        #endregion
        private void OnDestroy()
        {
            // 注销事件监听
            GameEvents.OnMenuShow -= OnMenuShow;
            GameEvents.OnMainMenuShow -= ShowMainMenu;
            GameEvents.OnPauseMenuShow -= ShowPauseMenu;
            GameEvents.OnHUDShow -= ShowHUD;
            GameEvents.OnConsoleShow -= ShowConsole;
            GameEvents.OnInventoryShow -= ShowInventory;
            GameEvents.OnSettingsPanelShow -= ShowSettingsPanel;
            GameEvents.OnAboutPanelShow -= ShowAboutPanel;
            GameEvents.OnSceneLoadStart -= ShowLoading;
            GameEvents.OnSceneLoadComplete -= HideLoading;
        }

        #endregion

        #region UI控制核心方法

        /// <summary>
        /// 隐藏所有UI界面(控制台除外)
        /// </summary>
        private void HideAllUI()
        {
            foreach (var panel in _panelMap.Values)
            {
                if (panel.PanelType != UIType.Console)
                {
                    panel.Hide();
                }
            }
            currentState = UIType.None;
        }

        /// <summary>
        /// 设置UI状态并处理互斥关系
        /// </summary>
        private void SetUIState(UIType state, bool show)
        {
            // 处理互斥关系
            if (show)
            {
                // 使用InputManager切换输入模式
                if (InputManager.Instance != null)
                {
                    // 对于需要完全UI控制的界面，切换到UI模式
                    if (state != UIType.Console && state != UIType.Loading)
                    {
                        InputManager.Instance.SwitchToUIMode();
                    }
                }
                
                switch (state)
                {
                    case UIType.MainMenu:
                        SetUIState(UIType.PauseMenu, false);
                        SetUIState(UIType.HUD, false);
                        break;
                    case UIType.PauseMenu:
                        SetUIState(UIType.MainMenu, false);
                        SetUIState(UIType.ResultPanel, false);
                        break;
                    case UIType.ResultPanel:
                        SetUIState(UIType.PauseMenu, false);
                        SetUIState(UIType.HUD, false);
                        break;
                    case UIType.Inventory:
                        SetUIState(UIType.MainMenu, false);
                        SetUIState(UIType.PauseMenu, false);
                        break;
                    case UIType.SettingsPanel:
                        SetUIState(UIType.MainMenu, false);
                        SetUIState(UIType.PauseMenu, false);
                        SetUIState(UIType.ResultPanel, false);
                        break;
                    case UIType.AboutPanel:
                        SetUIState(UIType.MainMenu, false);
                        SetUIState(UIType.PauseMenu, false);
                        SetUIState(UIType.ResultPanel, false);
                        break;
                    case UIType.Loading:
                        // 加载界面不与其他UI互斥
                    case UIType.Console:
                        // 调试界面不与其他UI互斥
                        break;
                }
            }
            else if (currentState == state && currentState != UIType.None && currentState != UIType.Loading && currentState != UIType.Console)
            {
                // 当关闭最后一个UI时，切换回游戏玩法模式
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.SwitchToGamePlayMode();
                }
            }

            // 更新当前状态
            if (show) currentState = state;
            else if (currentState == state) currentState = UIType.None;

            // 根据状态显示/隐藏对应UI
            if (_panelMap.TryGetValue(state, out var panel))
            {
                if (show)
                    panel.Show();
                else
                    panel.Hide();
            }
        }

        #endregion

        #region UI事件响应(独特UI处理)

        private void ShowMainMenu(bool show)
        {
            SetUIState(UIType.MainMenu, show);
        }

        private void ShowPauseMenu(bool show)
        {
            SetUIState(UIType.PauseMenu, show);
        }

        private void ShowHUD(bool show)
        {
            SetUIState(UIType.HUD, show);
        }

        private void ShowConsole(bool show)
        {
            SetUIState(UIType.Console, show);
        }
        /// <summary>
        /// 显示或隐藏背包界面
        /// 特殊动画应在对应的BaseUI子类中通过重写Show/Hide方法实现
        /// </summary>
        private void ShowInventory(bool show)
        {
            SetUIState(UIType.Inventory, show);
        }
        
        private void OnMenuShow(UIType state, bool show)
        {
            SetUIState(state, show);
        }

        #endregion

        #region 加载界面处理方法

        /// <summary>
        /// 显示加载界面
        /// </summary>
        private void ShowLoading(string sceneName)
        {
            SetUIState(UIType.Loading, true);
            
            // 查找Loading组件并调用其回调方法
            if (_panelMap.TryGetValue(UIType.Loading, out var loadingPanel))
            {
                var loadingScreen = loadingPanel as LoadingScreen;
                if (loadingScreen != null)
                {
                    loadingScreen.OnSceneLoadStarted(sceneName);
                }
            }
        }

        /// <summary>
        /// 隐藏加载界面
        /// </summary>
        private void HideLoading(string sceneName)
        {
            // 查找Loading组件并调用其回调方法
            if (_panelMap.TryGetValue(UIType.Loading, out var loadingPanel))
            {
                var loadingScreen = loadingPanel as LoadingScreen;
                if (loadingScreen != null)
                {
                    loadingScreen.OnSceneLoadCompleted(sceneName);
                }
            }
            
            SetUIState(UIType.Loading, false);
        }

        #endregion
    }
}