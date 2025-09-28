using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Logger;
using MyGame.Managers;
using MyGame.UI;
using MyGame.Events;

using MyGame.UI.Core.Config;
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
        
        [Header("配置文件")]
        [Tooltip("面板地址配置文件，定义UI面板类型与Addressable资源地址的映射关系")]
        [SerializeField]
        private PanelAddressConfig _panelAddressConfig;
        
        /// <summary>
        /// 面板地址配置文件
        /// </summary>
        public PanelAddressConfig Config
        {
            get { return _panelAddressConfig; }
            set { _panelAddressConfig = value; }
        }
        
        /// <summary>
        /// 更新Canvas引用，确保使用的是当前场景中的Canvas
        /// </summary>
        private void UpdateCanvasReference()
        {
            // 查找场景中现有的Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // 如果没有找到Canvas，则创建一个新的
            GameObject canvasObj = new("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // 添加并配置CanvasScaler组件，与场景中设置的配置保持一致
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            Log.Info(module, "创建了新的Canvas作为UI面板的父容器，并配置了CanvasScaler以匹配场景中的设置");
            }
            _canvasTransform = canvas.transform;
            Log.Info(module, "已更新Canvas引用");
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            // 查找或创建Canvas作为所有UI面板的父容器
            UpdateCanvasReference();
            
            // 初始化面板地址映射
            InitializePanelAddressMap();
        }
        
        /// <summary>
        /// 初始化面板类型与Addressable地址的映射
        /// 从配置文件加载面板地址映射信息
        /// </summary>
        private void InitializePanelAddressMap()
        {
            // 清空现有的映射
            _panelAddressMap.Clear();
            
            // 如果配置文件存在，则加载映射信息
            if (_panelAddressConfig != null)
            {
                Dictionary<UIType, string> configMap = _panelAddressConfig.GetAddressMap();
                foreach (var kvp in configMap)
                {
                    _panelAddressMap[kvp.Key] = kvp.Value;
                    Log.Info(module, $"从配置文件加载面板地址映射: {kvp.Key} -> {kvp.Value}");
                }
            }
            else
            {
                Log.Warning(module, "PanelAddressConfig未设置");
            }
        }
        
        /// <summary>
        /// 重新加载面板地址映射
        /// 可用于运行时更新配置
        /// </summary>
        public void ReloadPanelAddressMap()
        {
            InitializePanelAddressMap();
            Log.Info(module, "已重新加载面板地址映射");
        }
        
        /// <summary>
        /// 注册面板类型与Addressable地址的映射关系
        /// </summary>
        /// <param name="panelType">面板类型</param>
        /// <param name="addressableAddress">Addressable资源地址</param>
        public void RegisterPanelAddress(UIType panelType, string addressableAddress)
        {
            if (!_panelAddressMap.ContainsKey(panelType))
            {
                _panelAddressMap.Add(panelType, addressableAddress);
                Log.Info(module, $"注册面板地址映射: {panelType} -> {addressableAddress}");
            }
            else
            {
                Log.Warning(module, $"面板类型 {panelType} 的地址映射已存在，将被覆盖");
                _panelAddressMap[panelType] = addressableAddress;
            }
        }
        
        /// <summary>
        /// 异步加载面板资源并实例化
        /// 加载完成后自动注册到UIManager，但不自动显示
        /// </summary>
        /// <param name="panelType">面板类型</param>
        /// <param name="panelAddress">可选：Addressable资源地址，如果未提供则使用映射表中的地址</param>
        /// <returns>加载操作的Task</returns>
        public async Task<bool> LoadPanelAsync(UIType panelType, string panelAddress = null)
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
                    // 如果内存映射表中没有，且配置文件存在，尝试直接从配置文件获取
                    if (_panelAddressConfig != null)
                    {
                        address = _panelAddressConfig.GetAddressForPanel(panelType);
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
                    // 确保使用的是当前场景中的Canvas
                    UpdateCanvasReference();
                    
                    // 实例化面板
                    GameObject panelObj = Instantiate(loadHandle.Result, _canvasTransform);
                    panelObj.name = $"{panelType}Panel";
                    
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
                            Log.Error(module, $"面板 {panelType} 注册到UIManager失败");
                            Destroy(panelObj);
                            return false;
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
        
        /// <summary>
        /// 检查指定类型的面板是否已加载
        /// </summary>
        /// <param name="panelType">面板类型</param>
        /// <returns>是否已加载</returns>
        public bool IsPanelLoaded(UIType panelType)
        {
            return _loadedPanels.ContainsKey(panelType);
        }
    }
}