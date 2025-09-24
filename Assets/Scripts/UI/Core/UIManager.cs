using System;
using System.Collections;
using System.Collections.Generic;
using Logger;
using MyGame.Events;
using MyGame.Managers;
using MyGame.UI;
using MyGame.UI.Loading;
using MyGame.UI.Loading.View;
using UnityEngine;

namespace MyGame.Managers
{
    /// <summary>
    /// 全局UI管理器，负责调度和管理所有UI界面。
    /// 通过事件系统与其他模块通信，实现解耦。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        public const string module = LogModules.UIMANAGER;
        #region UI引用

        [System.Serializable]
        public class UIPanelWrapper
        {
            [Tooltip("UI面板组件")]
            public MonoBehaviour panel;
            
            [Tooltip("IUIPanel接口组件 - 直接指定面板接口")]
            public IUIPanel iUIPanel;
        }
        
        [Header("UI面板引用")]
        [Tooltip("UI面板列表 - 编辑器中可拖拽任意MonoBehaviour组件，运行时会自动过滤出实现IUIPanel接口的组件")]
        public List<UIPanelWrapper> uiPanelWrappers = new();
        
        // 用于运行时访问的IUIPanel列表
        [Header("运行时访问的IUIPanel列表")]
        [Tooltip("运行时访问的IUIPanel列表 - 自动填充，无需手动操作")]
        [SerializeField]
        private List<IUIPanel> uiPanels = new();

        #endregion

        #region 状态管理

        public UIType currentState = UIType.None;
        [Header("面板映射")]
        private Dictionary<UIType, IUIPanel> _panelMap; // 面板类型到面板实例的映射

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            // 初始化面板映射字典
            _panelMap = new Dictionary<UIType, IUIPanel>();
            
            // 注册UI相关事件监听
            GameEvents.OnMenuShow += OnMenuShow;    // UI显隐处理
        }
        
        /// <summary>
        /// 开始时初始化面板映射（移至此确保其他组件已就绪）
        /// </summary>
        private void Start()
        {
            // 初始化面板映射-在Start中执行以确保其他组件已就绪
            InitializePanelMap();
            // 初始隐藏所有UI
            HideAllUI();
            // 当主菜单面板存在时才显示主菜单
            if (_panelMap.ContainsKey(UIType.MainMenu))
            {
                SetUIState(UIType.MainMenu, true);
            }
        }

        /// <summary>
        /// 初始化UI面板映射字典
        /// 从包装器列表中收集所有有效的IUIPanel组件并建立类型到实例的映射
        /// </summary>
        private void InitializePanelMap()
        {
            Log.Info(module, "开始初始化面板映射");
            
            // 清空现有映射和面板列表
            _panelMap.Clear();
            uiPanels.Clear();
            
            // 从包装器列表中收集面板
            foreach (var wrapper in uiPanelWrappers)
            {
                if (wrapper == null)
                {
                    Log.Warning(module, "包装器为空");
                    continue;
                }

                // 获取面板组件（三个层次的检查）
                IUIPanel panel = GetPanelFromWrapper(wrapper);
                
                // 添加到运行时列表和映射字典中
                if (panel != null)
                {
                    AddPanelToMap(panel);
                }
            }
            
            // 输出最终映射内容
            Log.Info(module, "面板映射初始化完成，共包含 " + _panelMap.Count + " 个面板类型");
        }
        
        private void OnDestroy()
        {
            // 注销事件监听
            GameEvents.OnMenuShow -= OnMenuShow;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 从包装器中获取有效的IUIPanel组件
        /// 按优先级进行三个层次的检查
        /// </summary>
        /// <param name="wrapper">UI面板包装器</param>
        /// <returns>有效的IUIPanel组件，如果无法获取则返回null</returns>
        private IUIPanel GetPanelFromWrapper(UIPanelWrapper wrapper)
        {
            // 1. 首先检查是否直接指定了IUIPanel
            if (wrapper.iUIPanel != null)
            {
                return wrapper.iUIPanel;
            }
            
            // 2. 如果没有直接指定IUIPanel，则检查MonoBehaviour是否实现了IUIPanel接口
            else if (wrapper.panel != null)
            {
                if (wrapper.panel is IUIPanel)
                {
                    IUIPanel panel = wrapper.panel as IUIPanel;
                    return panel;
                }
                // 3. 如果MonoBehaviour没有实现IUIPanel接口，则尝试从同一GameObject上查找IUIPanel组件
                else
                {
                    if (wrapper.panel.TryGetComponent<IUIPanel>(out var foundPanel))
                    {
                        return foundPanel;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            
            // 包装器的panel字段为空
            Log.Warning(module, "包装器的panel字段为空");
            return null;
        }
        
        /// <summary>
        /// 将面板添加到运行时列表和映射字典中
        /// </summary>
        /// <param name="panel">要添加的面板</param>
        private void AddPanelToMap(IUIPanel panel)
        {
            uiPanels.Add(panel);
            
            // 检查面板类型是否已存在于映射中
            if (!_panelMap.ContainsKey(panel.PanelType))
            {
                _panelMap.Add(panel.PanelType, panel);
                panel.Initialize();
            }
            else
            {
                Log.Info(module, "面板类型已存在于映射中: " + panel.PanelType + " (" + panel.GetType().Name + ")");
            }
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
                        SetUIState(UIType.SettingsPanel, false);
                        SetUIState(UIType.AboutPanel, false);
                        break;
                    case UIType.PauseMenu:
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
            // 当关闭非加载界面时，尝试切换回游戏玩法模式
            else if (currentState == state && currentState != UIType.None && currentState != UIType.Loading && currentState != UIType.Console)
            {
                // 当关闭最后一个UI时，切换回游戏玩法模式
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.SwitchToGamePlayMode();
                    // 确保在游戏玩法模式下显示HUD
                    SetUIState(UIType.HUD, true);
                }
            }

            // 更新当前状态
            if (show) currentState = state;
            else if (currentState == state) currentState = UIType.None;

            // 根据状态显示/隐藏对应UI
            Log.Info(module, "尝试显示/隐藏UI类型: " + state + ", show: " + show);
            
            if (_panelMap.TryGetValue(state, out var panel))
            {               
                if (show)
                {
                    panel.Show();
                }
                else
                {
                    panel.Hide();
                }
            }
            else
            {
                Log.Error(module, "未找到对应UI类型的面板: " + state);
            }
        }

        #endregion

        #region UI事件响应
        private void OnMenuShow(UIType state, bool show)
        {
            SetUIState(state, show);
        }

        #endregion

 
    }
}