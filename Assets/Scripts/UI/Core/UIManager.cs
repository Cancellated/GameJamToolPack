using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logger;
using MyGame.Events;
using MyGame.Managers;
using MyGame.UI;
using MyGame.UI.Core;
using MyGame.UI.Loading;
using MyGame.UI.Loading.View;
using Unity.VisualScripting;
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

        #region 加载配置     
        /// <summary>
        /// 加载界面的最小显示时间（秒）
        /// 确保加载界面至少显示指定时间，避免一闪而过
        /// </summary>
        [Header("加载界面配置")]
        [Tooltip("加载界面的最小显示时间（秒）")]
        [SerializeField]
        private float m_minLoadingDisplayTime = 1.5f;

        private float m_loadingStartTime = 0f; // 记录加载界面显示的开始时间
        #endregion

        #region 状态管理

        public UIType currentState = UIType.None;
        [Header("面板映射")]
        public Dictionary<UIType, IUIPanel> PanelMap; // 面板类型到面板实例的映射

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            // 初始化面板映射字典
            PanelMap = new Dictionary<UIType, IUIPanel>();
            
            // 注册UI相关事件监听
            GameEvents.OnMenuShow += OnMenuShow;    // UI显隐处理
            GameEvents.OnSceneLoadStart += ShowLoading;    // 场景加载开始时显示加载界面
            GameEvents.OnSceneLoadComplete += HideLoading;  // 场景加载完成时隐藏加载界面
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
            if (PanelMap.ContainsKey(UIType.MainMenu))
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
            PanelMap.Clear();
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
            Log.Info(module, "面板映射初始化完成，共包含 " + PanelMap.Count + " 个面板类型");
        }
        
        private void OnDestroy()
        {
            // 注销事件监听
            GameEvents.OnMenuShow -= OnMenuShow;
            GameEvents.OnSceneLoadStart -= ShowLoading;
            GameEvents.OnSceneLoadComplete -= HideLoading;
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
            if (!PanelMap.ContainsKey(panel.PanelType))
            {
                PanelMap.Add(panel.PanelType, panel);
                panel.Initialize();
            }
            else
            {
                Log.Info(module, "面板类型已存在于映射中: " + panel.PanelType + " (" + panel.GetType().Name + ")");
            }
        }

        #endregion

        #region 动态面板管理

        /// <summary>
        /// 动态注册UI面板到管理器
        /// 用于从预制体实例化的面板注册到UIManager
        /// </summary>
        /// <param name="panel">要注册的IUIPanel接口实现</param>
        /// <returns>注册是否成功</returns>
        public bool RegisterUIPanel(IUIPanel panel)
        {
            if (panel == null)
            {
                Log.Error(module, "尝试注册空面板");
                return false;
            }

            if (!PanelMap.ContainsKey(panel.PanelType))
            {
                AddPanelToMap(panel);
                Log.Info(module, "成功动态注册面板: " + panel.PanelType + " (" + panel.GetType().Name + ")");
                return true;
            }
            else
            {
                Log.Info(module, "面板类型已存在于映射中，跳过注册: " + panel.PanelType);
                return false;
            }
        }

        /// <summary>
        /// 从管理器中注销UI面板
        /// </summary>
        /// <param name="panelType">要注销的面板类型</param>
        /// <returns>注销是否成功</returns>
        public bool UnregisterUIPanel(UIType panelType)
        {
            if (PanelMap.TryGetValue(panelType, out var panel))
            {
                PanelMap.Remove(panelType);
                uiPanels.Remove(panel);
                panel.Cleanup();
                Log.Info(module, "成功注销面板: " + panelType);
                return true;
            }
            else
            {
                Log.Warning(module, "未找到要注销的面板类型: " + panelType);
                return false;
            }
        }

        #endregion

        #region UI控制核心方法
        
        /// <summary>
        /// 隐藏所有UI界面(控制台除外)
        /// </summary>
        private void HideAllUI()
        {
            foreach (var panel in PanelMap.Values)
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
        internal void SetUIState(UIType state, bool show)
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
                    case UIType.None:
                        HideAllUI();
                        break;
                    case UIType.MainMenu:
                        SetUIState(UIType.SettingsPanel, false);
                        SetUIState(UIType.AboutPanel, false);
                        SetUIState(UIType.SaveLoadMenu, false);
                        break;
                    case UIType.PauseMenu:
                        SetUIState(UIType.ResultPanel, false);
                        break;
                    case UIType.ResultPanel:
                        SetUIState(UIType.PauseMenu, false);
                        SetUIState(UIType.HUD, false);
                        break;
                    case UIType.Inventory:
                        SetUIState(UIType.PauseMenu, false);
                        SetUIState(UIType.ResultPanel, false);
                        break;
                    case UIType.SettingsPanel:
                        SetUIState(UIType.AboutPanel, false);
                        SetUIState(UIType.SaveLoadMenu, false);
                        break;
                    case UIType.AboutPanel:
                        SetUIState(UIType.SettingsPanel, false);
                        SetUIState(UIType.SaveLoadMenu, false);
                        break;
                    case UIType.SaveLoadMenu:
                        SetUIState(UIType.AboutPanel, false);
                        SetUIState(UIType.SettingsPanel, false);
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
                    if (currentState != UIType.MainMenu && currentState != UIType.SaveLoadMenu && 
                        currentState != UIType.SettingsPanel && currentState != UIType.AboutPanel)
                    {
                        SetUIState(UIType.HUD, true);
                    }
                }
            }

            // 更新当前状态
            if (show) currentState = state;
            else if (currentState == state) currentState = UIType.None;

            // 根据状态显示/隐藏对应UI
            Log.Info(module, "尝试显示/隐藏UI类型: " + state + ", show: " + show);
            
            if (PanelMap.TryGetValue(state, out var panel))
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
            else if (show)
            {
                // 如果面板不存在且请求显示，则尝试自动加载
                Log.Warning(module, "未找到面板类型: " + state + ", 正在尝试自动加载");
                StartCoroutine(LoadPanelAutomatically(state));
            }
            else
            {
                Log.Error(module, "未找到对应UI类型的面板: " + state);
            }
        }
        
        /// <summary>
        /// 自动加载面板的协程
        /// 解决动态面板注册时机问题，确保在访问面板前已完成加载和注册
        /// </summary>
        /// <param name="panelType">需要加载的面板类型</param>
        private IEnumerator LoadPanelAutomatically(UIType panelType)
        {
            Log.Info(module, "开始自动加载面板: " + panelType);
            
            // 使用PanelLoader异步加载面板
            Task<bool> loadTask = PanelLoader.Instance.LoadPanelAsync(panelType);
            
            // 等待加载完成
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }
            
            // 如果加载成功，则显示面板
            if (loadTask.Result && PanelMap.TryGetValue(panelType, out var panel))
            {
                Log.Info(module, "面板 " + panelType + " 自动加载成功并显示");
                panel.Show();
            }
            else
            {
                Log.Error(module, "面板 " + panelType + " 自动加载失败");
            }
        }
        #endregion

        #region UI事件响应
        private void OnMenuShow(UIType state, bool show)
        {
            SetUIState(state, show);
        }

        /// <summary>
        /// 场景加载开始时显示加载界面
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        private void ShowLoading(string sceneName)
        {
            Log.Info(module, "场景加载开始，显示加载界面");
            SetUIState(UIType.Loading, true);
            // 记录加载界面显示的开始时间
            m_loadingStartTime = Time.unscaledTime;
        }

        /// <summary>
        /// 场景加载完成时隐藏加载界面
        /// </summary>
        /// <param name="sceneName">已加载完成的场景名称</param>
        private void HideLoading(string sceneName)
        {
            Log.Info(module, "场景加载完成，开始隐藏加载界面");
            
            // 检查Loading面板是否已经注册，如果没有则等待并延迟隐藏
            if (!PanelMap.ContainsKey(UIType.Loading))
            {
                Log.Info(module, "Loading面板尚未加载完成，将延迟隐藏");
                StartCoroutine(WaitAndHideLoading());
            }
            else
            {
                // 计算加载界面已经显示的时间
                float loadingDisplayedTime = Time.unscaledTime - m_loadingStartTime;
                
                // 如果加载界面显示时间不足最小显示时间，则等待足够的时间再隐藏
                if (loadingDisplayedTime < m_minLoadingDisplayTime)
                {
                    Log.Info(module, "加载界面显示时间不足，将等待至最小显示时间后隐藏");
                    StartCoroutine(WaitForMinLoadingTime());
                }
                else
                {
                    // 如果面板已注册且显示时间足够，则直接隐藏
                    SetUIState(UIType.Loading, false);
                }
            }
        }
        
        /// <summary>
        /// 等待加载界面达到最小显示时间后再隐藏的协程
        /// 确保加载界面不会因场景加载过快而一闪而过
        /// </summary>
        private IEnumerator WaitForMinLoadingTime()
        {
            // 计算还需要等待的时间
            float waitTime = m_minLoadingDisplayTime - (Time.unscaledTime - m_loadingStartTime);
            
            // 确保等待时间为正数
            waitTime = Mathf.Max(0f, waitTime);
            
            Log.Info(module, "等待加载界面最小显示时间，还需等待: " + waitTime.ToString("F2") + "秒");
            
            // 等待剩余时间
            float elapsedTime = 0f;
            while (elapsedTime < waitTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            
            // 等待完成后隐藏加载界面
            Log.Info(module, "加载界面已达到最小显示时间，现在隐藏它");
            SetUIState(UIType.Loading, false);
        }
        
        /// <summary>
        /// 等待Loading面板加载完成后再隐藏的协程
        /// 解决场景加载完成时面板可能尚未加载完成的时序问题
        /// </summary>
        private IEnumerator WaitAndHideLoading()
        {
            // 等待直到面板被注册或超时（最多等待2秒）
            float timeout = 2f;
            float elapsedTime = 0f;
            
            while (!PanelMap.ContainsKey(UIType.Loading) && elapsedTime < timeout)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 如果面板已加载完成
            if (PanelMap.ContainsKey(UIType.Loading))
            {
                Log.Info(module, "Loading面板已加载完成，现在检查显示时间");
                
                // 计算加载界面已经显示的时间
                float loadingDisplayedTime = Time.unscaledTime - m_loadingStartTime;
                
                // 如果加载界面显示时间不足最小显示时间，则等待足够的时间再隐藏
                if (loadingDisplayedTime < m_minLoadingDisplayTime)
                {
                    Log.Info(module, "加载界面显示时间不足，将等待至最小显示时间后隐藏");
                    StartCoroutine(WaitForMinLoadingTime());
                }
                else
                {
                    // 如果显示时间足够，则直接隐藏
                    Log.Info(module, "加载界面显示时间足够，现在隐藏它");
                    SetUIState(UIType.Loading, false);
                }
            }
            else
            {
                Log.Warning(module, "等待Loading面板超时，无法隐藏");
            }
        }
        #endregion

 
    }
}