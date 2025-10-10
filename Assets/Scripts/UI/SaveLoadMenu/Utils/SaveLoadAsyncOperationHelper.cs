using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using Logger;
using MyGame.UI.SaveLoad.View;
using MyGame.Data;

namespace MyGame.UI.SaveLoad.Utils
{
    /// <summary>
    /// 异步操作辅助类
    /// 处理存档菜单中涉及的异步资源加载、实例化和释放操作
    /// </summary>
    public class SaveLoadAsyncOperationHelper
    {
        private const string LOG_MODULE = "SaveLoadAsyncOperationHelper";

        /// <summary>
        /// 异步加载预制件
        /// </summary>
        /// <param name="prefabRef">预制件资源引用</param>
        /// <param name="onLoaded">加载完成回调</param>
        /// <param name="onFailed">加载失败回调</param>
        public void LoadPrefabAsync(AssetReference prefabRef, System.Action<GameObject> onLoaded, System.Action<string> onFailed = null)
        {
            if (prefabRef == null)
            {
                Log.Error(LOG_MODULE, "预制件引用为空");
                onFailed?.Invoke("预制件引用为空");
                return;
            }

            // 开始异步加载预制件
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(prefabRef);
            handle.Completed += obj =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    onLoaded?.Invoke(obj.Result);
                }
                else
                {
                    string errorMsg = string.Format("加载预制件失败: {0}", obj.OperationException?.Message ?? "未知错误");
                    Log.Error(LOG_MODULE, errorMsg);
                    onFailed?.Invoke(errorMsg);
                    // 释放失败的操作句柄
                    Addressables.Release(obj);
                }
            };
        }

        /// <summary>
        /// 异步实例化预制件
        /// </summary>
        /// <param name="prefabRef">预制件资源引用</param>
        /// <param name="parent">父变换</param>
        /// <param name="onInstantiated">实例化完成回调</param>
        /// <param name="onFailed">实例化失败回调</param>
        public void InstantiatePrefabAsync(AssetReference prefabRef, Transform parent, System.Action<GameObject> onInstantiated, System.Action<string> onFailed = null)
        {
            if (prefabRef == null)
            {
                Log.Error(LOG_MODULE, "预制件引用为空");
                onFailed?.Invoke("预制件引用为空");
                return;
            }

            // 开始异步实例化预制件
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(prefabRef, parent);
            handle.Completed += obj =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    onInstantiated?.Invoke(obj.Result);
                }
                else
                {
                    string errorMsg = string.Format("实例化预制件失败: {0}", obj.OperationException?.Message ?? "未知错误");
                    Log.Error(LOG_MODULE, errorMsg);
                    onFailed?.Invoke(errorMsg);
                    // 释放失败的操作句柄
                    Addressables.Release(obj);
                }
            };
        }

        /// <summary>
        /// 异步加载并实例化多个预制件
        /// </summary>
        /// <param name="prefabRef">预制件资源引用</param>
        /// <param name="parent">父变换</param>
        /// <param name="count">实例化数量</param>
        /// <param name="onAllInstantiated">所有实例化完成回调</param>
        /// <param name="onProgress">进度回调</param>
        /// <param name="onFailed">失败回调</param>
        public void InstantiateMultiplePrefabsAsync(AssetReference prefabRef, Transform parent, int count, 
            System.Action<List<GameObject>> onAllInstantiated, System.Action<float> onProgress = null, System.Action<string> onFailed = null)
        {
            if (prefabRef == null)
            {
                Log.Error(LOG_MODULE, "预制件引用为空");
                onFailed?.Invoke("预制件引用为空");
                return;
            }

            List<GameObject> instantiatedObjects = new();
            int instantiatedCount = 0;
            bool hasFailed = false;

            for (int i = 0; i < count; i++)
            {
                // 为每个实例创建一个本地索引变量，避免闭包中的陷阱
                int index = i;
                InstantiatePrefabAsync(prefabRef, parent, 
                    instance =>
                    {
                        if (hasFailed) return;

                        instantiatedObjects.Add(instance);
                        instantiatedCount++;

                        // 更新进度
                        if (onProgress != null)
                        {
                            float progress = (float)instantiatedCount / count;
                            onProgress(progress);
                        }

                        // 检查是否所有实例都已创建完成
                        if (instantiatedCount == count)
                        {
                            onAllInstantiated?.Invoke(instantiatedObjects);
                        }
                    },
                    errorMsg =>
                    {
                        if (hasFailed) return;

                        hasFailed = true;
                        onFailed?.Invoke(errorMsg);
                    }
                );
            }
        }

        /// <summary>
        /// 释放异步操作句柄
        /// </summary>
        /// <param name="handle">要释放的操作句柄</param>
        public void ReleaseHandle<T>(AsyncOperationHandle<T> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        /// <summary>
        /// 批量释放异步操作句柄
        /// </summary>
        /// <param name="handles">要释放的操作句柄集合</param>
        public void ReleaseHandles<T>(IEnumerable<AsyncOperationHandle<T>> handles)
        {
            foreach (var handle in handles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        /// <summary>
        /// 异步创建存档槽UI
        /// </summary>
        /// <param name="prefabRef">存档槽预制件引用</param>
        /// <param name="container">容器变换</param>
        /// <param name="count">创建数量</param>
        /// <param name="saveSlotInfos">存档槽信息列表</param>
        /// <param name="view">视图引用</param>
        /// <param name="onAllCreated">全部创建完成回调</param>
        /// <param name="onProgress">进度回调</param>
        public IEnumerator CreateSaveSlotUIs(AssetReference prefabRef, Transform container, int count, List<SaveSlotInfo> saveSlotInfos, 
            SaveLoadMenuView view, System.Action<List<ISaveSlotUI>> onAllCreated, System.Action<float> onProgress = null)
        {
            if (prefabRef == null || container == null || count <= 0 || view == null)
            {
                Log.Error(LOG_MODULE, "无效的参数");
                yield break;
            }

            // 确保存档槽信息列表有足够的元素
            while (saveSlotInfos.Count < count)
            {
                saveSlotInfos.Add(new SaveSlotInfo());
            }

            List<ISaveSlotUI> saveSlotUIs = new();
            int createdCount = 0;
            bool hasError = false;

            // 先加载预制件一次
            AsyncOperationHandle<GameObject> prefabHandle = Addressables.LoadAssetAsync<GameObject>(prefabRef);
            yield return prefabHandle;

            if (prefabHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error(LOG_MODULE, "加载存档槽预制件失败: " + prefabHandle.OperationException?.Message);
                Addressables.Release(prefabHandle);
                yield break;
            }

            GameObject saveSlotPrefab = prefabHandle.Result;

            // 然后实例化所需数量的预制件
            for (int i = 0; i < count && !hasError; i++)
            {
                try
                {
                    // 直接实例化预制件（同步）
                    GameObject instance = Object.Instantiate(saveSlotPrefab, container);
                    instance.name = "SaveSlot_" + i;
                    
                    // 获取ISaveSlotUI组件
                    if (instance.TryGetComponent<ISaveSlotUI>(out var saveSlotUI))
                    {
                        saveSlotUI.Initialize(saveSlotInfos[i], view);
                        saveSlotUIs.Add(saveSlotUI);
                        createdCount++;
                    }
                    else
                    {
                        Log.Error(LOG_MODULE, string.Format("存档槽预制件缺少ISaveSlotUI组件"));
                        Object.Destroy(instance);
                        hasError = true;
                    }

                    // 更新进度
                    if (onProgress != null)
                    {
                        float progress = (float)createdCount / count;
                        onProgress(progress);
                    }

                    // 每创建几个对象就暂停一帧，避免帧卡顿
                    bool shouldYield = createdCount % 5 == 0;
                }
                catch (System.Exception e)
                {
                    Log.Error(LOG_MODULE, "创建存档槽UI时出错: " + e.Message);
                    hasError = true;
                }

                if (createdCount % 5 == 0 && !hasError)
                {
                    yield return null;
                }
            }

            // 释放预制件资源
            Addressables.Release(prefabHandle);

            // 如果没有错误，调用完成回调
            if (!hasError && onAllCreated != null)
            {
                onAllCreated(saveSlotUIs);
            }
        }

        /// <summary>
        /// 清理存档槽UI
        /// </summary>
        /// <param name="saveSlotUIs">要清理的存档槽UI列表</param>
        public void CleanupSaveSlotUIs(List<ISaveSlotUI> saveSlotUIs)
        {
            if (saveSlotUIs == null) return;

            foreach (var saveSlotUI in saveSlotUIs)
            {
                if (saveSlotUI is MonoBehaviour monoBehaviour)
                {
                    // 销毁存档槽游戏对象
                    Object.Destroy(monoBehaviour.gameObject);
                }
            }
            
            // 清空列表
            saveSlotUIs.Clear();
        }
    }
}