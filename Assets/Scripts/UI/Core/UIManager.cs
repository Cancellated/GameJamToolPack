using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logger;
using MyGame.Events;
using MyGame.Managers;
using MyGame.UI.Core;
using MyGame.UI.Config;
using UnityEngine;

namespace MyGame.UI
{
    /// <summary>
    /// 全局UI管理器，负责调度和管理所有UI界面。
    ///
    /// 【职责范围】（重构后收敛为两点）
    ///   1. 面板注册：唯一缓存 PanelMap 的维护（场景面板优先 → Addressable 配置加载 → 懒加载）；
    ///   2. 面板显隐调度：SetUIState 依据 UIConfig 中的互斥关系与输入模式数据化驱动。
    ///
    /// 【已拆分的职责】
    ///   - 场景加载时序编排（显示 Loading → 等待就绪 → 隐藏 Loading）→ SceneLoadingController；
    ///   - 面板资源的实际加载/实例化 → PanelLoader。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private const string LOG_MODULE = LogModules.UIMANAGER;

        #region 配置与状态

        /// <summary>
        /// 面板配置资产访问入口（单一配置源）。
        /// UIConfig 由 PanelLoader 独占持有并负责加载（通过 Addressable 按 Config/UIConfig 地址异步加载），
        /// UIManager 仅读取该配置以驱动面板注册、互斥关系与输入模式，避免双份配置源
        /// </summary>
        private UIConfig PanelConfig
        {
            get { return PanelLoader.Instance != null ? PanelLoader.Instance.Config : null; }
        }

        /// <summary>
        /// 当前 UI 状态
        /// </summary>
        public UIType currentState = UIType.None;

        /// <summary>
        /// 面板类型到面板实例的映射（唯一面板缓存）。
        /// 移除了 uiPanels 重复缓存，所有读写统一走此字典
        /// </summary>
        public Dictionary<UIType, IUIPanel> PanelMap;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            // 初始化面板映射字典（唯一缓存）
            PanelMap = new Dictionary<UIType, IUIPanel>();

            // 注册UI相关事件监听
            GameEvents.OnMenuShow += OnMenuShow;              // UI显隐处理
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete; // 场景切换后刷新面板映射

            // 自举创建场景加载编排控制器（Singleton 懒创建），
            // Loading 界面的显隐时序已从本类拆出，由 SceneLoadingController 负责
            if (SceneLoadingController.Instance == null)
            {
                Log.Warning(LOG_MODULE, "SceneLoadingController 自举创建失败");
            }
        }

        /// <summary>
        /// 开始时初始化面板映射（移至此确保其他组件已就绪）。
        /// 面板配置资产由 PanelLoader 通过 Addressable 异步加载，先等待其就绪；
        /// 配置未就绪时 InitializePanelMap 仅能注册场景中的面板
        /// </summary>
        private async void Start()
        {
            await PanelLoader.Instance.EnsureConfigLoadedAsync();

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

        protected override void OnDestroy()
        {
            // 注销事件监听
            GameEvents.OnMenuShow -= OnMenuShow;
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;

            // 通知基类置位销毁标志，禁止后续 Instance 懒创建
            base.OnDestroy();
        }

        #endregion

        #region 面板注册（单一注册源：场景面板优先 → Addressable 配置加载 → 懒加载）

        /// <summary>
        /// 初始化面板映射字典。
        /// 注册来源（仅两种）：
        ///   1. 场景中已激活的面板（兼容现状：场景预置的 HUD/PauseMenu/Loading 直接注册）；
        ///   2. UIConfig 配置清单：场景中没有的非懒加载面板经 PanelLoader 按 Addressable 地址
        ///      异步加载并实例化到全局 UI 容器（prefab 字段已弃用，地址为唯一来源）。
        /// </summary>
        private void InitializePanelMap()
        {
            Log.Info(LOG_MODULE, "开始初始化面板映射");

            // 清空现有映射
            PanelMap.Clear();

            // 1. 注册场景中已激活的面板，避免"场景里已有面板却仍被自动加载"
            RegisterPanelsInScene();

            // 2. 配置清单：场景中没有的面板按配置实例化（懒加载面板首次显示时才实例化）
            if (PanelConfig != null)
            {
                foreach (var entry in PanelConfig.Entries)
                {
                    if (entry == null || entry.panelType == UIType.None)
                    {
                        continue;
                    }
                    if (PanelMap.ContainsKey(entry.panelType))
                    {
                        continue; // 场景面板优先
                    }
                    if (entry.lazyLoad)
                    {
                        continue; // 懒加载：首次显示时再实例化
                    }
                    // 统一经 PanelLoader 按 Addressable 地址异步加载
                    if (!string.IsNullOrEmpty(entry.addressableAddress))
                    {
                        _ = PanelLoader.Instance.LoadPanelAsync(entry.panelType);
                    }
                    else
                    {
                        Log.Warning(LOG_MODULE, $"面板 {entry.panelType} 未配置 Addressable 地址，跳过");
                    }
                }
            }
            else
            {
                Log.Warning(LOG_MODULE, "未挂载 UIConfig 配置资产，仅注册场景中的面板");
            }

            Log.Info(LOG_MODULE, "面板映射初始化完成，共包含 " + PanelMap.Count + " 个面板类型");
        }

        /// <summary>
        /// 扫描场景中已激活的 MonoBehaviour，将实现 IUIPanel 接口的组件注册进映射。
        /// 场景预置的面板默认保持物体激活（显隐由 Show/Hide 控制），因此只需注册激活实例
        /// </summary>
        private void RegisterPanelsInScene()
        {
            int registered = 0;
            foreach (var mono in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (mono is IUIPanel panel)
                {
                    AddPanelToMap(panel);
                    registered++;
                }
            }
            if (registered > 0)
            {
                Log.Info(LOG_MODULE, $"扫描到并注册了 {registered} 个场景内面板");
            }
        }

        /// <summary>
        /// 将面板添加到映射字典（唯一缓存）。
        /// 面板初始化由 BaseView.OnEnable 防重入触发，此处不再调用 panel.Initialize()
        /// </summary>
        /// <param name="panel">要添加的面板</param>
        private void AddPanelToMap(IUIPanel panel)
        {
            if (PanelMap.ContainsKey(panel.PanelType))
            {
                Log.Info(LOG_MODULE, "面板类型已存在于映射中: " + panel.PanelType + " (" + panel.GetType().Name + ")");
                return;
            }

            PanelMap.Add(panel.PanelType, panel);
        }

        #endregion

        #region 动态面板管理

        /// <summary>
        /// 动态注册UI面板到管理器（PanelLoader 加载完成后调用）
        /// </summary>
        /// <param name="panel">要注册的IUIPanel接口实现</param>
        /// <returns>注册是否成功</returns>
        public bool RegisterUIPanel(IUIPanel panel)
        {
            if (panel == null)
            {
                Log.Error(LOG_MODULE, "尝试注册空面板");
                return false;
            }

            if (!PanelMap.ContainsKey(panel.PanelType))
            {
                AddPanelToMap(panel);
                Log.Info(LOG_MODULE, "成功动态注册面板: " + panel.PanelType + " (" + panel.GetType().Name + ")");
                return true;
            }

            Log.Info(LOG_MODULE, "面板类型已存在于映射中，跳过注册: " + panel.PanelType);
            return false;
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
                panel.Cleanup();
                Log.Info(LOG_MODULE, "成功注销面板: " + panelType);
                return true;
            }

            Log.Warning(LOG_MODULE, "未找到要注销的面板类型: " + panelType);
            return false;
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
        /// 只切换指定面板 CanvasGroup 的可交互性，不改变其显隐。
        /// 用于子界面打开时保留下层面板作为背景但禁止点击。
        /// </summary>
        public void SetPanelInteractable(UIType panelType, bool interactable)
        {
            if (PanelMap.TryGetValue(panelType, out IUIPanel panel) && panel != null)
            {
                panel.SetInteractable(interactable);
            }
        }

        /// <summary>
        /// 设置UI状态并处理互斥关系（互斥名单由 UIConfig 数据化驱动，替代原 switch 特判）
        /// </summary>
        internal void SetUIState(UIType state, bool show)
        {
            if (show)
            {
                // 互斥关系（数据化）：显示本面板时隐藏配置中与其互斥的面板。
                // 注意：必须走静默隐藏——若直接 SetUIState(hideType, false)，
                // 会触发下方"关闭独占面板后恢复 HUD/输入模式"分支，
                // 导致刚被隐藏的 HUD 又被恢复显示（如显示结算面板时）
                if (PanelConfig != null)
                {
                    foreach (var hideType in PanelConfig.GetHideOnShow(state))
                    {
                        if (hideType != state)
                        {
                            HidePanelSilently(hideType);
                        }
                    }
                }

                // 输入模式（数据化）：由配置决定显示本面板时切换到的输入状态
                ApplyInputMode(state);
            }
            // 关闭当前独占面板（非 Loading/Console）时，切回玩法模式并恢复 HUD
            else if (currentState == state && currentState != UIType.None && currentState != UIType.Loading && currentState != UIType.Console)
            {
                if (InputManager.Instance != null)
                {
                    InputManager.Instance.SwitchToGamePlayMode();
                    // 关闭菜单类面板后恢复 HUD（主菜单/存档/设置/关于除外，它们属于非游戏场景 UI）
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

            Log.Info(LOG_MODULE, "尝试显示/隐藏UI类型: " + state + ", show: " + show);

            if (PanelMap.TryGetValue(state, out var panel))
            {
                // 防御：面板引用已被销毁（场景实例随旧场景卸载）时移除失效引用
                if (panel is MonoBehaviour mono && mono == null)
                {
                    PanelMap.Remove(state);
                    Log.Warning(LOG_MODULE, "面板引用已销毁，从映射中移除: " + state);
                    if (show)
                    {
                        Log.Warning(LOG_MODULE, "未找到面板类型: " + state + ", 正在尝试自动加载");
                        StartCoroutine(LoadPanelAutomatically(state));
                    }
                    return;
                }

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
                // 面板缺失且请求显示：尝试自动加载（Addressable 加载，prefab 字段为已弃用的兼容分支）
                Log.Warning(LOG_MODULE, "未找到面板类型: " + state + ", 正在尝试自动加载");
                StartCoroutine(LoadPanelAutomatically(state));
            }
            else
            {
                Log.Error(LOG_MODULE, "未找到对应UI类型的面板: " + state);
            }
        }

        /// <summary>
        /// 静默隐藏指定面板：仅隐藏面板本身并同步 currentState，
        /// 不触发"关闭独占面板后恢复 HUD/输入模式"逻辑。
        /// 用于显示其他面板时的互斥隐藏——此时被隐藏面板之前的 UI 状态不应被恢复
        /// </summary>
        /// <param name="state">要静默隐藏的面板类型</param>
        private void HidePanelSilently(UIType state)
        {
            if (PanelMap.TryGetValue(state, out var panel))
            {
                panel.Hide();
            }
            if (currentState == state)
            {
                currentState = UIType.None;
            }
        }

        /// <summary>
        /// 依据配置应用显示面板时应切换的输入模式
        /// </summary>
        private void ApplyInputMode(UIType state)
        {
            if (InputManager.Instance == null || PanelConfig == null)
            {
                return;
            }

            switch (PanelConfig.GetInputMode(state))
            {
                case PanelInputMode.UI:
                    InputManager.Instance.SwitchToUIMode();
                    break;
                case PanelInputMode.Gameplay:
                    InputManager.Instance.SwitchToGamePlayMode();
                    break;
                case PanelInputMode.None:
                    // 瞬态面板（Loading/Console）不切换输入模式
                    break;
            }
        }

        /// <summary>
        /// 自动加载面板的协程（面板缺失且请求显示时触发）。
        /// 主要路径：经 PanelLoader 按 Addressable 地址异步加载（prefab 字段已弃用，仅作兼容分支保留）。
        /// 覆盖场景：懒加载面板首次显示、面板被 UnloadPanel 释放后的再次显示。
        /// </summary>
        /// <param name="panelType">需要加载的面板类型</param>
        private IEnumerator LoadPanelAutomatically(UIType panelType)
        {
            Log.Info(LOG_MODULE, "开始自动加载面板: " + panelType);

            // Addressable 异步加载
            Task<bool> loadTask = PanelLoader.Instance.LoadPanelAsync(panelType);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.Result && PanelMap.TryGetValue(panelType, out var loadedPanel))
            {
                Log.Info(LOG_MODULE, "面板 " + panelType + " 自动加载成功并显示");
                loadedPanel.Show();
            }
            else
            {
                Log.Error(LOG_MODULE, "面板 " + panelType + " 自动加载失败");
            }
        }

        #endregion

        #region 事件响应

        private void OnMenuShow(UIType state, bool show)
        {
            SetUIState(state, show);
        }

        /// <summary>
        /// 场景切换完成后刷新面板映射：
        ///   1. 清理随旧场景销毁的面板引用（防止 missing 引用残留）；
        ///   2. 注册新场景中激活的面板；
        ///   3. 补注册配置中缺失的非懒加载面板（全局常驻面板不随场景销毁）。
        /// </summary>
        private void OnSceneLoadComplete(string sceneName)
        {
            RefreshPanelMap();
        }

        /// <summary>
        /// 刷新面板映射：场景切换后调用
        /// </summary>
        private void RefreshPanelMap()
        {
            Log.Info(LOG_MODULE, "场景切换完成，刷新面板映射");

            // 1. 清理已销毁的面板引用（旧场景实例已随场景卸载失效）
            var destroyedTypes = new List<UIType>();
            foreach (var kvp in PanelMap)
            {
                if (kvp.Value is MonoBehaviour mono && mono == null)
                {
                    destroyedTypes.Add(kvp.Key);
                }
            }
            foreach (var type in destroyedTypes)
            {
                PanelMap.Remove(type);
                Log.Info(LOG_MODULE, "清理已销毁的面板引用: " + type);
            }

            // 2. 注册新场景中激活的面板（PanelMap 已有类型自动跳过，不会重复初始化）
            RegisterPanelsInScene();

            // 3. 补注册配置中缺失的非懒加载面板
            if (PanelConfig != null)
            {
                foreach (var entry in PanelConfig.Entries)
                {
                    if (entry == null || entry.panelType == UIType.None)
                    {
                        continue;
                    }
                    if (PanelMap.ContainsKey(entry.panelType) || entry.lazyLoad)
                    {
                        continue;
                    }
                    // 统一经 PanelLoader 按 Addressable 地址异步加载
                    if (!string.IsNullOrEmpty(entry.addressableAddress))
                    {
                        _ = PanelLoader.Instance.LoadPanelAsync(entry.panelType);
                    }
                }
            }
        }

        #endregion
    }
}
