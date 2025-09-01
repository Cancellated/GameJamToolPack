using System.Collections.Generic;
using UnityEngine;
using Logger;
using static Logger.LogModules;
using MyGame.Events;
using MyGame.Managers;

namespace MyGame.UI.Core
{
    /// <summary>
    /// 场景组件注册表，管理场景与UI组件的绑定关系
    /// </summary>
    public class SceneComponentRegistry : Singleton<SceneComponentRegistry>
    {
        private const string LOG_MODULE = LogModules.UI;

        // 场景名称到UI面板ID列表的映射
        private Dictionary<string, List<string>> _sceneToPanelIds;

        // 当前场景加载的UI面板实例
        private List<GameObject> _currentSceneUIInstances;
        
        protected override void Awake()
        {
            base.Awake();
            _currentSceneUIInstances = new List<GameObject>();
            _sceneToPanelIds = new Dictionary<string, List<string>>();
            
            // 注册场景加载事件监听
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
            GameEvents.OnSceneUnload += OnSceneUnload;
            
            Log.Info(LOG_MODULE, "场景组件注册表初始化完成");
        }
        
        void OnDestroy()
        {
            // 取消事件监听
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;
            GameEvents.OnSceneUnload -= OnSceneUnload;
        }
        
        /// <summary>
        /// 注册场景与UI面板的绑定关系
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="panelIds">UI面板ID列表</param>
        public void RegisterSceneUI(string sceneName, List<string> panelIds)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Log.Error(LOG_MODULE, "注册场景UI失败：场景名称不能为空");
                return;
            }
            
            if (panelIds == null || panelIds.Count == 0)
            {
                Log.Error(LOG_MODULE, $"注册场景UI失败：场景 '{sceneName}' 的面板ID列表为空");
                return;
            }
            
            if (!_sceneToPanelIds.ContainsKey(sceneName))
            {
                _sceneToPanelIds[sceneName] = new List<string>();
            }
            
            // 添加面板ID，避免重复
            foreach (var panelId in panelIds)
            {
                if (!string.IsNullOrEmpty(panelId) && !_sceneToPanelIds[sceneName].Contains(panelId))
                {
                    _sceneToPanelIds[sceneName].Add(panelId);
                }
            }
            
            Log.Info(LOG_MODULE, $"成功注册场景 '{sceneName}' 的UI面板：{string.Join(", ", _sceneToPanelIds[sceneName])}");
        }
        
        /// <summary>
        /// 从配置加载场景UI绑定
        /// </summary>
        public void LoadSceneUIBindings()
        {
            // 这里可以从ScriptableObject或其他配置源加载
            // 当前实现中，我们假设使用SceneUIData ScriptableObject
            
            // 查找所有SceneUIData资源
            var sceneUIDatas = Resources.FindObjectsOfTypeAll<SceneUIData>();
            
            foreach (var data in sceneUIDatas)
            {
                RegisterSceneUI(data.sceneName, data.uiPanelIds);
            }
            
            Log.Info(LOG_MODULE, $"从配置加载了 {sceneUIDatas.Length} 个场景的UI绑定信息");
        }
        
        /// <summary>
        /// 场景加载完成时的处理
        /// </summary>
        private void OnSceneLoadComplete(string sceneName)
        {
            // 清理当前场景的UI实例
            ClearCurrentSceneUI();
            
            // 加载新场景的UI
            LoadSceneUI(sceneName);
        }
        
        /// <summary>
        /// 加载指定场景的UI面板
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <summary>
        /// 加载指定场景的UI面板
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        private void LoadSceneUI(string sceneName)
        {
            if (!_sceneToPanelIds.ContainsKey(sceneName))
            {
                Log.Info(LOG_MODULE, $"场景 '{sceneName}' 没有注册UI面板");
                return;
            }
            
            var panelIds = _sceneToPanelIds[sceneName];
            Log.Info(LOG_MODULE, $"开始加载场景 '{sceneName}' 的UI面板：{string.Join(", ", panelIds)}");
            
            // 加载每个面板
        }
        
        /// <summary>
        /// 清理当前场景的UI实例
        /// </summary>
        private void ClearCurrentSceneUI()
        {
            if (_currentSceneUIInstances.Count == 0)
            {
                return;
            }
            
            Log.Info(LOG_MODULE, $"清理当前场景的 {_currentSceneUIInstances.Count} 个UI实例");
            
            foreach (var instance in _currentSceneUIInstances)
            {
                if (instance != null)
                {
                    var panel = instance.GetComponent<IUIPanel>();
                    panel?.Cleanup();
                }
            }
            
            _currentSceneUIInstances.Clear();
        }
        
        /// <summary>
        /// 场景卸载时的处理
        /// </summary>
        private void OnSceneUnload(string sceneName)
        {
            Log.Info(LOG_MODULE, $"场景 '{sceneName}' 卸载，准备清理UI面板");
            
            // 清理当前场景的UI实例
            ClearCurrentSceneUI();
        }
        
        /// <summary>
        /// 检查场景是否注册了UI面板
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>是否注册了UI面板</returns>
        public bool HasSceneUI(string sceneName)
        {
            return _sceneToPanelIds.ContainsKey(sceneName) && _sceneToPanelIds[sceneName].Count > 0;
        }
        
        /// <summary>
        /// 获取场景注册的UI面板ID列表
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>UI面板ID列表</returns>
        public List<string> GetScenePanelIds(string sceneName)
        {
            if (_sceneToPanelIds.TryGetValue(sceneName, out var panelIds))
            {
                return new List<string>(panelIds);
            }
            
            return new List<string>();
        }
        
        /// <summary>
        /// 移除场景的UI面板注册
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void UnregisterSceneUI(string sceneName)
        {
            if (_sceneToPanelIds.ContainsKey(sceneName))
            {
                _sceneToPanelIds.Remove(sceneName);
                Log.Info(LOG_MODULE, $"已移除场景 '{sceneName}' 的UI面板注册");
            }
        }
    }
}