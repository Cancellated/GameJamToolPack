using Logger;
using MyGame.Data;
using MyGame.DevTools;
using MyGame.Managers;
using MyGame.UI;
using MyGame.UI.Control;
using MyGame.UI.Core;
using UnityEngine;

namespace MyGame
{
    /// <summary>
    /// 全局管理器统一引导。
    /// 在首场景加载完成后，按依赖顺序确认所有核心管理器都存在。
    /// 若某个管理器未挂载到场景 Managers 根对象，Singleton 会触发补挂并输出警告；
    /// 这里再统一输出引导结果，避免事件监听型管理器（如 SceneSwitcher）漏挂后静默失效。
    /// </summary>
    public static class ManagersBootstrap
    {
        private const string LOG_MODULE = LogModules.MANAGERBOOTSTRAP;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // 顺序说明：
            // 1. InputManager：无依赖，其它输入消费者依赖它；
            // 2. GameManager / UIManager / PanelLoader / SceneSwitcher：核心流程；
            // 3. UIController / AudioListenerManager / SceneLoadingController / SaveManager：功能管理器。
            Ensure<InputManager>();
            Ensure<GameManager>();
            Ensure<UIManager>();
            Ensure<PanelLoader>();
            Ensure<SceneSwitcher>();
            Ensure<UIController>();
            Ensure<AudioListenerManager>();
            Ensure<SceneLoadingController>();
            Ensure<SaveManager>();

            Log.Info(LOG_MODULE, "全局管理器引导完成");
        }

        private static void Ensure<T>() where T : MonoBehaviour
        {
            T instance = Singleton<T>.Instance;
            if (instance == null)
            {
                Log.Error(LOG_MODULE, $"管理器 {typeof(T).Name} 创建或查找失败");
                return;
            }

            Log.Info(LOG_MODULE, $"管理器 {typeof(T).Name} 就绪，挂载于 {instance.gameObject.name}");
        }
    }
}
