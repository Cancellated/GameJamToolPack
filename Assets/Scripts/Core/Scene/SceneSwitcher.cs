using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using MyGame.Events;
using Logger;

namespace MyGame.Managers
{
    /// <summary>
    /// 场景切换管理器，负责处理场景加载和卸载。
    /// 基于事件的统一场景切换系统；场景通过 Addressables 加载。
    ///
    /// 【场景地址约定】
    ///   - "Level Select" 等可热更场景：Addressable 地址（Scenes 组，如 "Scenes/Level Select"）；
    ///   - "MainMenu" 启动场景：保留在 Build Settings 中作为入口，
    ///     运行时作为内置场景以场景路径（Assets/Scenes/MainMenu.unity）为 key 加载。
    ///   映射表见 s_sceneAddressMap，新增场景时在此登记。
    /// </summary>
    public class SceneSwitcher : Singleton<SceneSwitcher>
    {
        private const string module = LogModules.SCENE;

        /// <summary>
        /// 场景名 → Addressable 加载 key 映射。
        /// 内置场景（Build Settings 中）用场景路径作 key；纯 Addressable 场景用组内地址。
        /// </summary>
        private static readonly Dictionary<string, string> s_sceneAddressMap = new()
        {
            { "MainMenu", "Assets/Scenes/MainMenu.unity" },
            { "Level Select", "Scenes/Level Select" },
        };

        /// <summary>最近一次 Single 模式加载的场景句柄（旧场景卸载后释放）</summary>
        private AsyncOperationHandle<SceneInstance> _activeSceneHandle;

        /// <summary>Additive 模式加载的场景句柄（随下次 Single 加载一起释放）</summary>
        private readonly List<AsyncOperationHandle<SceneInstance>> _additiveSceneHandles = new();

        #region 生命周期
        private void OnEnable()
        {
            // 注册场景加载请求事件监听
            GameEvents.OnSceneLoadStart += OnSceneLoadStartHandler;
            // 注册加载界面准备就绪事件监听
            GameEvents.OnLoadingScreenReady += OnLoadingScreenReadyHandler;
        }

        private void OnDisable()
        {
            // 注销场景加载请求事件监听
            GameEvents.OnSceneLoadStart -= OnSceneLoadStartHandler;
            // 注销加载界面准备就绪事件监听
            GameEvents.OnLoadingScreenReady -= OnLoadingScreenReadyHandler;
        }
        #endregion
        
        #region 统一入口
        /// <summary>
        /// 请求加载场景（静态方法，外部系统可以直接调用）
        /// 这是统一的场景加载入口，通过事件机制实现
        /// </summary>
        /// <param name="sceneName">要加载的场景名称（须在 s_sceneAddressMap 中登记）</param>
        public static void RequestLoadScene(string sceneName)
        {
            Log.Info(module, $"发起场景加载请求: {sceneName}");
            GameEvents.TriggerSceneLoadStart(sceneName);
        }
        #endregion

        #region 事件处理方法
        /// <summary>
        /// 处理场景加载开始事件
        /// 此方法仅记录日志，实际加载逻辑已移至OnLoadingScreenReadyHandler
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        private void OnSceneLoadStartHandler(string sceneName)
        {
            Log.Info(module, $"接收到场景加载请求: {sceneName}");
            // 不再直接加载场景，等待加载界面准备就绪后由OnLoadingScreenReadyHandler处理
        }
        
        /// <summary>
        /// 处理加载界面准备就绪事件
        /// 当加载界面完全显示后，开始实际的场景加载
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        private void OnLoadingScreenReadyHandler(string sceneName)
        {
            Log.Info(module, $"加载界面已准备就绪，开始实际加载场景: {sceneName}");
            LoadSceneAsync(sceneName);
        }
        #endregion

        #region 场景加载方法
        /// <summary>
        /// 异步加载场景（Addressables）
        /// </summary>
        /// <param name="sceneName">场景名称（须在 s_sceneAddressMap 中登记）</param>
        /// <param name="unloadCurrent">是否卸载当前场景（Single/Additive 模式）</param>
        public void LoadSceneAsync(string sceneName, bool unloadCurrent = true)
        {
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName, unloadCurrent));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, bool unloadCurrent)
        {
            if (!s_sceneAddressMap.TryGetValue(sceneName, out string address))
            {
                Log.Error(module, $"场景地址映射中未找到场景: {sceneName}，请在 SceneSwitcher.s_sceneAddressMap 中登记");
                yield break;
            }

            Log.Info(module, $"开始异步加载场景: {sceneName} (address: {address})");

            LoadSceneMode loadMode = unloadCurrent ? LoadSceneMode.Single : LoadSceneMode.Additive;

            // Addressables 异步加载场景
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(address, loadMode);
            while (!handle.IsDone)
            {
                yield return null;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error(module, $"场景加载失败: {sceneName}，状态: {handle.Status}");
                Addressables.Release(handle);
                yield break;
            }

            // Single 模式：旧场景已被卸载，释放旧句柄与 Additive 句柄
            if (unloadCurrent)
            {
                if (_activeSceneHandle.IsValid())
                {
                    Addressables.Release(_activeSceneHandle);
                }
                foreach (var additiveHandle in _additiveSceneHandles)
                {
                    if (additiveHandle.IsValid())
                    {
                        Addressables.Release(additiveHandle);
                    }
                }
                _additiveSceneHandles.Clear();

                _activeSceneHandle = handle;
            }
            else
            {
                // Additive 模式：手动设置新场景为活动场景
                _additiveSceneHandles.Add(handle);
                if (handle.Result.Scene.IsValid())
                {
                    SceneManager.SetActiveScene(handle.Result.Scene);
                }
                else
                {
                    Log.Warning(module, $"Additive 加载的场景 {sceneName} 无效，无法设为活动场景");
                }
            }

            // 触发场景加载完成事件
            GameEvents.TriggerSceneLoadComplete(sceneName);
            Log.Info(module, $"场景加载完成: {sceneName}");
        }
        #endregion
    }
}
