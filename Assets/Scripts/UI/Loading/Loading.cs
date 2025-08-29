using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Logger;
using MyGame.Managers;


namespace UI.Loading
{
    /// <summary>
    /// 加载界面组件
    /// </summary>
    public class Loading : MonoBehaviour
    {
        private const string module = "Loading";
        private CanvasGroup m_canvasGroup;
        
        private void Awake()
        {
            // 获取CanvasGroup组件
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                // 如果没有CanvasGroup组件，自动添加
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 确保在UIManager初始化时能找到此组件
            if (UIManager.Instance != null)
            {
                UIManager.Instance.loadingPanel = m_canvasGroup;
            }
        }
        
        /// <summary>
        /// 设置加载界面的显示状态
        /// 注意：此方法应由UIManager调用，而不是直接调用
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetVisible(bool show)
        {
            if (m_canvasGroup != null)
            {
                Log.Info(module, show ? "显示加载界面" : "隐藏加载界面");
            // 这里只记录日志，实际显隐由UIManager的CanvasGroup动画控制
            }
        }
        /// <summary>
        /// 当场景开始加载时的回调
        /// 可在此处添加加载动画、进度条等逻辑
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        public void OnSceneLoadStarted(string sceneName)
        {
            Log.Info(module, $"场景加载开始: {sceneName}");
            // 可以在这里添加加载动画的启动逻辑
        }
        
        /// <summary>
        /// 当场景加载完成时的回调
        /// 可在此处清理加载相关资源
        /// </summary>
        /// <param name="sceneName">已加载完成的场景名称</param>
        public void OnSceneLoadCompleted(string sceneName)
        {
            Log.Info(module, $"场景加载完成: {sceneName}");
            // 可以在这里添加加载动画的结束逻辑
        }
    }
}
