using System.Collections;
using Logger;
using MyGame.Events;
using UnityEngine;

namespace MyGame.UI.Core
{
    /// <summary>
    /// 场景加载界面编排控制器。
    ///
    /// 从 UIManager 拆出的独立职责：负责"显示 Loading → 等待就绪 → 触发实际加载 → 隐藏 Loading"
    /// 的完整时序编排，使 UIManager 只专注于面板注册与显隐调度。
    ///
    /// 【时序】（事件驱动，不轮询面板注册状态）
    ///   OnSceneLoadStart  → 显示 Loading（防重入拦截并发加载请求）
    ///                     → 等待最短显示时长（unscaledTime，暂停时不受 timeScale 冻结）
    ///                     → TriggerLoadingScreenReady（SceneSwitcher 收到后开始 LoadSceneAsync）
    ///   OnSceneLoadComplete → 重置防重入标志 → 等待最短时长 → 隐藏 Loading
    ///
    /// 【实例化】由 UIManager.Awake 自举创建（Singleton 懒创建），无需在场景中挂载
    /// </summary>
    public class SceneLoadingController : Singleton<SceneLoadingController>
    {
        private const string LOG_MODULE = LogModules.LOADING;

        /// <summary>Loading 最短显示时长（秒）：保证加载动画有足够时间播放。unscaledTime 计时</summary>
        private const float LOADING_MIN_SHOW_TIME = 0.8f;

        /// <summary>Loading 最短停留时长（秒）：保证 Hide 动画播放完整后再关闭</summary>
        private const float LOADING_MIN_HIDE_TIME = 0.5f;

        /// <summary>
        /// 是否正处于场景加载流程中（OnSceneLoadStart 之后、OnSceneLoadComplete 之前）。
        /// 防重入：重复点击"返回主菜单"等导致的并发加载会打乱加载链，必须忽略后续请求
        /// </summary>
        private bool _isLoading;

        #region 生命周期

        private void OnEnable()
        {
            GameEvents.OnSceneLoadStart += OnSceneLoadStart;
            GameEvents.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        private void OnDisable()
        {
            GameEvents.OnSceneLoadStart -= OnSceneLoadStart;
            GameEvents.OnSceneLoadComplete -= OnSceneLoadComplete;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 场景加载开始：显示加载界面，并等待最短显示时长后触发实际场景加载
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        private void OnSceneLoadStart(string sceneName)
        {
            // 防重入：已有加载流程进行中时忽略新请求。
            // 按钮重复绑定曾导致一次点击触发两次 RequestLoadScene，并发加载会打乱加载链
            if (_isLoading)
            {
                Log.Warning(LOG_MODULE, "已有场景加载流程进行中，忽略重复请求: " + sceneName);
                return;
            }
            _isLoading = true;

            Log.Info(LOG_MODULE, "场景加载开始，显示加载界面");
            UIManager.Instance.SetUIState(UIType.Loading, true);

            // 启动协程等待加载界面完全显示后再继续场景加载
            StartCoroutine(WaitForLoadingScreenReady(sceneName));
        }

        /// <summary>
        /// 等待加载界面准备就绪后再触发实际的场景加载事件。
        /// 使用 unscaledTime：暂停状态（timeScale=0）下从暂停界面发起的场景加载不被冻结
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        private IEnumerator WaitForLoadingScreenReady(string sceneName)
        {
            Log.Info(LOG_MODULE, "等待加载界面完全显示");

            // 确保加载界面至少显示一小段时间，使其可见（不受暂停影响）
            yield return new WaitForSecondsRealtime(LOADING_MIN_SHOW_TIME);

            // 触发实际的场景加载事件
            Log.Info(LOG_MODULE, "加载界面已准备就绪，通知实际的场景加载: " + sceneName);
            GameEvents.TriggerLoadingScreenReady(sceneName);
        }

        /// <summary>
        /// 场景加载完成：结束加载流程标志，等待最短停留时长后隐藏加载界面
        /// </summary>
        /// <param name="sceneName">已加载完成的场景名称</param>
        private void OnSceneLoadComplete(string sceneName)
        {
            // 结束加载流程标志，允许下一次场景加载请求
            _isLoading = false;
            Log.Info(LOG_MODULE, "场景加载完成，开始隐藏加载界面");
            StartCoroutine(HideLoading());
        }

        /// <summary>
        /// 等待 Loading 隐藏动画有足够时间播放后再关闭面板
        /// </summary>
        private IEnumerator HideLoading()
        {
            Log.Info(LOG_MODULE, "等待加载界面隐藏动画播放");
            yield return new WaitForSecondsRealtime(LOADING_MIN_HIDE_TIME);
            UIManager.Instance.SetUIState(UIType.Loading, false);
        }

        #endregion
    }
}
