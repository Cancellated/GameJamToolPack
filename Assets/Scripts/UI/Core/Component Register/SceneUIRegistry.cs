using System.Collections.Generic;
using MyGame.Managers;
using UnityEngine;
using Logger;
using static Logger.LogModules;
using MyGame.Events;

namespace MyGame.UI
{
    /// <summary>
    /// 场景UI注册表管理器，负责管理场景与UI面板的绑定关系
    /// </summary>
    public class SceneUIRegistry : Singleton<SceneUIRegistry>
    {
        #region 字段
        
        [Header("场景UI配置")]
        [Tooltip("所有场景的UI配置数据")]
        public List<SceneUIData> sceneUIDatas = new List<SceneUIData>();
        
        // 场景名称到UI面板ID列表的映射
        private Dictionary<string, List<string>> _sceneToUIPanelMap = new Dictionary<string, List<string>>();
        
        // 当前场景加载的UI面板实例
        private List<GameObject> _currentSceneUIInstances = new List<GameObject>();
        
        const string module = LogModules.UIMANAGER;
        
        #endregion
        
        #region 生命周期
        
        protected override void Awake()
        {
            base.Awake();
            InitializeRegistry();
            
            // 注册场景加载事件
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
            GameEvents.OnSceneUnload += OnSceneUnload;
        }
        
        private void InitializeRegistry()
        {
            _sceneToUIPanelMap.Clear();
            
            // 构建场景名称到UI面板ID的映射
            foreach (var sceneUIData in sceneUIDatas)
            {
                if (!string.IsNullOrEmpty(sceneUIData.sceneName) && sceneUIData.uiPanelIds != null)
                {
                    if (!_sceneToUIPanelMap.ContainsKey(sceneUIData.sceneName))
                    {
                        _sceneToUIPanelMap[sceneUIData.sceneName] = new List<string>(sceneUIData.uiPanelIds);
                    }
                    else
                    {
                        Log.Warning(module, $"场景 '{sceneUIData.sceneName}' 的UI配置已存在，将被覆盖");
                        _sceneToUIPanelMap[sceneUIData.sceneName] = new List<string>(sceneUIData.uiPanelIds);
                    }
                }
            }
            
            Log.Info(module, $"场景UI注册表初始化完成，共加载 {_sceneToUIPanelMap.Count} 个场景的UI配置");
        }
        
        private void OnDestroy()
        {
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;
            GameEvents.OnSceneUnload -= OnSceneUnload;
            
            // 清理当前场景的UI实例
            ClearCurrentSceneUI();
        }
        
        #endregion
        
        #region 场景事件处理
        
        private void OnSceneLoadComplete(string sceneName)
        {
            // 清理之前场景的UI
            ClearCurrentSceneUI();
            
            // 加载当前场景的UI
            LoadSceneUI(sceneName);
        }
        
        private void OnSceneUnload(string sceneName)
        {
            // 清理当前场景的UI
            ClearCurrentSceneUI();
        }
        
        #endregion
        
        #region UI加载和卸载
        
        /// <summary>
        /// 加载指定场景的UI面板
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void LoadSceneUI(string sceneName)
        {
            if (_sceneToUIPanelMap.TryGetValue(sceneName, out var panelIds))
            {
                Log.Info(module, $"开始加载场景 '{sceneName}' 的UI面板，共 {panelIds.Count} 个面板");
                
                // 调用UIManager加载UI面板
                foreach (var panelId in panelIds)
                {
                    var uiInstance = UIManager.Instance.LoadUIPanel(panelId);
                    if (uiInstance != null)
                    {
                        _currentSceneUIInstances.Add(uiInstance);
                    }
                }
            }
        }
        
        /// <summary>
        /// 清理当前场景的所有UI实例
        /// </summary>
        private void ClearCurrentSceneUI()
        {
            foreach (var uiInstance in _currentSceneUIInstances)
            {
                if (uiInstance != null)
                {
                    var panel = uiInstance.GetComponent<IUIPanel>();
                    if (panel != null)
                    {
                        panel.Cleanup();
                    }
                    Destroy(uiInstance);
                }
            }
            
            _currentSceneUIInstances.Clear();
        }
        
        /// <summary>
        /// 检查指定场景是否有注册的UI面板
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>是否有注册的UI面板</returns>
        public bool HasSceneUI(string sceneName)
        {
            return _sceneToUIPanelMap.ContainsKey(sceneName) && _sceneToUIPanelMap[sceneName].Count > 0;
        }
        
        /// <summary>
        /// 获取指定场景的UI面板ID列表
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>UI面板ID列表</returns>
        public List<string> GetScenePanelIds(string sceneName)
        {
            if (_sceneToUIPanelMap.TryGetValue(sceneName, out var panelIds))
            {
                return new List<string>(panelIds);
            }
            
            return new List<string>();
        }
        
        #endregion
    }
}