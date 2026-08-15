using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Logger;
using MyGame.Managers;
using MyGame.UI;
using MyGame.UI.Config;
using UnityEngine.UI;

namespace MyGame.UI.Core
{
    /// <summary>
    /// 面板资源管理器，负责UI面板资源的加载、实例化和卸载
    /// </summary>
    public class PanelLoader : Singleton<PanelLoader>
    {
        private const string module = LogModules.UIMANAGER;
        
        // 存储已加载的面板实例
        private Dictionary<UIType, IUIPanel> _loadedPanels = new();
        
        // 存储面板预制体地址映射
        private Dictionary<UIType, string> _panelAddressMap = new();
        
        // 存储异步加载操作的句柄，用于后续释放资源
        private Dictionary<UIType, AsyncOperationHandle<GameObject>> _loadHandles = new();
        
        // UI面板的父级容器
        private Transform _canvasTransform;

        /// <summary>
        /// UIConfig 资产的 Addressable 地址（与 Config 分组中 UIConfig.asset 的地址一致）
        /// </summary>
        private const string UICONFIG_ADDRESS = "Config/UIConfig";

        [Header("配置文件")]
        [Tooltip("面板配置资产（可选）：未拖入时通过 Addressable 按地址异步加载")]
        [SerializeField]
        private UIConfig _panelConfig;

        /// <summary>
        /// 配置异步加载任务（幂等缓存，防止重复触发 Addressable 加载）
        /// </summary>
        private Task<UIConfig> _configLoadTask;

        /// <summary>
        /// 面板配置资产访问入口（全项目唯一持有者）。
        /// 已就绪时返回缓存实例；未就绪返回 null，请先调用 EnsureConfigLoadedAsync 等待加载完成。
        /// UIManager 等其他模块均通过本入口读取，避免双份配置源
        /// </summary>
        public UIConfig Config
        {
            get { return _panelConfig; }
            set { _panelConfig = value; }
        }

        /// <summary>
        /// 确保面板配置资产已就绪（幂等）：
        /// Inspector 已拖入直接返回；否则通过 Addressable 按 UICONFIG_ADDRESS 异步加载并缓存。
        /// 加载完成后自动初始化面板地址映射
        /// </summary>
        public Task<UIConfig> EnsureConfigLoadedAsync()
        {
            if (_panelConfig != null)
            {
                return Task.FromResult(_panelConfig);
            }
            if (_configLoadTask == null)
            {
                _configLoadTask = LoadConfigAsync();
            }
            return _configLoadTask;
        }

        /// <summary>
        /// 通过 Addressable 异步加载 UIConfig 资产并缓存（仅触发一次）
        /// </summary>
        private async Task<UIConfig> LoadConfigAsync()
        {
            AsyncOperationHandle<UIConfig> handle = Addressables.LoadAssetAsync<UIConfig>(UICONFIG_ADDRESS);
            _panelConfig = await handle.Task;

            if (_panelConfig != null)
            {
                // 面板地址映射依赖配置内容，配置就绪后再初始化
                InitializePanelAddressMap();
                Log.Info(module, $"已通过 Addressable 加载面板配置资产: {UICONFIG_ADDRESS}");
            }
            else
            {
                Log.Error(module, $"通过 Addressable 加载面板配置失败: {UICONFIG_ADDRESS}，请检查 Config 分组配置");
            }
            return _panelConfig;
        }
        
        /// <summary>
        /// 向GameObject添加并配置CanvasScaler组件
        /// 统一配置CanvasScaler参数，避免重复代码
        /// </summary>
        /// <param name="gameObject">要添加CanvasScaler的GameObject</param>
        /// <returns>配置好的CanvasScaler组件</returns>
        private CanvasScaler AddAndConfigureCanvasScaler(GameObject gameObject)
        {
            CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasScaler.referencePixelsPerUnit = 100;
            return canvasScaler;
        }

        /// <summary>
        /// 更新Canvas引用，可以指定是使用全局Canvas还是场景特定Canvas
        /// </summary>
        /// <param name="useGlobalCanvas">是否使用全局Canvas</param>
        /// <param name="canvasName">如果不使用全局Canvas，指定要查找的Canvas名称</param>
        private void UpdateCanvasReference(bool useGlobalCanvas = true, string canvasName = "UI")
        {
            Canvas canvas;
            
            if (useGlobalCanvas)
            {
                // 查找或创建全局Canvas
                GameObject globalCanvasObj = GameObject.Find("GlobalUI");
                if (globalCanvasObj == null)
                {
                    // 如果没有找到全局Canvas，则创建一个新的
                    globalCanvasObj = new GameObject("GlobalUI");
                    // 标记为全局不销毁对象
                    DontDestroyOnLoad(globalCanvasObj);
                    Log.Info(module, "创建了新的全局Canvas并设置为DontDestroyOnLoad");
                    
                    canvas = globalCanvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    
                    // 添加并配置CanvasScaler组件
                    AddAndConfigureCanvasScaler(globalCanvasObj);
                    
                    globalCanvasObj.AddComponent<GraphicRaycaster>();
                }
                else
                {
                    // 如果找到全局Canvas，获取其Canvas组件
                    canvas = globalCanvasObj.GetComponent<Canvas>();
                    
                    // 确保已存在的GlobalUI也被设置为DontDestroyOnLoad
                    if (globalCanvasObj.scene.buildIndex == -1)
                    {
                        Log.Info(module, "检测到全局Canvas已存在并已设置为DontDestroyOnLoad");
                    }
                    else
                    {
                        DontDestroyOnLoad(globalCanvasObj);
                        Log.Info(module, "已将全局Canvas设置为DontDestroyOnLoad");
                    }
                }
                Log.Info(module, "已更新全局Canvas引用");
            }
            else
            {
                // 查找场景特定的Canvas
                GameObject sceneCanvasObj = GameObject.Find(canvasName);
                if (sceneCanvasObj != null)
                {
                    canvas = sceneCanvasObj.GetComponent<Canvas>();
                    Log.Info(module, $"已找到场景特定Canvas: {canvasName}");
                }
                else
                {
                    // 如果没有找到场景Canvas，则创建一个新的场景Canvas
                    sceneCanvasObj = new GameObject(canvasName);
                    canvas = sceneCanvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    
                    // 添加并配置CanvasScaler组件
                    AddAndConfigureCanvasScaler(sceneCanvasObj);
                    
                    sceneCanvasObj.AddComponent<GraphicRaycaster>();
                    Log.Info(module, $"创建了新的场景特定Canvas: {canvasName}");
                }
            }
            
            _canvasTransform = canvas.transform;
        }


        protected override void Awake()
        {
            base.Awake();
            
            // 查找或创建Canvas作为所有UI面板的父容器
            // 在初始化时使用全局Canvas
            UpdateCanvasReference(true, "GlobalUI");
            
            // 面板地址映射依赖 UIConfig 配置内容，
            // 而配置由 Addressable 异步加载，故在 LoadConfigAsync 完成后初始化（见 EnsureConfigLoadedAsync）
        }
        
        /// <summary>
        /// 初始化面板类型与Addressable地址的映射
        /// 从 UIConfig 面板配置资产收集 addressableAddress 字段（面板资源的唯一来源）
        /// </summary>
        private void InitializePanelAddressMap()
        {
            // 清空现有的映射
            _panelAddressMap.Clear();

            // 从面板配置资产收集 Addressable 地址
            if (Config != null)
            {
                foreach (var entry in Config.Entries)
                {
                    if (entry == null || entry.panelType == UIType.None || string.IsNullOrEmpty(entry.addressableAddress))
                    {
                        continue;
                    }
                    _panelAddressMap[entry.panelType] = entry.addressableAddress;
                    Log.Info(module, $"从面板配置加载地址映射: {entry.panelType} -> {entry.addressableAddress}");
                }
            }
            else
            {
                Log.Warning(module, "UIConfig 未设置，面板地址映射为空");
            }
        }
        
        /// <summary>
        /// 设置UI面板的排序层级
        /// 确保重要的UI元素（如加载界面和控制台）显示在正确的层级
        /// </summary>
        /// <param name="panelType">面板类型</param>
        /// <param name="panelTransform">面板的Transform组件</param>
        private void SetPanelSortingOrder(UIType panelType, Transform panelTransform)
        {

            // 根据面板类型设置排序层级
            int sortingOrder = panelType switch
            {
                UIType.Loading => 1000,// 加载界面应该在最顶层
                UIType.Console => 900,// 调试控制台也应该在较高层级，但比加载界面低
                _ => 10,// 普通UI面板使用默认层级
            };

            // 创建或获取Sorting Group组件来管理排序
            if (!panelTransform.TryGetComponent<Canvas>(out var canvas))
            {
                canvas = panelTransform.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
            }
            
            canvas.sortingOrder = sortingOrder;
            Log.Info(module, $"为面板 {panelType} 设置了排序层级 {sortingOrder}");
        }
        
        /// <summary>
        /// 异步加载面板资源并实例化
        /// 加载完成后自动注册到UIManager，但不自动显示
        /// </summary>
        /// <param name="panelType">面板类型</param>
        /// <param name="panelAddress">可选：Addressable资源地址，如果未提供则使用映射表中的地址</param>
        /// <param name="useGlobalCanvas">可选：是否使用全局Canvas</param>
        /// <param name="canvasName">可选：如果不使用全局Canvas，指定要查找的Canvas名称</param>
        /// <returns>加载操作的Task</returns>
        public async Task<bool> LoadPanelAsync(UIType panelType, string panelAddress = null, bool useGlobalCanvas = true, string canvasName = "UI")
        {
            // 检查面板是否已经加载
            if (_loadedPanels.ContainsKey(panelType))
            {
                Log.Info(module, $"面板 {panelType} 已加载");
                return true;
            }
            
            // 获取面板地址
            string address = panelAddress;
            if (string.IsNullOrEmpty(address))
            {
                // 首先尝试从内存中的映射表获取
                if (!_panelAddressMap.TryGetValue(panelType, out address))
                {
                    // 内存映射表中没有时，直接从面板配置资产获取地址
                    if (Config != null)
                    {
                        address = Config.GetAddressableAddress(panelType);
                    }
                    
                    if (string.IsNullOrEmpty(address))
                    {
                        Log.Error(module, $"未找到面板类型 {panelType} 的Addressable地址");
                        return false;
                    }
                }
            }
            
            try
            {
                // 异步加载面板预制体
                AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(address);
                _loadHandles[panelType] = loadHandle;
                
                // 等待加载完成
                await loadHandle.Task;
                
                if (loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null)
                {
                    // 只在使用场景特定Canvas时更新引用
                    // 全局Canvas已在初始化时创建，无需重复更新
                    if (!useGlobalCanvas)
                    {
                        UpdateCanvasReference(false, canvasName);
                    }
                    
                    // 实例化面板
                    GameObject panelObj = Instantiate(loadHandle.Result, _canvasTransform);
                    panelObj.name = $"{panelType}Panel";
                    
                    // 设置面板的排序层级，确保重要面板在正确层级显示
                    SetPanelSortingOrder(panelType, panelObj.transform);
                    
                    // 获取IUIPanel组件
                    if (panelObj.TryGetComponent<IUIPanel>(out IUIPanel panel))
                    {
                        // 注册到UIManager
                        if (UIManager.Instance.RegisterUIPanel(panel))
                        {
                            _loadedPanels[panelType] = panel;
                            
                            Log.Info(module, $"成功加载并注册面板: {panelType}");
                            return true;
                        }
                        else
                        {
                            // 面板已在场景中注册，跳过重复加载
                            Log.Info(module, $"面板 {panelType} 已存在，跳过重复加载");
                            Destroy(panelObj);
                            return true;
                        }
                    }
                    else
                    {
                        Log.Error(module, $"面板预制体 {address} 未实现IUIPanel接口");
                        Destroy(panelObj);
                        return false;
                    }
                }
                else
                {
                    Log.Error(module, $"面板 {panelType} 加载失败，状态: {loadHandle.Status}");
                    if (_loadHandles.ContainsKey(panelType))
                    {
                        _loadHandles.Remove(panelType);
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Error(module, $"加载面板 {panelType} 时发生异常: {e.Message}");
                if (_loadHandles.ContainsKey(panelType))
                {
                    _loadHandles.Remove(panelType);
                }
                return false;
            }
        }
        
        /// <summary>
        /// 卸载指定类型的UI面板资源
        /// 仅负责资源卸载，不直接控制面板显隐
        /// </summary>
        /// <param name="panelType">面板类型</param>
        /// <param name="unloadAsset">是否同时卸载预制体资源</param>
        public void UnloadPanel(UIType panelType, bool unloadAsset = true)
        {
            // 从UIManager中注销
            if (UIManager.Instance.UnregisterUIPanel(panelType))
            {
                // 从已加载面板列表中移除
                if (_loadedPanels.TryGetValue(panelType, out IUIPanel panel))
                {
                    // 获取面板的GameObject并销毁
                    MonoBehaviour panelMono = panel as MonoBehaviour;
                    GameObject panelObj = panelMono != null ? panelMono.gameObject : null;
                    if (panelObj != null)
                    {
                        Destroy(panelObj);
                    }
                    
                    _loadedPanels.Remove(panelType);
                }
                
                // 释放预制体资源
                if (unloadAsset && _loadHandles.TryGetValue(panelType, out AsyncOperationHandle<GameObject> loadHandle))
                {
                    if (loadHandle.IsValid())
                    {
                        Addressables.Release(loadHandle);
                    }
                    _loadHandles.Remove(panelType);
                }
                
                Log.Info(module, $"成功卸载面板资源: {panelType}");
            }
        }
        
    }
}
