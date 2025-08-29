using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyGame.Events;
using Logger;

namespace MyGame.Managers
{
    /// <summary>
    /// 场景切换管理器，负责处理场景加载和卸载
    /// 实现了基于事件的统一场景切换系统
    /// </summary>
    public class SceneSwitcher : Singleton<SceneSwitcher>
    {
        private const string module = LogModules.SCENE;

        #region 生命周期
        private void OnEnable()
        {
            // 注册场景加载请求事件监听
            GameEvents.OnSceneLoadStart += OnSceneLoadStartHandler;
        }

        private void OnDisable()
        {
            // 注销场景加载请求事件监听
            GameEvents.OnSceneLoadStart -= OnSceneLoadStartHandler;
        }
        #endregion
        
        #region 统一入口
        /// <summary>
        /// 请求加载场景（静态方法，外部系统可以直接调用）
        /// 这是统一的场景加载入口，通过事件机制实现
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        public static void RequestLoadScene(string sceneName)
        {
            Log.Info(module, $"发起场景加载请求: {sceneName}");
            GameEvents.TriggerSceneLoadStart(sceneName);
        }
        #endregion

        #region 事件处理方法
        /// <summary>
        /// 处理场景加载开始事件
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        private void OnSceneLoadStartHandler(string sceneName)
        {
            Log.Info(module, $"接收到场景加载请求: {sceneName}");
            LoadSceneAsync(sceneName);
        }
        #endregion

        #region 场景加载方法
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="unloadCurrent">是否卸载当前场景</param>
        public void LoadSceneAsync(string sceneName, bool unloadCurrent = true)
        {
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName, unloadCurrent));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, bool unloadCurrent)
        {
            // 注意：这里不再触发SceneLoadStart事件，因为该事件已经在RequestLoadScene中触发
            Log.Info(module, $"开始异步加载场景: {sceneName}");

            if (unloadCurrent)
            {
                // 卸载当前场景
                var currentScene = SceneManager.GetActiveScene();
                GameEvents.TriggerSceneUnload(currentScene.name);
                yield return SceneManager.UnloadSceneAsync(currentScene);
            }

            // 异步加载新场景
            var asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 设置新场景为活动场景
            var newScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(newScene);

            // 触发场景加载完成事件
            GameEvents.TriggerSceneLoadComplete(sceneName);
            Log.Info(module, $"场景加载完成: {sceneName}");
        }

        /// <summary>
        /// 直接加载场景（同步）
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void LoadScene(string sceneName)
        {
            Log.Info(module, $"开始同步加载场景: {sceneName}");
            SceneManager.LoadScene(sceneName);
            // 注意：同步加载后可能无法立即触发完成事件，因为场景加载是阻塞的
            // 如果需要确保完成事件被触发，请使用异步加载方法
        }

        /// <summary>
        /// 请求卸载场景
        /// </summary>
        /// <param name="sceneName">要卸载的场景名称</param>
        public static void RequestUnloadScene(string sceneName)
        {
            Log.Info(module, $"发起场景卸载请求: {sceneName}");
            GameEvents.TriggerSceneUnload(sceneName);
        }
        #endregion
    }
}