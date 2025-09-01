using System.Collections;
using System.Collections.Generic;
using MyGame.Events;
using MyGame.UI;
using MyGame.UI.Loading;
using UnityEngine;
using Logger;
using static Logger.LogModules;

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
        [Tooltip("拖入实现了IUIPanel接口的GameObject")]
        public List<GameObject> uiPanels = new();

        [Header("动画设置")]
        [Tooltip("UI淡入淡出动画时长(秒)")]
        public float fadeDuration = 0.3f;

        #endregion

        #region 状态管理

        public UIType currentState = UIType.None;
        private GameControl _inputActions;
        private Dictionary<UIType, IUIPanel> _panelMap;
        
        #endregion

        const string module = LogModules.UIMANAGER;
        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            _inputActions = GameManager.Instance.InputActions;
            _panelMap = new Dictionary<UIType, IUIPanel>();
            
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
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
            // 场景卸载处理
            GameEvents.OnSceneUnload += OnSceneUnload;
        }

        private void InitializePanelMap()
        {
            _panelMap.Clear();
            foreach (var panelObj in uiPanels)
            {
                if (panelObj != null)
                {
                    var panel = panelObj.GetComponent<IUIPanel>();
                    if (panel != null && !_panelMap.ContainsKey(panel.PanelType))
                    {
                        _panelMap.Add(panel.PanelType, panel);
                        panel.Initialize();
                    }
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
                _inputActions.GamePlay.Disable();
                _inputActions.UI.Enable();
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

        #region UI事件响应

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
        /// 处理场景加载完成事件
        /// </summary>
        private void OnSceneLoadComplete(string sceneName)
        {
            // 重新初始化面板映射，确保获取新场景中的UI
            InitializePanelMap();

            // 隐藏加载界面
            if (_panelMap.TryGetValue(UIType.Loading, out var loadingPanel))
            {
                var loadingScreen = loadingPanel as LoadingScreen;
                if (loadingScreen != null)
                {
                    loadingScreen.OnSceneLoadCompleted(sceneName);
                }
                SetUIState(UIType.Loading, false);
            }

            // 根据加载的场景类型显示相应的UI
            UpdateUIByScene(sceneName);
        }

        /// <summary>
        /// 处理场景卸载事件
        /// </summary>
        private void OnSceneUnload(string sceneName)
        {
            // 清空面板映射，因为当前场景中的UI将被销毁
            _panelMap.Clear();
            Log.Info(module, $"场景 '{sceneName}' 卸载，清空UI面板映射");
        }

        /// <summary>
        /// 根据场景更新UI显示状态
        /// </summary>
        /// <param name="sceneName">当前场景名称</param>
        private void UpdateUIByScene(string sceneName)
        {
            // 隐藏所有UI
            HideAllUI();

            // 根据不同场景显示不同的UI
            if (sceneName == "MainMenu")
            {
                // 主菜单场景显示主菜单
                GameEvents.TriggerMainMenuShow(true);
            }
            else if (sceneName == "Level Select")
            {
                // 关卡选择场景显示关卡选择UI
                // 这里可以根据实际情况添加关卡选择UI的显示逻辑
            }
            else
            {
                // 游戏场景显示HUD
                GameEvents.TriggerHUDShow(true);
            }

            Log.Info(module, $"场景 '{sceneName}' 加载完成，已更新UI状态");
        }

        #endregion
    }
}