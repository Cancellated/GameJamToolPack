using MyGame.Events;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace MyGame.Managers
{
    /// <summary>
    /// UI预制体管理器，负责UI预制体的加载、实例化和缓存。
    /// 支持将UI面板做成预制体，并在需要时动态实例化。
    /// </summary>
    public class UIPrefabManager : Singleton<UIPrefabManager>
    {
        #region 字段与属性

        [Header("UI预制体配置")]
        [Tooltip("UI预制体资源路径")]
        public string uiPrefabPath = "Prefabs/UI";

        [Tooltip("所有UI预制体的引用")]
        public List<UIPrefabInfo> uiPrefabs = new List<UIPrefabInfo>();

        private Dictionary<UIManager.UIState, GameObject> _instantiatedUIPanels = new Dictionary<UIManager.UIState, GameObject>();
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

        #endregion

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            
            // 注册场景加载完成事件，确保UI在场景切换时正确初始化
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        private void OnDestroy()
        {
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取指定UI状态对应的CanvasGroup组件
        /// 如果UI尚未实例化，则自动实例化
        /// </summary>
        /// <param name="state">UI状态</param>
        /// <returns>CanvasGroup组件，如果找不到则返回null</returns>
        public CanvasGroup GetOrCreateCanvasGroup(UIManager.UIState state)
        {
            // 检查是否已实例化
            if (_instantiatedUIPanels.ContainsKey(state) && _instantiatedUIPanels[state] != null)
            {
                return _instantiatedUIPanels[state].GetComponent<CanvasGroup>();
            }

            // 查找对应的预制体信息
            UIPrefabInfo prefabInfo = uiPrefabs.Find(info => info.uiState == state);
            if (prefabInfo == null || prefabInfo.prefab == null)
            {
                Debug.LogWarningFormat("[UIPrefabManager] 未找到UI状态 {0} 对应的预制体配置", state);
                return null;
            }

            // 实例化预制体
            GameObject uiPanel = InstantiateUIPrefab(prefabInfo);
            if (uiPanel != null)
            {
                // 缓存实例化的面板
                _instantiatedUIPanels[state] = uiPanel;
                return uiPanel.GetComponent<CanvasGroup>();
            }

            return null;
        }

        /// <summary>
        /// 实例化UI预制体
        /// </summary>
        /// <param name="prefabInfo">预制体信息</param>
        /// <returns>实例化的GameObject</returns>
        public GameObject InstantiateUIPrefab(UIPrefabInfo prefabInfo)
        {
            if (prefabInfo == null || prefabInfo.prefab == null)
            {
                Debug.LogError("[UIPrefabManager] 预制体信息或预制体为空");
                return null;
            }

            // 确保有Canvas作为父对象
            GameObject canvas = FindOrCreateCanvas();
            if (canvas == null)
            {
                Debug.LogError("[UIPrefabManager] 无法找到或创建Canvas对象");
                return null;
            }

            // 实例化预制体
            GameObject uiInstance = Instantiate(prefabInfo.prefab, canvas.transform);
            uiInstance.name = prefabInfo.prefab.name;
            
            // 根据配置设置初始状态
            CanvasGroup canvasGroup = uiInstance.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = prefabInfo.startVisible ? 1f : 0f;
                canvasGroup.interactable = prefabInfo.startVisible;
                canvasGroup.blocksRaycasts = prefabInfo.startVisible;
            }

            return uiInstance;
        }

        /// <summary>
        /// 清理指定状态的UI实例
        /// </summary>
        /// <param name="state">UI状态</param>
        public void ClearUIInstance(UIManager.UIState state)
        {
            if (_instantiatedUIPanels.ContainsKey(state))
            {
                if (_instantiatedUIPanels[state] != null)
                {
                    Destroy(_instantiatedUIPanels[state]);
                }
                _instantiatedUIPanels.Remove(state);
            }
        }

        /// <summary>
        /// 清理所有UI实例
        /// </summary>
        public void ClearAllUIInstances()
        {
            foreach (var kvp in _instantiatedUIPanels)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            _instantiatedUIPanels.Clear();
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 查找或创建Canvas对象
        /// </summary>
        /// <returns>Canvas游戏对象</returns>
        private GameObject FindOrCreateCanvas()
        {
            // 查找现有Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                return canvas.gameObject;
            }

            // 创建新Canvas
            GameObject canvasObj = new GameObject("MainCanvas");
            canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Canvas newCanvas = canvasObj.GetComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            return canvasObj;
        }

        /// <summary>
        /// 场景加载完成回调
        /// </summary>
        /// <param name="sceneName">加载完成的场景名称</param>
        private void OnSceneLoadComplete(string sceneName)
        {
            // 场景切换时重新初始化必要的UI
            // 这里可以根据需要决定是否保留或重新创建UI面板
        }

        #endregion

        #region 编辑器调试方法

#if UNITY_EDITOR
        [ContextMenu("清理所有UI实例")]
        public void DebugClearAllUIInstances()
        {
            ClearAllUIInstances();
        }

        [ContextMenu("打印UI实例状态")]
        public void DebugPrintUIInstances()
        {
            Debug.LogFormat("[UIPrefabManager] 当前实例化的UI面板数量: {0}", _instantiatedUIPanels.Count);
            foreach (var kvp in _instantiatedUIPanels)
            {
                Debug.LogFormat("[UIPrefabManager] UI状态: {0}, 实例: {1}", kvp.Key, kvp.Value ? kvp.Value.name : "null");
            }
        }
#endif

        #endregion
    }

    /// <summary>
    /// UI预制体信息类，用于配置UI预制体与UI状态的对应关系
    /// </summary>
    [System.Serializable]
    public class UIPrefabInfo
    {
        [Tooltip("UI状态")]
        public UIManager.UIState uiState;

        [Tooltip("UI预制体")]
        public GameObject prefab;

        [Tooltip("初始是否可见")]
        public bool startVisible = false;
    }
}