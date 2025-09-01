using UnityEngine;
using Logger;
using static Logger.LogModules;

namespace MyGame.UI.Core
{
    /// <summary>
    /// 场景组件注册表初始化器
    /// 负责在游戏启动时初始化场景组件注册表系统
    /// </summary>
    [DefaultExecutionOrder(-100)] // 确保在其他系统之前执行
    public class SceneComponentRegistryInitializer : MonoBehaviour
    {
        private static string module = LogModules.UI;
        
        private void Awake()
        {
            // 确保SceneComponentRegistry实例存在
            if (SceneComponentRegistry.Instance == null)
            {
                // 创建SceneComponentRegistry实例
                var registryObj = new GameObject("SceneComponentRegistry");
                registryObj.AddComponent<SceneComponentRegistry>();
                DontDestroyOnLoad(registryObj);
                
                Log.Info(module, "创建了SceneComponentRegistry实例");
            }
            
            // 加载场景UI绑定配置
            SceneComponentRegistry.Instance.LoadSceneUIBindings();
            
            // 该初始化器完成任务后可以销毁
            Destroy(this);
        }
    }
}